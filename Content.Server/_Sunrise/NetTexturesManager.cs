using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Content.Shared._Sunrise.CartridgeLoader.Cartridges;
using Content.Shared._Sunrise.NetTextures;
using Robust.Server.Player;
using Robust.Shared.ContentPack;
using Robust.Shared.Network;
using Robust.Shared.Network.Transfer;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using ByteHelpers = Robust.Shared.Utility.ByteHelpers;

namespace Content.Server._Sunrise;

/// <summary>
/// Manager that handles dynamic loading of network textures from server to client.
/// Uses High Bandwidth Transfer (WebSocket) to avoid blocking main game traffic.
/// Textures are loaded into MemoryContentRoot on the client.
/// </summary>
public sealed class NetTexturesManager
{
    /// <summary>
    /// Transfer key for server -> client texture downloads via WebSocket
    /// </summary>
    private const string TransferKeyNetTextures = "TransferKeyNetTextures";
    private const int MaxConcurrentTransferWorkers = 2;

    [Dependency] private readonly IResourceManager _resourceManager = default!;
    [Dependency] private readonly IServerNetManager _netManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly ITransferManager _transferManager = default!;

    private ISawmill _sawmill = default!;
    private const string AllowedPrefix = "/NetTextures/";

    /// <summary>
    /// Dynamically registered in-memory resources that are not present on disk.
    /// Key is the relative upload path used on the client (e.g. "NetTextures/Messenger/photo_123.png").
    /// </summary>
    private readonly Dictionary<ResPath, byte[]> _dynamicResources = new();
    private readonly Lock _dynamicResourcesLock = new();
    private readonly Queue<TransferRequest> _pendingTransferRequests = new();
    private readonly Lock _transferQueueLock = new();
    private readonly Dictionary<ResPath, StaticTransferBundle> _staticBundles = new();
    private readonly Dictionary<ResPath, Task<StaticTransferBundle?>> _staticBundleTasks = new();
    private readonly Lock _staticBundleLock = new();
    private int _activeTransferWorkers;

    /// <summary>
    /// Callback for handling photo captures. PhotoCartridgeSystem registers itself here.
    /// </summary>
    public Action<PdaPhotoCaptureMessage>? OnPhotoCaptureMessage { get; set; }

    /// <summary>
    /// Registers the request, fallback, and photo handlers used by the server-side NetTextures pipeline.
    /// </summary>
    public void Initialize()
    {
        _sawmill = _logManager.GetSawmill("network.textures");
        _netManager.RegisterNetMessage<RequestNetworkResourceMessage>(OnRequestNetworkResource);
        _netManager.RegisterNetMessage<NetTextureResourceChunkMessage>();

        _netManager.RegisterNetMessage<PdaPhotoCaptureMessage>(
            msg => OnPhotoCaptureMessage?.Invoke(msg),
            accept: NetMessageAccept.Server);

        _transferManager.RegisterTransferMessage(TransferKeyNetTextures);
    }

    /// <summary>
    /// Clears round-scoped NetTextures caches during round restarts to prevent memory growth.
    /// </summary>
    public void ClearRoundCaches()
    {
        lock (_dynamicResourcesLock)
        {
            _dynamicResources.Clear();
        }

        lock (_staticBundleLock)
        {
            _staticBundles.Clear();
            _staticBundleTasks.Clear();
        }

        _sawmill.Info("Cleared NetTextures round caches due to round restart.");
    }

    /// <summary>
    /// Handles a client resource request after resolving the sender session and validating the path.
    /// </summary>
    /// <param name="msg">The incoming resource request.</param>
    private void OnRequestNetworkResource(RequestNetworkResourceMessage msg)
    {
        if (!_playerManager.TryGetSessionByChannel(msg.MsgChannel, out var session))
            return;

        var resourcePath = msg.ResourcePath;
        ResPath resPath;

        if (resourcePath.StartsWith("/"))
        {
            resPath = new ResPath(resourcePath);
        }
        else
        {
            resPath = new ResPath("/") / resourcePath;
        }

        resPath = resPath.Clean();

        if (!ValidateResourcePath(resPath, out var errorMessage))
        {
            _sawmill.Warning($"Rejected resource request from {session.Name}: {errorMessage} (path: {msg.ResourcePath})");
            return;
        }

        EnqueueResourceSend(session, resPath);
    }

    /// <summary>
    /// Validates that a resource path is safe and within allowed directories.
    /// Prevents path traversal attacks by ensuring paths don't escape allowed directories.
    /// </summary>
    private bool ValidateResourcePath(ResPath path, out string? errorMessage)
    {
        errorMessage = null;

        if (!path.IsRooted)
        {
            errorMessage = "Path must be rooted";
            return false;
        }

        var pathStr = path.ToString();
        if (pathStr.Contains("../") || pathStr.Contains("..\\") || pathStr.StartsWith(".."))
        {
            errorMessage = "Path contains traversal sequences";
            return false;
        }

        if (!pathStr.StartsWith(AllowedPrefix, StringComparison.Ordinal))
        {
            errorMessage = $"Path must start with {AllowedPrefix}";
            return false;
        }

        var relativePath = path.ToRelativePath();
        var relativePathStr = relativePath.ToString();

        if (!relativePathStr.StartsWith("NetTextures/", StringComparison.Ordinal))
        {
            errorMessage = "Path escapes allowed directory after normalization";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Enqueues one validated resource send request onto a bounded background worker pool.
    /// </summary>
    /// <param name="session">The recipient session.</param>
    /// <param name="resourcePath">The validated rooted resource path.</param>
    private void EnqueueResourceSend(ICommonSession session, ResPath resourcePath)
    {
        var shouldStartWorker = false;

        lock (_transferQueueLock)
        {
            _pendingTransferRequests.Enqueue(new TransferRequest(session, resourcePath));

            if (_activeTransferWorkers < MaxConcurrentTransferWorkers)
            {
                _activeTransferWorkers++;
                shouldStartWorker = true;
            }
        }

        if (shouldStartWorker)
            _ = Task.Run(ProcessTransferQueueWorker);
    }

    /// <summary>
    /// Drains queued transfer requests on a small fixed-size worker pool.
    /// </summary>
    private async Task ProcessTransferQueueWorker()
    {
        var shouldStartWorker = false;

        try
        {
            while (true)
            {
                TransferRequest request;
                lock (_transferQueueLock)
                {
                    if (_pendingTransferRequests.Count == 0)
                        return;

                    request = _pendingTransferRequests.Dequeue();
                }

                try
                {
                    await SendResourceAsync(request.Session, request.ResourcePath).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _sawmill.Error(
                        $"Unhandled exception while sending NetTextures resource {request.ResourcePath} to {request.Session.Name}: {ex}");
                }
            }
        }
        finally
        {
            lock (_transferQueueLock)
            {
                _activeTransferWorkers--;

                if (_pendingTransferRequests.Count > 0 &&
                    _activeTransferWorkers < MaxConcurrentTransferWorkers)
                {
                    _activeTransferWorkers++;
                    shouldStartWorker = true;
                }
            }

            if (shouldStartWorker)
                _ = Task.Run(ProcessTransferQueueWorker);
        }
    }

    /// <summary>
    /// Sends a resource (file or directory) to the client using High Bandwidth Transfer (WebSocket).
    /// If the path points to a directory (e.g., .rsi), all files in that directory are sent.
    /// If the path points to a file, only that file is sent.
    /// </summary>
    /// <param name="session">The recipient session.</param>
    /// <param name="resourcePath">The validated rooted resource path to send.</param>
    private async Task SendResourceAsync(ICommonSession session, ResPath resourcePath)
    {
        var startTime = DateTime.UtcNow;
        _sawmill.Debug($"[NetTextures] Starting transfer of {resourcePath} to {session.Name}");

        IReadOnlyList<TransferResourceEntry> filesToSend;
        try
        {
            filesToSend = await CollectFilesToSendAsync(resourcePath).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _sawmill.Warning($"Failed to collect NetTextures resource {resourcePath} for {session.Name}: {ex.Message}");
            return;
        }

        if (filesToSend.Count == 0)
        {
            _sawmill.Warning($"Resource not found: {resourcePath}");
            return;
        }

        try
        {
            var transferStartTime = DateTime.UtcNow;
            await using var transferStream = _transferManager.StartTransfer(session.Channel,
                new TransferStartInfo
                {
                    MessageKey = TransferKeyNetTextures
                });

            await WriteFileStream(transferStream, filesToSend).ConfigureAwait(false);

            var totalTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
            var transferTime = (DateTime.UtcNow - transferStartTime).TotalMilliseconds;
            long totalSize = 0;
            foreach (var file in filesToSend)
            {
                totalSize += file.Length;
            }

            _sawmill.Info($"[NetTextures] Sent {filesToSend.Count} files ({ByteHelpers.FormatBytes(totalSize)}) via High Bandwidth Transfer to {session.Name} in {transferTime:F0}ms (total: {totalTime:F0}ms)");
        }
        catch (Exception ex)
        {
            _sawmill.Warning($"Failed to send resource via High Bandwidth Transfer to {session.Name}: {ex.Message}");
            SendResourceFallback(session, filesToSend);
        }
    }

    /// <summary>
    /// Collects the files that should be sent for a requested resource path.
    /// Dynamic in-memory resources are resolved immediately, while static content resources share an in-flight cache.
    /// </summary>
    /// <param name="resourcePath">The validated rooted resource path.</param>
    /// <returns>The ordered list of uploaded files to transfer.</returns>
    private async Task<IReadOnlyList<TransferResourceEntry>> CollectFilesToSendAsync(ResPath resourcePath)
    {
        var relativeUploadPath = resourcePath.ToRelativePath();

        lock (_dynamicResourcesLock)
        {
            if (_dynamicResources.TryGetValue(relativeUploadPath, out var dynamicData))
                return new[] { TransferResourceEntry.FromMemory(relativeUploadPath, dynamicData) };
        }

        var bundle = await GetOrCreateStaticBundleAsync(resourcePath).ConfigureAwait(false);
        return bundle?.Files ?? [];
    }

    /// <summary>
    /// Gets a cached static transfer bundle or builds it once for all concurrent requesters.
    /// </summary>
    /// <param name="resourcePath">The rooted static content path requested by the client.</param>
    /// <returns>The cached bundle, or <see langword="null"/> if the resource does not exist.</returns>
    private async Task<StaticTransferBundle?> GetOrCreateStaticBundleAsync(ResPath resourcePath)
    {
        Task<StaticTransferBundle?> bundleTask;

        lock (_staticBundleLock)
        {
            if (_staticBundles.TryGetValue(resourcePath, out var cached))
                return cached;

            if (!_staticBundleTasks.TryGetValue(resourcePath, out bundleTask!))
            {
                bundleTask = Task.Run(() => BuildStaticTransferBundle(resourcePath));
                _staticBundleTasks[resourcePath] = bundleTask;
            }
        }

        try
        {
            var bundle = await bundleTask.ConfigureAwait(false);
            if (bundle != null)
            {
                lock (_staticBundleLock)
                {
                    _staticBundles.TryAdd(resourcePath, bundle);
                }
            }

            return bundle;
        }
        finally
        {
            lock (_staticBundleLock)
            {
                if (_staticBundleTasks.TryGetValue(resourcePath, out var existingTask) && existingTask == bundleTask)
                    _staticBundleTasks.Remove(resourcePath);
            }
        }
    }

    /// <summary>
    /// Builds the immutable transfer manifest for a static content file or directory.
    /// </summary>
    /// <param name="resourcePath">The rooted static content path requested by the client.</param>
    /// <returns>The static transfer bundle, or <see langword="null"/> if the resource does not exist.</returns>
    private StaticTransferBundle? BuildStaticTransferBundle(ResPath resourcePath)
    {
        var filesToSend = new List<TransferResourceEntry>();
        var relativeUploadPath = resourcePath.ToRelativePath();
        var files = _resourceManager.ContentFindFiles(resourcePath).ToList();

        if (files.Count == 0)
        {
            if (!_resourceManager.ContentFileExists(resourcePath))
                return null;

            return TryAddContentFile(resourcePath, relativeUploadPath, filesToSend)
                ? new StaticTransferBundle(filesToSend.ToArray())
                : null;
        }

        foreach (var filePath in files)
        {
            if (!filePath.TryRelativeTo(resourcePath, out var relativePath))
                continue;

            TryAddContentFile(filePath, relativeUploadPath / relativePath.Value, filesToSend);
        }

        if (filesToSend.Count == 0)
            return null;

        _sawmill.Debug($"Collected resource directory {resourcePath} ({filesToSend.Count} files)");
        return new StaticTransferBundle(filesToSend.ToArray());
    }

    /// <summary>
    /// Collects one static content file into the transfer manifest.
    /// </summary>
    /// <param name="filePath">The rooted file path to collect.</param>
    /// <param name="relativePath">The relative upload path the client should store.</param>
    /// <param name="filesToSend">The destination collection for the transfer payload.</param>
    /// <returns><see langword="true"/> if the file was read successfully.</returns>
    private bool TryAddContentFile(ResPath filePath, ResPath relativePath, List<TransferResourceEntry> filesToSend)
    {
        var relativePathLength = Encoding.UTF8.GetByteCount(relativePath.CanonPath);
        if ((uint) relativePathLength > NetTextureConstants.MaxTransferPathLength)
        {
            _sawmill.Warning(
                $"Skipping NetTexture file with a relative path longer than the supported transfer limit: {relativePath} ({relativePathLength} > {NetTextureConstants.MaxTransferPathLength})");
            return false;
        }

        if (!_resourceManager.TryContentFileRead(filePath, out var stream))
        {
            _sawmill.Warning($"Failed to read file: {filePath}");
            return false;
        }

        using (stream)
        {
            if (stream.Length < 0 || stream.Length > NetTextureConstants.MaxTransferFileSize)
            {
                _sawmill.Warning(
                    $"Skipping NetTexture file larger than the supported transfer size: {filePath} ({stream.Length} > {NetTextureConstants.MaxTransferFileSize})");
                return false;
            }

            filesToSend.Add(TransferResourceEntry.FromContent(relativePath, filePath, (int) stream.Length));
        }

        return true;
    }

    /// <summary>
    /// Fallback method: sends resources via regular network messages if WebSocket transfer fails.
    /// </summary>
    /// <param name="session">The recipient session.</param>
    /// <param name="files">The files that still need to be delivered.</param>
    private void SendResourceFallback(ICommonSession session, IReadOnlyList<TransferResourceEntry> files)
    {
        var chunkCount = 0;

        foreach (var file in files)
        {
            if (file.DynamicData != null)
            {
                foreach (var message in CreateFallbackChunks(file.RelativePath, file.DynamicData))
                {
                    session.Channel.SendMessage(message);
                    chunkCount++;
                }

                continue;
            }

            chunkCount += SendContentFileFallback(session, file);
        }

        _sawmill.Debug($"Sent {files.Count} files via fallback ({chunkCount} chunk messages) to {session.Name}");
    }

    /// <summary>
    /// Streams one static content file through the fallback chunked transport without materializing the whole file.
    /// </summary>
    /// <param name="session">The recipient session.</param>
    /// <param name="file">The static file descriptor to send.</param>
    /// <returns>The number of chunk messages emitted for this file.</returns>
    private int SendContentFileFallback(ICommonSession session, TransferResourceEntry file)
    {
        if (file.ContentPath == null)
            return 0;

        if (!_resourceManager.TryContentFileRead(file.ContentPath.Value, out var stream))
        {
            _sawmill.Warning($"Failed to read fallback NetTexture file: {file.ContentPath.Value}");
            return 0;
        }

        using (stream)
        {
            var totalChunks = Math.Max(1, (file.Length + NetTextureConstants.MaxChunkSize - 1) / NetTextureConstants.MaxChunkSize);

            for (var chunkIndex = 0; chunkIndex < totalChunks; chunkIndex++)
            {
                var offset = chunkIndex * NetTextureConstants.MaxChunkSize;
                var chunkLength = Math.Min(NetTextureConstants.MaxChunkSize, file.Length - offset);
                var chunkData = new byte[chunkLength];
                ReadExactly(stream, chunkData, chunkLength);

                session.Channel.SendMessage(new NetTextureResourceChunkMessage
                {
                    RelativePath = file.RelativePath.ToString(),
                    ChunkIndex = chunkIndex,
                    TotalChunks = totalChunks,
                    ChunkOffset = offset,
                    TotalLength = file.Length,
                    Data = chunkData
                });
            }

            return totalChunks;
        }
    }

    /// <summary>
    /// Splits one uploaded file into ordered chunk messages for the fallback transport path.
    /// </summary>
    /// <param name="relativePath">The relative client upload path of the file.</param>
    /// <param name="data">The raw file bytes to split.</param>
    /// <param name="chunkSize">The target maximum chunk size in bytes.</param>
    /// <returns>The chunk sequence needed to reconstruct the file on the client.</returns>
    internal static IEnumerable<NetTextureResourceChunkMessage> CreateFallbackChunks(
        ResPath relativePath,
        byte[] data,
        int chunkSize = NetTextureConstants.MaxChunkSize)
    {
        if (chunkSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(chunkSize));

        var totalChunks = Math.Max(1, (data.Length + chunkSize - 1) / chunkSize);

        for (var chunkIndex = 0; chunkIndex < totalChunks; chunkIndex++)
        {
            var offset = chunkIndex * chunkSize;
            var chunkLength = Math.Min(chunkSize, data.Length - offset);
            var chunkData = new byte[chunkLength];
            Buffer.BlockCopy(data, offset, chunkData, 0, chunkLength);

            yield return new NetTextureResourceChunkMessage
            {
                RelativePath = relativePath.ToString(),
                ChunkIndex = chunkIndex,
                TotalChunks = totalChunks,
                ChunkOffset = offset,
                TotalLength = data.Length,
                Data = chunkData
            };
        }
    }

    /// <summary>
    /// Writes files to a transfer stream using the same format as SharedNetworkResourceManager.
    /// Format: [pathLength: uint32][dataLength: uint32][path: bytes][data: bytes][continue: byte]...
    /// </summary>
    /// <param name="stream">The writable transfer stream.</param>
    /// <param name="files">The files to encode into the transfer stream.</param>
    private async Task WriteFileStream(Stream stream, IReadOnlyList<TransferResourceEntry> files)
    {
        var continueByte = new byte[1];
        var buffer = ArrayPool<byte>.Shared.Rent(NetTextureConstants.MaxChunkSize);
        var headerBuffer = Array.Empty<byte>();

        try
        {
            var first = true;

            foreach (var file in files)
            {
                if (!first)
                {
                    continueByte[0] = 1;
                    await stream.WriteAsync(continueByte).ConfigureAwait(false);
                }

                first = false;

                var pathBytes = file.PathBytes;
                var requiredHeaderLength = 8 + pathBytes.Length;
                if (headerBuffer.Length < requiredHeaderLength)
                {
                    if (headerBuffer.Length != 0)
                        ArrayPool<byte>.Shared.Return(headerBuffer);

                    headerBuffer = ArrayPool<byte>.Shared.Rent(requiredHeaderLength);
                }

                BinaryPrimitives.WriteUInt32LittleEndian(headerBuffer.AsSpan(0, 4), (uint) pathBytes.Length);
                BinaryPrimitives.WriteUInt32LittleEndian(headerBuffer.AsSpan(4, 4), (uint) file.Length);
                Array.Copy(pathBytes, 0, headerBuffer, 8, pathBytes.Length);
                await stream.WriteAsync(headerBuffer.AsMemory(0, requiredHeaderLength)).ConfigureAwait(false);
                await WriteTransferData(stream, file, buffer).ConfigureAwait(false);
            }

            continueByte[0] = 0;
            await stream.WriteAsync(continueByte).ConfigureAwait(false);
        }
        finally
        {
            if (headerBuffer.Length != 0)
                ArrayPool<byte>.Shared.Return(headerBuffer);

            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Writes one transfer file payload to the HBT stream, streaming static content files through a pooled buffer.
    /// </summary>
    /// <param name="stream">The destination transfer stream.</param>
    /// <param name="file">The transfer file descriptor to write.</param>
    /// <param name="buffer">The pooled copy buffer used for static files.</param>
    private async Task WriteTransferData(Stream stream, TransferResourceEntry file, byte[] buffer)
    {
        if (file.DynamicData != null)
        {
            await stream.WriteAsync(file.DynamicData).ConfigureAwait(false);
            return;
        }

        if (file.ContentPath == null)
            throw new InvalidOperationException($"NetTextures transfer file {file.RelativePath} has no content source");

        using var contentStream = _resourceManager.ContentFileRead(file.ContentPath.Value);
        while (true)
        {
            var read = await contentStream.ReadAsync(buffer.AsMemory(0, buffer.Length)).ConfigureAwait(false);
            if (read == 0)
                break;

            await stream.WriteAsync(buffer.AsMemory(0, read)).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Reads the exact number of bytes requested from a content stream.
    /// </summary>
    /// <param name="stream">The input stream.</param>
    /// <param name="buffer">The destination buffer.</param>
    /// <param name="count">The number of bytes that must be read.</param>
    private static void ReadExactly(Stream stream, byte[] buffer, int count)
    {
        var offset = 0;
        while (offset < count)
        {
            var read = stream.Read(buffer, offset, count - offset);
            if (read == 0)
                throw new InvalidDataException("Unexpected end of NetTextures content stream");

            offset += read;
        }
    }

    /// <summary>
    /// Registers a dynamic in-memory network texture that is not present on disk.
    /// The resourcePath must point inside /NetTextures/ and will be validated by <see cref="ValidateResourcePath"/>.
    /// </summary>
    /// <param name="resourcePath">Rooted resource path, e.g. "/NetTextures/Messenger/photo_123.png".</param>
    /// <param name="data">Raw file bytes (PNG, WEBP, etc.).</param>
    public void RegisterDynamicResource(string resourcePath, byte[] data)
    {
        var path = resourcePath.StartsWith("/")
            ? new ResPath(resourcePath)
            : new ResPath("/") / resourcePath;

        path = path.Clean();

        if (!ValidateResourcePath(path, out var error))
        {
            _sawmill.Warning($"Failed to register dynamic NetTexture {resourcePath}: {error}");
            return;
        }

        var relativeUploadPath = path.ToRelativePath();
        lock (_dynamicResourcesLock)
        {
            _dynamicResources[relativeUploadPath] = data;
        }

        _sawmill.Debug($"Registered dynamic NetTexture resource: {relativeUploadPath}");
    }

    /// <summary>
    /// Unregisters a dynamic in-memory network texture.
    /// </summary>
    /// <param name="resourcePath">Rooted resource path, e.g. "/NetTextures/Messenger/photo_123.png".</param>
    public void UnregisterDynamicResource(string resourcePath)
    {
        var path = resourcePath.StartsWith('/')
            ? new ResPath(resourcePath)
            : new ResPath("/") / resourcePath;

        path = path.Clean();
        var relativeUploadPath = path.ToRelativePath();

        lock (_dynamicResourcesLock)
        {
            if (_dynamicResources.Remove(relativeUploadPath))
            {
                _sawmill.Debug($"Unregistered dynamic NetTexture resource: {relativeUploadPath}");
            }
        }
    }

    /// <summary>
    /// One file scheduled for transfer to the client.
    /// </summary>
    private sealed class TransferRequest(ICommonSession session, ResPath resourcePath)
    {
        public ICommonSession Session { get; } = session;
        public ResPath ResourcePath { get; } = resourcePath;
    }

    /// <summary>
    /// Immutable manifest for a static content resource so concurrent requesters can share the same transfer plan.
    /// </summary>
    private sealed class StaticTransferBundle(TransferResourceEntry[] files)
    {
        public TransferResourceEntry[] Files { get; } = files;
    }

    private sealed class TransferResourceEntry(ResPath relativePath, int length, ResPath? contentPath, byte[]? dynamicData, byte[] pathBytes)
    {
        public ResPath RelativePath { get; } = relativePath;
        public int Length { get; } = length;
        public ResPath? ContentPath { get; } = contentPath;
        public byte[]? DynamicData { get; } = dynamicData;
        public byte[] PathBytes { get; } = pathBytes;

        public static TransferResourceEntry FromContent(ResPath relativePath, ResPath contentPath, int length)
        {
            return new TransferResourceEntry(relativePath, length, contentPath, null, Encoding.UTF8.GetBytes(relativePath.CanonPath));
        }

        public static TransferResourceEntry FromMemory(ResPath relativePath, byte[] data)
        {
            return new TransferResourceEntry(relativePath, data.Length, null, data, Encoding.UTF8.GetBytes(relativePath.CanonPath));
        }
    }
}

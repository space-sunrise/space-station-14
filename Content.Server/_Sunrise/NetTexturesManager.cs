using System.Buffers.Binary;
using System.IO;
using System.Linq;
using System.Text;
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
    private const int FallbackChunkSize = 64 * 1024;

    [Dependency] private readonly IResourceManager _resourceManager = default!;
    [Dependency] private readonly IServerNetManager _netManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly ITransferManager _transferManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    private ISawmill _sawmill = default!;
    private const string AllowedPrefix = "/NetTextures/";

    /// <summary>
    /// Dynamically registered in-memory resources that are not present on disk.
    /// Key is the relative upload path used on the client (e.g. "NetTextures/Messenger/photo_123.png").
    /// </summary>
    private readonly Dictionary<ResPath, byte[]> _dynamicResources = new();
    private readonly object _dynamicResourcesLock = new();

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
    /// Clears all dynamically registered in-memory resources.
    /// Used during round restarts to prevent memory leaks.
    /// </summary>
    public void ClearDynamicResources()
    {
        lock (_dynamicResourcesLock)
        {
            _dynamicResources.Clear();
        }

        _sawmill.Info("Cleared all dynamic NetTexture resources due to round restart.");
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

        SendResource(session, resPath);
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
    /// Sends a resource (file or directory) to the client using High Bandwidth Transfer (WebSocket).
    /// If the path points to a directory (e.g., .rsi), all files in that directory are sent.
    /// If the path points to a file, only that file is sent.
    /// </summary>
    /// <param name="session">The recipient session.</param>
    /// <param name="resourcePath">The validated rooted resource path to send.</param>
    private async void SendResource(ICommonSession session, ResPath resourcePath)
    {
        var startTime = DateTime.UtcNow;
        _sawmill.Debug($"[NetTextures] Starting transfer of {resourcePath} to {session.Name}");

        List<(ResPath Relative, byte[] Data)> filesToSend;
        try
        {
            filesToSend = await Task.Run(() => CollectFilesToSend(resourcePath)).ConfigureAwait(true);
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
            var totalSize = filesToSend.Sum(f => f.Data.Length);
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
    /// </summary>
    /// <remarks>
    /// A file request yields one uploaded file. An RSI directory request yields every child file using upload paths
    /// relative to the requested directory.
    /// </remarks>
    /// <param name="resourcePath">The validated rooted resource path.</param>
    /// <returns>The ordered list of uploaded files to transfer.</returns>
    private List<(ResPath Relative, byte[] Data)> CollectFilesToSend(ResPath resourcePath)
    {
        var filesToSend = new List<(ResPath Relative, byte[] Data)>();
        var relativeUploadPath = resourcePath.ToRelativePath();

        lock (_dynamicResourcesLock)
        {
            if (_dynamicResources.TryGetValue(relativeUploadPath, out var dynamicData))
            {
                filesToSend.Add((relativeUploadPath, dynamicData));
                return filesToSend;
            }
        }

        var files = _resourceManager.ContentFindFiles(resourcePath).ToList();
        if (files.Count == 0)
        {
            if (!_resourceManager.ContentFileExists(resourcePath))
                return filesToSend;

            CollectSingleFile(resourcePath, filesToSend);
            return filesToSend;
        }

        foreach (var filePath in files)
        {
            if (!filePath.TryRelativeTo(resourcePath, out var relativePath))
                continue;

            var relativePathValue = relativePath.Value;

            if (!_resourceManager.TryContentFileRead(filePath, out var stream))
            {
                _sawmill.Warning($"Failed to read file: {filePath}");
                continue;
            }

            using (stream)
            {
                var data = new byte[stream.Length];
                stream.Read(data, 0, data.Length);

                var relativeUploadPath2 = resourcePath.ToRelativePath();
                var uploadedPath = relativeUploadPath2 / relativePathValue;
                filesToSend.Add((uploadedPath, data));
            }
        }

        _sawmill.Debug($"Collected resource directory {resourcePath} ({files.Count} files)");
        return filesToSend;
    }

    /// <summary>
    /// Collects a single file for transfer.
    /// </summary>
    /// <param name="filePath">The rooted file path to collect.</param>
    /// <param name="filesToSend">The destination collection for the transfer payload.</param>
    /// <returns><see langword="true"/> if the file was read successfully.</returns>
    private bool CollectSingleFile(ResPath filePath, List<(ResPath Relative, byte[] Data)> filesToSend)
    {
        if (!_resourceManager.TryContentFileRead(filePath, out var stream))
        {
            _sawmill.Warning($"Failed to read file: {filePath}");
            return false;
        }

        using (stream)
        {
            var data = new byte[stream.Length];
            stream.Read(data, 0, data.Length);

            var relativeUploadPath = filePath.ToRelativePath();
            filesToSend.Add((relativeUploadPath, data));
        }

        return true;
    }

    /// <summary>
    /// Fallback method: sends resources via regular network messages if WebSocket transfer fails.
    /// </summary>
    /// <param name="session">The recipient session.</param>
    /// <param name="files">The files that still need to be delivered.</param>
    private void SendResourceFallback(ICommonSession session, List<(ResPath Relative, byte[] Data)> files)
    {
        var chunkCount = 0;

        foreach (var (relativePath, data) in files)
        {
            foreach (var message in CreateFallbackChunks(relativePath, data))
            {
                session.Channel.SendMessage(message);
                chunkCount++;
            }
        }

        _sawmill.Debug($"Sent {files.Count} files via fallback ({chunkCount} chunk messages) to {session.Name}");
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
        int chunkSize = FallbackChunkSize)
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
    private static async Task WriteFileStream(Stream stream, IEnumerable<(ResPath Relative, byte[] Data)> files)
    {
        var lengthBytes = new byte[4];
        var continueByte = new byte[1];

        var first = true;

        foreach (var (relative, data) in files)
        {
            if (!first)
            {
                continueByte[0] = 1;
                await stream.WriteAsync(continueByte).ConfigureAwait(false);
            }

            first = false;

            BinaryPrimitives.WriteUInt32LittleEndian(lengthBytes, (uint)Encoding.UTF8.GetByteCount(relative.CanonPath));
            await stream.WriteAsync(lengthBytes).ConfigureAwait(false);

            BinaryPrimitives.WriteUInt32LittleEndian(lengthBytes, (uint)data.Length);
            await stream.WriteAsync(lengthBytes).ConfigureAwait(false);

            await stream.WriteAsync(Encoding.UTF8.GetBytes(relative.CanonPath)).ConfigureAwait(false);
            await stream.WriteAsync(data).ConfigureAwait(false);
        }

        continueByte[0] = 0;
        await stream.WriteAsync(continueByte).ConfigureAwait(false);
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
        var path = resourcePath.StartsWith("/")
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
}


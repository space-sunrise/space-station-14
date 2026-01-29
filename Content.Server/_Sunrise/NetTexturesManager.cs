using System.Buffers.Binary;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Shared._Sunrise.NetTextures;
using Robust.Server.Player;
using Robust.Shared.ContentPack;
using Robust.Shared.Network;
using Robust.Shared.Network.Transfer;
using Robust.Shared.Player;
using Robust.Shared.Upload;
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

    [Dependency] private readonly IResourceManager _resourceManager = default!;
    [Dependency] private readonly IServerNetManager _netManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly ITransferManager _transferManager = default!;

    private ISawmill _sawmill = default!;
    private const string AllowedPrefix = "/NetTextures/";

    public void Initialize()
    {
        _sawmill = _logManager.GetSawmill("network.textures");
        _netManager.RegisterNetMessage<RequestNetworkResourceMessage>(OnRequestNetworkResource);

        // Register transfer key for High Bandwidth Transfer (WebSocket)
        _transferManager.RegisterTransferMessage(TransferKeyNetTextures);
    }

    private void OnRequestNetworkResource(RequestNetworkResourceMessage msg)
    {
        if (!_playerManager.TryGetSessionByChannel(msg.MsgChannel, out var session))
            return;

        // Normalize the path - ensure it's rooted
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

        // Clean the path to remove any .. sequences
        resPath = resPath.Clean();

        // Validate the path to prevent path traversal attacks
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

        // Path must be rooted
        if (!path.IsRooted)
        {
            errorMessage = "Path must be rooted";
            return false;
        }

        // Check for dangerous path traversal sequences in the original string representation
        // Even after Clean(), we should verify the path doesn't contain .. in segments
        var pathStr = path.ToString();
        if (pathStr.Contains("../") || pathStr.Contains("..\\") || pathStr.StartsWith(".."))
        {
            errorMessage = "Path contains traversal sequences";
            return false;
        }

        // Only allow paths that start with /NetTextures/
        // This ensures clients can only access resources from the NetTextures directory
        if (!pathStr.StartsWith(AllowedPrefix, StringComparison.Ordinal))
        {
            errorMessage = $"Path must start with {AllowedPrefix}";
            return false;
        }

        // Additional check: ensure the cleaned path doesn't escape the allowed directory
        // by checking that it still starts with the allowed prefix after cleaning
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
    private async void SendResource(ICommonSession session, ResPath resourcePath)
    {
        var startTime = DateTime.UtcNow;
        _sawmill.Debug($"[NetTextures] Starting transfer of {resourcePath} to {session.Name}");

        // Collect all files to send
        var filesToSend = new List<(ResPath Relative, byte[] Data)>();

        // Check if it's a directory (RSI files are directories)
        // Try to find files in the directory first
        var files = _resourceManager.ContentFindFiles(resourcePath).ToList();

        if (files.Count == 0)
        {
            // No files found in directory, try as single file
            if (!_resourceManager.ContentFileExists(resourcePath))
            {
                _sawmill.Warning($"Resource not found: {resourcePath}");
                return;
            }

            if (!CollectSingleFile(resourcePath, filesToSend))
                return;
        }
        else
        {
            // Directory - collect all files
            foreach (var filePath in files)
            {
                if (!filePath.TryRelativeTo(resourcePath, out var relativePath))
                    continue;

                // relativePath is guaranteed to be non-null here because TryRelativeTo returned true
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

                    // Calculate uploaded path: preserve the original path structure relative to Resources root
                    // Remove leading / and use as relative path for MemoryContentRoot
                    var relativeUploadPath = resourcePath.ToRelativePath();
                    var uploadedPath = relativeUploadPath / relativePathValue;

                    filesToSend.Add((uploadedPath, data));
                }
            }

            _sawmill.Debug($"Collected resource directory {resourcePath} ({files.Count} files) for {session.Name}");
        }

        // Send via High Bandwidth Transfer (WebSocket) to avoid blocking main game traffic
        try
        {
            var transferStartTime = DateTime.UtcNow;
            await using var transferStream = _transferManager.StartTransfer(session.Channel,
                new TransferStartInfo
                {
                    MessageKey = TransferKeyNetTextures
                });

            await WriteFileStream(transferStream, filesToSend);

            var totalTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
            var transferTime = (DateTime.UtcNow - transferStartTime).TotalMilliseconds;
            var totalSize = filesToSend.Sum(f => f.Data.Length);
            _sawmill.Info($"[NetTextures] Sent {filesToSend.Count} files ({ByteHelpers.FormatBytes(totalSize)}) via High Bandwidth Transfer to {session.Name} in {transferTime:F0}ms (total: {totalTime:F0}ms)");
        }
        catch (Exception ex)
        {
            _sawmill.Warning($"Failed to send resource via High Bandwidth Transfer to {session.Name}: {ex.Message}");
            // Fallback to regular network message if WebSocket transfer fails
            SendResourceFallback(session, filesToSend);
        }
    }

    /// <summary>
    /// Collects a single file for transfer.
    /// </summary>
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

            // Calculate uploaded path: preserve the original path structure relative to Resources root
            var relativeUploadPath = filePath.ToRelativePath();
            filesToSend.Add((relativeUploadPath, data));
        }

        return true;
    }

    /// <summary>
    /// Fallback method: sends resources via regular network messages if WebSocket transfer fails.
    /// </summary>
    private void SendResourceFallback(ICommonSession session, List<(ResPath Relative, byte[] Data)> files)
    {
        foreach (var (relativePath, data) in files)
        {
            var uploadMsg = new NetworkResourceUploadMessage(data, relativePath);
            session.Channel.SendMessage(uploadMsg);
        }
        _sawmill.Debug($"Sent {files.Count} files via fallback (regular network) to {session.Name}");
    }

    /// <summary>
    /// Writes files to a transfer stream using the same format as SharedNetworkResourceManager.
    /// Format: [pathLength: uint32][dataLength: uint32][path: bytes][data: bytes][continue: byte]...
    /// </summary>
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
                    await stream.WriteAsync(continueByte);
                }

                first = false;

                BinaryPrimitives.WriteUInt32LittleEndian(lengthBytes, (uint)Encoding.UTF8.GetByteCount(relative.CanonPath));
                await stream.WriteAsync(lengthBytes);

                BinaryPrimitives.WriteUInt32LittleEndian(lengthBytes, (uint)data.Length);
                await stream.WriteAsync(lengthBytes);

                await stream.WriteAsync(Encoding.UTF8.GetBytes(relative.CanonPath));
                await stream.WriteAsync(data);
            }

            continueByte[0] = 0;
            await stream.WriteAsync(continueByte);
    }
}


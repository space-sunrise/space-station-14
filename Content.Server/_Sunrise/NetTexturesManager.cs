using System.Linq;
using Content.Shared._Sunrise.NetTextures;
using Robust.Server.Player;
using Robust.Shared.ContentPack;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Upload;
using Robust.Shared.Utility;

namespace Content.Server._Sunrise;

/// <summary>
/// Manager that handles dynamic loading of network textures from server to client.
/// Textures are loaded into MemoryContentRoot on the client.
/// </summary>
public sealed class NetTexturesManager
{
    [Dependency] private readonly IResourceManager _resourceManager = default!;
    [Dependency] private readonly IServerNetManager _netManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;

    private ISawmill _sawmill = default!;

    public void Initialize()
    {
        _sawmill = _logManager.GetSawmill("network.textures");
        _netManager.RegisterNetMessage<RequestNetworkResourceMessage>(OnRequestNetworkResource);
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

        SendResource(session, resPath);
    }

    /// <summary>
    /// Sends a resource (file or directory) to the client.
    /// If the path points to a directory (e.g., .rsi), all files in that directory are sent.
    /// If the path points to a file, only that file is sent.
    /// </summary>
    private void SendResource(ICommonSession session, ResPath resourcePath)
    {
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

            SendSingleFile(session, resourcePath);
        }
        else
        {
            // Directory - send all files
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

                    var uploadMsg = new NetworkResourceUploadMessage(data, uploadedPath);
                    session.Channel.SendMessage(uploadMsg);
                }
            }

            _sawmill.Debug($"Sent resource directory {resourcePath} ({files.Count} files) to {session.Name}");
        }
    }

    /// <summary>
    /// Sends a single file resource to the client.
    /// </summary>
    private void SendSingleFile(ICommonSession session, ResPath filePath)
    {
        if (!_resourceManager.TryContentFileRead(filePath, out var stream))
        {
            _sawmill.Warning($"Failed to read file: {filePath}");
            return;
        }

        using (stream)
        {
            var data = new byte[stream.Length];
            stream.Read(data, 0, data.Length);

            // Calculate uploaded path: preserve the original path structure relative to Resources root
            var relativeUploadPath = filePath.ToRelativePath();
            var uploadMsg = new NetworkResourceUploadMessage(data, relativeUploadPath);
            session.Channel.SendMessage(uploadMsg);
        }

        _sawmill.Debug($"Sent resource file {filePath} to {session.Name}");
    }
}


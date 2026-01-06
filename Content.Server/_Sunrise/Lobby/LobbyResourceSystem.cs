using System.Linq;
using Content.Shared._Sunrise.Lobby;
using Content.Shared.GameTicking.Prototypes;
using Robust.Server.Player;
using Robust.Shared.ContentPack;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Upload;
using Robust.Shared.Utility;

namespace Content.Server._Sunrise.Lobby;

/// <summary>
/// System that handles dynamic loading of lobby resources (animations and arts) from server to client.
/// </summary>
public sealed class LobbyResourceSystem : EntitySystem
{
    [Dependency] private readonly IResourceManager _resourceManager = default!;
    [Dependency] private readonly IServerNetManager _netManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    private const string UploadedPrefix = "/Uploaded";
    // Server-side paths - these should not exist in client archive
    // Resources are now in Resources root, not in Textures
    private const string AnimationsPath = "/_Sunrise/Lobby/Animations";
    private const string ArtsPath = "/_Sunrise/Lobby/Arts";

    public override void Initialize()
    {
        base.Initialize();
        _netManager.RegisterNetMessage<RequestLobbyResourceMessage>(OnRequestLobbyResource);
    }

    private void OnRequestLobbyResource(RequestLobbyResourceMessage msg)
    {
        if (!_playerManager.TryGetSessionByChannel(msg.MsgChannel, out var session))
            return;

        switch (msg.ResourceType)
        {
            case LobbyResourceType.Animation:
                SendAnimationResource(session, msg.ResourceId);
                break;
            case LobbyResourceType.Art:
                SendArtResource(session, msg.ResourceId);
                break;
        }
    }

    private void SendAnimationResource(ICommonSession session, string animationId)
    {
        if (!_prototypeManager.TryIndex<LobbyAnimationPrototype>(animationId, out var prototype))
        {
            Logger.Warning($"Failed to find LobbyAnimationPrototype with ID: {animationId}");
            return;
        }

        // Get the RSI path from the prototype
        // The path in prototype might be relative (e.g., "_Sunrise/Lobby/Animations/bar.rsi")
        // or absolute (e.g., "/_Sunrise/Lobby/Animations/bar.rsi")
        // Resources are now in Resources root, not in Textures
        var prototypePath = prototype.Animation;
        ResPath rsiPath;

        if (prototypePath.StartsWith("/"))
        {
            // Absolute path - use as is (e.g., "/_Sunrise/Lobby/Animations/bar.rsi")
            rsiPath = new ResPath(prototypePath);
        }
        else
        {
            // Relative path - use as is from Resources root (e.g., "_Sunrise/Lobby/Animations/bar.rsi")
            rsiPath = new ResPath("/") / prototypePath;
        }

        // The RSI path points to the directory, so we need to find all files in it
        // Find all files that start with the RSI directory path
        var rsiFiles = _resourceManager.ContentFindFiles(rsiPath)
            .Where(p => p.TryRelativeTo(rsiPath, out _))
            .ToList();

        if (rsiFiles.Count == 0)
        {
            Logger.Warning($"No files found in RSI directory: {rsiPath}");
            return;
        }

        // Get the RSI directory name (e.g., "bar.rsi")
        var rsiDirName = rsiPath.Filename;

        // Send each file
        foreach (var filePath in rsiFiles)
        {
            if (!_resourceManager.TryContentFileRead(filePath, out var stream))
            {
                Logger.Warning($"Failed to read file: {filePath}");
                continue;
            }

            using (stream)
            {
                var data = new byte[stream.Length];
                stream.Read(data, 0, data.Length);

                // Calculate the uploaded path
                // filePath is something like "/_Sunrise/Lobby/Animations/bar.rsi/meta.json"
                // MemoryContentRoot is registered with prefix /Uploaded, so we need to send relative path
                // (without /Uploaded prefix): "_Sunrise/Lobby/Animations/bar.rsi/meta.json"
                var relativePath = filePath.RelativeTo(rsiPath);
                var uploadedPath = new ResPath("_Sunrise") / "Lobby" / "Animations" / rsiDirName / relativePath;

                var uploadMsg = new NetworkResourceUploadMessage(data, uploadedPath);
                session.Channel.SendMessage(uploadMsg);
            }
        }

        Logger.Debug($"Sent animation resource {animationId} ({rsiFiles.Count} files) to {session.Name}");
    }

    private void SendArtResource(ICommonSession session, string artId)
    {
        if (!_prototypeManager.TryIndex<LobbyBackgroundPrototype>(artId, out var prototype))
        {
            Logger.Warning($"Failed to find LobbyBackgroundPrototype with ID: {artId}");
            return;
        }

        // Get the image path from the prototype
        var imagePath = prototype.Background;

        if (!_resourceManager.TryContentFileRead(imagePath, out var stream))
        {
            Logger.Warning($"Failed to read art file: {imagePath}");
            return;
        }

        using (stream)
        {
            var data = new byte[stream.Length];
            stream.Read(data, 0, data.Length);

            // Calculate the uploaded path
            // MemoryContentRoot is registered with prefix /Uploaded, so we need to send relative path
            // (without /Uploaded prefix): "_Sunrise/Lobby/Arts/filename.webp"
            var filename = imagePath.Filename;
            var uploadedPath = new ResPath("_Sunrise") / "Lobby" / "Arts" / filename;

            var uploadMsg = new NetworkResourceUploadMessage(data, uploadedPath);
            session.Channel.SendMessage(uploadMsg);
        }

        Logger.Debug($"Sent art resource {artId} to {session.Name}");
    }
}


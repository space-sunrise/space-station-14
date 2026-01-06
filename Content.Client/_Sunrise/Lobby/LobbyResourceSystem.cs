using Content.Shared._Sunrise.Lobby;
using Robust.Client.ResourceManagement;
using Robust.Client.Upload;
using Robust.Shared.ContentPack;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._Sunrise.Lobby;

/// <summary>
/// System that handles dynamic loading of lobby resources (animations and arts) from server.
/// </summary>
public sealed class LobbyResourceSystem : EntitySystem
{
    [Dependency] private readonly IClientNetManager _netManager = default!;
    [Dependency] private readonly IResourceManager _resourceManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly NetworkResourceManager _networkResourceManager = default!;

    private const string UploadedPrefix = "/Uploaded";
    private readonly HashSet<string> _requestedAnimations = new();
    private readonly HashSet<string> _requestedArts = new();
    private readonly Dictionary<string, string> _pendingAnimations = new(); // animationId -> rsiPath
    private readonly Dictionary<string, ResPath> _pendingArts = new(); // artId -> imagePath

    public event Action<string>? AnimationResourceLoaded; // animationId
    public event Action<string>? ArtResourceLoaded; // artId

    private TimeSpan _lastCheckTime;

    public override void Initialize()
    {
        base.Initialize();
        // NetworkResourceUploadMessage is already registered by SharedNetworkResourceManager
        // We'll check for loaded resources periodically instead
        _lastCheckTime = _gameTiming.CurTime;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Check for loaded resources periodically (every 0.1 seconds for faster response)
        if (_gameTiming.CurTime - _lastCheckTime < TimeSpan.FromSeconds(0.1))
            return;

        _lastCheckTime = _gameTiming.CurTime;

        // Check pending animations
        var completedAnimations = new List<string>();
        foreach (var (animationId, rsiPath) in _pendingAnimations)
        {
            var rsiResPath = new ResPath(rsiPath);
            var rsiDirName = rsiResPath.Filename;
            // Path relative to /Uploaded prefix (without /Uploaded)
            var relativePath = new ResPath("_Sunrise") / "Lobby" / "Animations" / rsiDirName / "meta.json";
            var uploadedPath = (new ResPath(UploadedPrefix) / "_Sunrise" / "Lobby" / "Animations" / rsiDirName / "meta.json").ToRootedPath();

            Logger.Debug($"Checking for animation {animationId} at path: {uploadedPath}, relative: {relativePath}");

            // Check using NetworkResourceManager which checks MemoryContentRoot directly
            bool exists = _networkResourceManager.FileExists(relativePath) || _resourceManager.ContentFileExists(uploadedPath);

            if (exists)
            {
                _requestedAnimations.Add(animationId);
                completedAnimations.Add(animationId);
                Logger.Debug($"Animation resource {animationId} is now available, notifying listeners");
                AnimationResourceLoaded?.Invoke(animationId);
            }
        }

        foreach (var animationId in completedAnimations)
        {
            _pendingAnimations.Remove(animationId);
        }

        // Check pending arts
        var completedArts = new List<string>();
        foreach (var (artId, imagePath) in _pendingArts)
        {
            var filename = imagePath.Filename;
            // Path relative to /Uploaded prefix (without /Uploaded)
            var relativePath = new ResPath("_Sunrise") / "Lobby" / "Arts" / filename;
            var uploadedPath = (new ResPath(UploadedPrefix) / "_Sunrise" / "Lobby" / "Arts" / filename).ToRootedPath();

            Logger.Debug($"Checking for art {artId} at path: {uploadedPath}, relative: {relativePath}");

            // Check using NetworkResourceManager which checks MemoryContentRoot directly
            bool exists = _networkResourceManager.FileExists(relativePath) || _resourceManager.ContentFileExists(uploadedPath);

            if (exists)
            {
                _requestedArts.Add(artId);
                completedArts.Add(artId);
                Logger.Debug($"Art resource {artId} is now available, notifying listeners");
                ArtResourceLoaded?.Invoke(artId);
            }
        }

        foreach (var artId in completedArts)
        {
            _pendingArts.Remove(artId);
        }
    }

    /// <summary>
    /// Checks if an animation resource is available, and requests it if not.
    /// </summary>
    public bool EnsureAnimationResource(string animationId, string rsiPath)
    {
        // Parse the RSI path to get the directory name
        var rsiResPath = new ResPath(rsiPath);
        var rsiDirName = rsiResPath.Filename; // e.g., "bar.rsi"

        // Check if the resource is actually available
        // MemoryContentRoot stores paths relative to /Uploaded prefix
        var relativePath = new ResPath("_Sunrise") / "Lobby" / "Animations" / rsiDirName / "meta.json";
        var uploadedPath = (new ResPath(UploadedPrefix) / "_Sunrise" / "Lobby" / "Animations" / rsiDirName / "meta.json").ToRootedPath();
        if (_networkResourceManager.FileExists(relativePath) || _resourceManager.ContentFileExists(uploadedPath))
        {
            if (!_requestedAnimations.Contains(animationId))
                _requestedAnimations.Add(animationId);
            return true;
        }

        // Check if we've already requested this resource
        if (_requestedAnimations.Contains(animationId))
        {
            // Already requested, but not available yet
            return false;
        }

        // Request the resource
        RequestAnimationResource(animationId);
        _pendingAnimations[animationId] = rsiPath;
        return false;
    }

    /// <summary>
    /// Checks if an art resource is available, and requests it if not.
    /// </summary>
    public bool EnsureArtResource(string artId, ResPath imagePath)
    {
        // Check if the resource is actually available
        // MemoryContentRoot stores paths relative to /Uploaded prefix
        var filename = imagePath.Filename;
        var relativePath = new ResPath("_Sunrise") / "Lobby" / "Arts" / filename;
        var uploadedPath = (new ResPath(UploadedPrefix) / "_Sunrise" / "Lobby" / "Arts" / filename).ToRootedPath();
        if (_networkResourceManager.FileExists(relativePath) || _resourceManager.ContentFileExists(uploadedPath))
        {
            if (!_requestedArts.Contains(artId))
                _requestedArts.Add(artId);
            return true;
        }

        // Check if we've already requested this resource
        if (_requestedArts.Contains(artId))
        {
            // Already requested, but not available yet
            return false;
        }

        // Request the resource
        RequestArtResource(artId);
        _pendingArts[artId] = imagePath;
        return false;
    }

    private void RequestAnimationResource(string animationId)
    {
        if (_requestedAnimations.Contains(animationId))
            return;

        _requestedAnimations.Add(animationId);

        var msg = new RequestLobbyResourceMessage
        {
            ResourceType = LobbyResourceType.Animation,
            ResourceId = animationId
        };

        _netManager.ClientSendMessage(msg);
    }

    private void RequestArtResource(string artId)
    {
        if (_requestedArts.Contains(artId))
            return;

        _requestedArts.Add(artId);

        var msg = new RequestLobbyResourceMessage
        {
            ResourceType = LobbyResourceType.Art,
            ResourceId = artId
        };

        _netManager.ClientSendMessage(msg);
    }

    /// <summary>
    /// Gets the uploaded path for an animation resource.
    /// </summary>
    public ResPath GetAnimationUploadedPath(string rsiPath)
    {
        // rsiPath is something like "/Textures/_Sunrise/Lobby/Animations/bar.rsi" or "_Sunrise/Lobby/Animations/bar.rsi"
        // We need to extract just the directory name "bar.rsi"
        var rsiResPath = new ResPath(rsiPath);
        var rsiDirName = rsiResPath.Filename; // e.g., "bar.rsi"
        var path = new ResPath(UploadedPrefix) / "_Sunrise" / "Lobby" / "Animations" / rsiDirName;
        return path.ToRootedPath(); // Ensure it's always rooted
    }

    /// <summary>
    /// Gets the uploaded path for an art resource.
    /// </summary>
    public ResPath GetArtUploadedPath(ResPath originalPath)
    {
        var filename = originalPath.Filename;
        var path = new ResPath(UploadedPrefix) / "_Sunrise" / "Lobby" / "Arts" / filename;
        return path.ToRootedPath(); // Ensure it's always rooted
    }
}


using Content.Shared._Sunrise.NetTextures;
using Robust.Client.Upload;
using Robust.Shared.ContentPack;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._Sunrise;

/// <summary>
/// Manager that handles dynamic loading of network textures from server.
/// Textures are loaded into MemoryContentRoot on the client.
/// </summary>
public sealed class NetTexturesManager
{
    [Dependency] private readonly IClientNetManager _netManager = default!;
    [Dependency] private readonly IResourceManager _resourceManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly NetworkResourceManager _networkResourceManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;

        private ISawmill _sawmill = default!;

        private const string UploadedPrefix = "/Uploaded";
        private readonly HashSet<string> _requestedResources = new();
        private readonly Dictionary<string, ResPath> _pendingResources = new(); // resourcePath -> ResPath

        /// <summary>
        /// Event fired when a network texture becomes available.
        /// </summary>
        public event Action<string>? ResourceLoaded; // resourcePath

        public void Initialize()
        {
            _sawmill = _logManager.GetSawmill("network.textures");
            // NetworkResourceUploadMessage is already registered by SharedNetworkResourceManager
            // We'll check for loaded resources in Update() method very frequently
        }

    public void Update(float frameTime)
    {
        // If there are no pending resources, skip checking
        if (_pendingResources.Count == 0)
            return;

        // Check for loaded resources every frame when there are pending resources
        // This ensures we catch resources as soon as they're loaded
        var completedResources = new List<string>();
        foreach (var (resourcePath, resPath) in _pendingResources)
        {
            // Check if resource is available
            // MemoryContentRoot stores paths relative to /Uploaded prefix
            var relativePath = resPath.ToRelativePath();

            bool exists = false;
            ResPath checkPath;

            // For RSI directories, check for meta.json file
            // Check if path ends with .rsi (more reliable than Extension property)
            var pathStr = relativePath.ToString();
            if (pathStr.EndsWith(".rsi") || pathStr.EndsWith(".rsi/"))
            {
                checkPath = relativePath / "meta.json";
                exists = _networkResourceManager.FileExists(checkPath);
            }
            else
            {
                // Single file
                checkPath = relativePath;
                exists = _networkResourceManager.FileExists(checkPath);
            }

            if (exists)
            {
                _requestedResources.Add(resourcePath);
                completedResources.Add(resourcePath);
                ResourceLoaded?.Invoke(resourcePath);
            }
        }

        foreach (var resourcePath in completedResources)
        {
            _pendingResources.Remove(resourcePath);
        }
    }

    /// <summary>
    /// Checks if a network texture is available, and requests it if not.
    /// </summary>
    /// <param name="resourcePath">Path to the resource (as specified in prototype, e.g., "/NetTextures/Lobby/Animations/bar.rsi")</param>
    /// <returns>True if the resource is available, false if it's being requested</returns>
    public bool EnsureResource(string resourcePath)
    {
        // Normalize the path
        ResPath resPath;
        if (resourcePath.StartsWith("/"))
        {
            resPath = new ResPath(resourcePath);
        }
        else
        {
            var rootPath = new ResPath("/");
            resPath = rootPath / resourcePath;
        }

        // Check if the resource is actually available
        var relativePath = resPath.ToRelativePath();
        var uploadedPath = (new ResPath(UploadedPrefix) / relativePath).ToRootedPath();

        bool isAvailable = false;

        // For RSI directories, check for meta.json
        // Check if path ends with .rsi (more reliable than Extension property)
        var pathStr = relativePath.ToString();
        if (pathStr.EndsWith(".rsi") || pathStr.EndsWith(".rsi/"))
        {
            var metaPath = relativePath / "meta.json";
            var metaUploadedPath = (new ResPath(UploadedPrefix) / metaPath).ToRootedPath();
            isAvailable = _networkResourceManager.FileExists(metaPath) || _resourceManager.ContentFileExists(metaUploadedPath);
        }
        else
        {
            // Single file
            isAvailable = _networkResourceManager.FileExists(relativePath) || _resourceManager.ContentFileExists(uploadedPath);
        }

        if (isAvailable)
        {
            // Resource is available
            if (!_requestedResources.Contains(resourcePath))
                _requestedResources.Add(resourcePath);
            // Remove from pending if it was there
            _pendingResources.Remove(resourcePath);
            return true;
        }

        // Resource is not available yet
        // If it's already in pending, we're already tracking it
        if (_pendingResources.ContainsKey(resourcePath))
        {
            return false;
        }

        // If it was requested before but not in pending, add it to pending to track it
        if (_requestedResources.Contains(resourcePath))
        {
            _pendingResources[resourcePath] = resPath;
            return false;
        }

        // Request the resource for the first time
        RequestResource(resourcePath);
        _pendingResources[resourcePath] = resPath;

        // Immediately check if the resource is already available (might be cached or loaded very fast)
        // This helps catch resources that load synchronously
        CheckResourceImmediately(resourcePath, resPath);

        return false;
    }

    private void RequestResource(string resourcePath)
    {
        if (_requestedResources.Contains(resourcePath))
            return;

        // Check if client is connected to server before trying to send message
        if (!_netManager.IsConnected)
        {
            _sawmill.Debug($"Cannot request resource {resourcePath}: client not connected to server");
            return;
        }

        _requestedResources.Add(resourcePath);

        var msg = new RequestNetworkResourceMessage
        {
            ResourcePath = resourcePath
        };

        _netManager.ClientSendMessage(msg);
    }

    /// <summary>
    /// Immediately checks if a resource is available and fires the event if it is.
    /// This helps catch resources that load very quickly.
    /// </summary>
    private void CheckResourceImmediately(string resourcePath, ResPath resPath)
    {
        var relativePath = resPath.ToRelativePath();
        bool exists = false;

        // For RSI directories, check for meta.json file
        // Check if path ends with .rsi (more reliable than Extension property)
        var pathStr = relativePath.ToString();
        if (pathStr.EndsWith(".rsi") || pathStr.EndsWith(".rsi/"))
        {
            var metaRelativePath = relativePath / "meta.json";
            exists = _networkResourceManager.FileExists(metaRelativePath);
        }
        else
        {
            // Single file
            exists = _networkResourceManager.FileExists(relativePath);
        }

        if (exists)
        {
            if (!_requestedResources.Contains(resourcePath))
                _requestedResources.Add(resourcePath);
            _pendingResources.Remove(resourcePath);
            ResourceLoaded?.Invoke(resourcePath);
        }
    }

    /// <summary>
    /// Gets the uploaded path for a network texture.
    /// </summary>
    /// <param name="resourcePath">Original resource path from prototype</param>
    /// <returns>Rooted path to the resource in MemoryContentRoot</returns>
    public ResPath GetUploadedPath(string resourcePath)
    {
        ResPath resPath;
        if (resourcePath.StartsWith("/"))
        {
            resPath = new ResPath(resourcePath);
        }
        else
        {
            resPath = new ResPath("/") / resourcePath;
        }

        var relativePath = resPath.ToRelativePath();
        var path = new ResPath(UploadedPrefix) / relativePath;
        return path.ToRootedPath(); // Ensure it's always rooted
    }
}


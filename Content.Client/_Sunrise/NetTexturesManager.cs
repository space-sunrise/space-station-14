using System.Buffers.Binary;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Content.Shared._Sunrise.NetTextures;
using Robust.Client;
using Robust.Client.ResourceManagement;
using Robust.Client.Upload;
using Robust.Shared.Asynchronous;
using Robust.Shared.ContentPack;
using Robust.Shared.Network;
using Robust.Shared.Network.Transfer;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._Sunrise;

/// <summary>
/// Manager that handles dynamic loading of network textures from server.
/// Uses High Bandwidth Transfer (WebSocket) to avoid blocking main game traffic.
/// Textures are loaded into MemoryContentRoot on the client.
/// </summary>
public sealed class NetTexturesManager
{
    /// <summary>
    /// Transfer key for server -> client texture downloads via WebSocket
    /// </summary>
    private const string TransferKeyNetTextures = "TransferKeyNetTextures";

    [Dependency] private readonly IClientNetManager _netManager = default!;
    [Dependency] private readonly IResourceManager _resourceManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IBaseClient _baseClient = default!;
    [Dependency] private readonly ITransferManager _transferManager = default!;
    [Dependency] private readonly ITaskManager _taskManager = default!;

    private ISawmill _sawmill = default!;

    private const string UploadedPrefix = "/Uploaded";
    private readonly HashSet<string> _requestedResources = new();
    private readonly Dictionary<string, ResPath> _pendingResources = new(); // resourcePath -> ResPath

    private readonly MemoryContentRoot _netTexturesContentRoot = new();

        /// <summary>
        /// Event fired when a network texture becomes available.
        /// </summary>
        public event Action<string>? ResourceLoaded; // resourcePath

        public void Initialize()
        {
            _sawmill = _logManager.GetSawmill("network.textures");
            _resourceManager.AddRoot(new ResPath(UploadedPrefix), _netTexturesContentRoot);

            _transferManager.RegisterTransferMessage(TransferKeyNetTextures, ReceiveNetTexturesTransfer);

            // NetworkResourceUploadMessage is already registered by SharedNetworkResourceManager
            // We'll check for loaded resources in Update() method very frequently

            // Clear resources when disconnecting to ensure fresh state on reconnection
            _baseClient.RunLevelChanged += OnRunLevelChanged;
        }

        /// <summary>
        /// Receives NetTextures resources via High Bandwidth Transfer (WebSocket).
        /// This doesn't block the main game traffic.
        /// </summary>
        private async void ReceiveNetTexturesTransfer(TransferReceivedEvent transfer)
        {
            var startTime = DateTime.UtcNow;
            var fileCount = 0;
            var totalSize = 0L;

            _sawmill.Debug("[NetTextures] Starting receive via High Bandwidth Transfer!");

            await using var stream = transfer.DataStream;

            try
            {
                // Read transfer stream using the same format as SharedNetworkResourceManager
                // But without IAsyncEnumerable to avoid sandbox violations
                await ReadTransferStream(stream, (relative, data) =>
                {
                    fileCount++;
                    totalSize += data.Length;
                    _sawmill.Verbose($"Storing NetTexture: {relative} ({ByteHelpers.FormatBytes(data.Length)})");

                    // Store file on main thread (required for MemoryContentRoot)
                    _taskManager.RunOnMainThread(() =>
                    {
                        _netTexturesContentRoot.AddOrUpdateFile(relative, data);

                        // Check if any pending resources are now available
                        CheckPendingResourcesAfterLoad(relative);
                    });
                });

                var totalTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
                _sawmill.Info($"[NetTextures] Received {fileCount} files ({ByteHelpers.FormatBytes(totalSize)}) via High Bandwidth Transfer in {totalTime:F0}ms");
            }
            catch (Exception e)
            {
                _sawmill.Error($"Error while receiving NetTextures transfer: {e}");
            }
        }

        /// <summary>
        /// Reads the transfer stream format used by SharedNetworkResourceManager.
        /// Format: [pathLength: uint32][dataLength: uint32][path: bytes][data: bytes][continue: byte]...
        /// Uses callback instead of IAsyncEnumerable to avoid sandbox violations.
        /// </summary>
        private async Task ReadTransferStream(Stream stream, Action<ResPath, byte[]> onFileRead)
        {
            var lengthBytes = new byte[4];
            var continueByte = new byte[1];

            while (true)
            {
                await stream.ReadExactlyAsync(lengthBytes);
                var pathLength = BinaryPrimitives.ReadUInt32LittleEndian(lengthBytes);

                await stream.ReadExactlyAsync(lengthBytes);
                var dataLength = BinaryPrimitives.ReadUInt32LittleEndian(lengthBytes);

                var pathData = new byte[pathLength];
                await stream.ReadExactlyAsync(pathData);
                var data = new byte[dataLength];
                await stream.ReadExactlyAsync(data);

                var path = new ResPath(Encoding.UTF8.GetString(pathData));
                onFileRead(path, data);

                await stream.ReadExactlyAsync(continueByte);
                if (continueByte[0] == 0)
                    break;
            }
        }

        /// <summary>
        /// Checks if any pending resources became available after a file was loaded.
        /// </summary>
        private void CheckPendingResourcesAfterLoad(ResPath loadedPath)
        {
            var completedResources = new List<string>();

            foreach (var (resourcePath, resPath) in _pendingResources)
            {
                var relativePath = resPath.ToRelativePath();
                bool exists = false;

                // For RSI directories, check if all files are present
                var pathStr = relativePath.ToString();
                if (pathStr.EndsWith(".rsi") || pathStr.EndsWith(".rsi/"))
                {
                    exists = CheckRsiFilesComplete(relativePath);
                }
                else
                {
                    // Single file - check through resource manager (our content root is added there)
                    var uploadedPath = (new ResPath(UploadedPrefix) / relativePath).ToRootedPath();
                    exists = _resourceManager.ContentFileExists(uploadedPath);
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

        private void OnRunLevelChanged(object? sender, RunLevelChangedEventArgs e)
        {
            if (e.OldLevel == ClientRunLevel.InGame)
            {
                _sawmill.Debug("Clearing network texture resource tracking on disconnect");
                _requestedResources.Clear();
                _pendingResources.Clear();
            }
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

            // For RSI directories, check if all files are present
            // This ensures all PNG files are present, not just meta.json
            // Check if path ends with .rsi (more reliable than Extension property)
            var pathStr = relativePath.ToString();
            if (pathStr.EndsWith(".rsi") || pathStr.EndsWith(".rsi/"))
            {
                // Check if all RSI files are present by reading meta.json and verifying all PNG files exist
                exists = CheckRsiFilesComplete(relativePath);
            }
            else
            {
                // Single file - check through resource manager
                var uploadedPath = (new ResPath(UploadedPrefix) / relativePath).ToRootedPath();
                exists = _resourceManager.ContentFileExists(uploadedPath);
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
            isAvailable = _resourceManager.ContentFileExists(metaUploadedPath);
        }
        else
        {
            // Single file
            isAvailable = _resourceManager.ContentFileExists(uploadedPath);
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

        // For RSI directories, check if all files are present
        // Check if path ends with .rsi (more reliable than Extension property)
        var pathStr = relativePath.ToString();
        if (pathStr.EndsWith(".rsi") || pathStr.EndsWith(".rsi/"))
        {
            // Check if all RSI files are present by reading meta.json and verifying all PNG files exist
            exists = CheckRsiFilesComplete(relativePath);
        }
        else
        {
            // Single file - check through resource manager
            var uploadedPath = (new ResPath(UploadedPrefix) / relativePath).ToRootedPath();
            exists = _resourceManager.ContentFileExists(uploadedPath);
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

    /// <summary>
    /// Checks if all RSI files are present by reading meta.json and verifying all PNG files exist.
    /// Uses simple string parsing instead of JsonDocument to avoid sandbox restrictions.
    /// IMPORTANT: Checks files through VFS (_resourceManager) to ensure they're actually available for ResourceCache.
    /// </summary>
    private bool CheckRsiFilesComplete(ResPath relativePath)
    {
        try
        {
            // Read meta.json from uploaded path (check through VFS)
            var uploadedPath = (new ResPath(UploadedPrefix) / relativePath).ToRootedPath();
            var metaUploadedPath = (uploadedPath / "meta.json").ToRootedPath();

            // First check if meta.json exists in VFS
            if (!_resourceManager.ContentFileExists(metaUploadedPath))
            {
                return false;
            }

            // Try to read meta.json to verify it's accessible
            if (!_resourceManager.TryContentFileRead(metaUploadedPath, out var metaStream))
            {
                return false;
            }

            using (metaStream)
            {
                // Read JSON text
                using var reader = new StreamReader(metaStream);
                var jsonText = reader.ReadToEnd();

                // Simple regex to extract state names from JSON
                // Matches "name": "statename" patterns
                var namePattern = new Regex(@"""name""\s*:\s*""([^""]+)""", RegexOptions.Compiled);
                var matches = namePattern.Matches(jsonText);

                if (matches.Count == 0)
                {
                    // No states found, might be invalid JSON or empty states array
                    return false;
                }

                // Check if all PNG files for each state exist in VFS
                foreach (Match match in matches)
                {
                    if (match.Groups.Count < 2)
                        continue;

                    var stateName = match.Groups[1].Value;
                    if (string.IsNullOrEmpty(stateName))
                    {
                        continue;
                    }

                    // Check if PNG file exists in VFS (not just MemoryContentRoot)
                    var pngUploadedPath = (uploadedPath / $"{stateName}.png").ToRootedPath();
                    if (!_resourceManager.ContentFileExists(pngUploadedPath))
                    {
                        _sawmill.Debug($"RSI PNG file not yet available in VFS: {pngUploadedPath}");
                        return false;
                    }
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _sawmill.Debug($"Error checking RSI files completeness: {ex.Message}");
            return false;
        }
    }
}


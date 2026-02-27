using System.Buffers.Binary;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Content.Shared._Sunrise.CartridgeLoader.Cartridges;
using Content.Shared._Sunrise.NetTextures;
using Robust.Client;
using Robust.Shared.Asynchronous;
using Robust.Shared.ContentPack;
using Robust.Shared.Network;
using Robust.Shared.Network.Transfer;
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
    private readonly Dictionary<string, ResPath> _pendingResources = new();

    private readonly MemoryContentRoot _netTexturesContentRoot = new();

        /// <summary>
        /// Event fired when a network texture becomes available.
        /// </summary>
        public event Action<string>? ResourceLoaded;

        public void Initialize()
        {
            _sawmill = _logManager.GetSawmill("network.textures");
            _resourceManager.AddRoot(new ResPath(UploadedPrefix), _netTexturesContentRoot);

            _transferManager.RegisterTransferMessage(TransferKeyNetTextures, ReceiveNetTexturesTransfer);

            _netManager.RegisterNetMessage<PdaPhotoCaptureMessage>(accept: NetMessageAccept.Server);

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
                await ReadTransferStream(stream, (relative, data) =>
                {
                    fileCount++;
                    totalSize += data.Length;
                    _sawmill.Verbose($"Storing NetTexture: {relative} ({ByteHelpers.FormatBytes(data.Length)})");

                    _taskManager.RunOnMainThread(() =>
                    {
                        _netTexturesContentRoot.AddOrUpdateFile(relative, data);

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

                var pathStr = relativePath.ToString();
                if (pathStr.EndsWith(".rsi") || pathStr.EndsWith(".rsi/"))
                {
                    exists = CheckRsiFilesComplete(relativePath);
                }
                else
                {
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
        if (_pendingResources.Count == 0)
            return;

        var completedResources = new List<string>();
        foreach (var (resourcePath, resPath) in _pendingResources)
        {
            var relativePath = resPath.ToRelativePath();

            bool exists = false;
            ResPath checkPath;

            var pathStr = relativePath.ToString();
            if (pathStr.EndsWith(".rsi") || pathStr.EndsWith(".rsi/"))
            {
                exists = CheckRsiFilesComplete(relativePath);
            }
            else
            {
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

        var relativePath = resPath.ToRelativePath();
        var uploadedPath = (new ResPath(UploadedPrefix) / relativePath).ToRootedPath();

        bool isAvailable = false;

        var pathStr = relativePath.ToString();
        if (pathStr.EndsWith(".rsi") || pathStr.EndsWith(".rsi/"))
        {
            var metaPath = relativePath / "meta.json";
            var metaUploadedPath = (new ResPath(UploadedPrefix) / metaPath).ToRootedPath();
            isAvailable = _resourceManager.ContentFileExists(metaUploadedPath);
        }
        else
        {
            isAvailable = _resourceManager.ContentFileExists(uploadedPath);
        }

        if (isAvailable)
        {
            if (!_requestedResources.Contains(resourcePath))
                _requestedResources.Add(resourcePath);
            _pendingResources.Remove(resourcePath);
            return true;
        }

        if (_pendingResources.ContainsKey(resourcePath))
        {
            return false;
        }

        if (_requestedResources.Contains(resourcePath))
        {
            _pendingResources[resourcePath] = resPath;
            return false;
        }

        RequestResource(resourcePath);
        _pendingResources[resourcePath] = resPath;

        CheckResourceImmediately(resourcePath, resPath);

        return false;
    }

    private void RequestResource(string resourcePath)
    {
        if (_requestedResources.Contains(resourcePath))
            return;

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

        var pathStr = relativePath.ToString();
        if (pathStr.EndsWith(".rsi") || pathStr.EndsWith(".rsi/"))
        {
            exists = CheckRsiFilesComplete(relativePath);
        }
        else
        {
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
        return path.ToRootedPath();
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
            var uploadedPath = (new ResPath(UploadedPrefix) / relativePath).ToRootedPath();
            var metaUploadedPath = (uploadedPath / "meta.json").ToRootedPath();

            if (!_resourceManager.ContentFileExists(metaUploadedPath))
            {
                return false;
            }

            if (!_resourceManager.TryContentFileRead(metaUploadedPath, out var metaStream))
            {
                return false;
            }

            using (metaStream)
            {
                using var reader = new StreamReader(metaStream);
                var jsonText = reader.ReadToEnd();

                var namePattern = new Regex(@"""name""\s*:\s*""([^""]+)""", RegexOptions.Compiled);
                var matches = namePattern.Matches(jsonText);

                if (matches.Count == 0)
                {
                    return false;
                }

                foreach (Match match in matches)
                {
                    if (match.Groups.Count < 2)
                        continue;

                    var stateName = match.Groups[1].Value;
                    if (string.IsNullOrEmpty(stateName))
                    {
                        continue;
                    }

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

    /// <summary>
    /// Sends a captured photo to the server.
    /// This method is used by PhotoCartridgeClientSystem to avoid registering its own network message.
    /// </summary>
    /// <param name="loaderUid">Uid of the cartridge loader (PDA) that took the photo</param>
    /// <param name="imageData">PNG image data</param>
    /// <param name="width">Image width in pixels</param>
    /// <param name="height">Image height in pixels</param>
    public void SendPhotoToServer(NetEntity loaderUid, byte[] imageData, int width, int height)
    {
        if (!_netManager.IsConnected)
        {
            _sawmill.Warning("Cannot send photo: client not connected to server");
            return;
        }

        var message = new PdaPhotoCaptureMessage
        {
            LoaderUid = loaderUid,
            ImageData = imageData,
            Width = width,
            Height = height
        };

        _netManager.ClientSendMessage(message);
        _sawmill.Debug($"Sent photo to server: {width}x{height}, {imageData.Length} bytes, loader: {loaderUid}");
    }
}

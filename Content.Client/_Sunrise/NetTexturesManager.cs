using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Content.Shared._Sunrise.CartridgeLoader.Cartridges;
using Content.Shared._Sunrise.NetTextures;
using Robust.Client;
using Robust.Client.Graphics;
using Robust.Shared.Asynchronous;
using Robust.Shared.ContentPack;
using Robust.Shared.Network;
using Robust.Shared.Network.Transfer;
using Robust.Shared.Upload;
using Robust.Shared.Utility;
using ByteHelpers = Robust.Shared.Utility.ByteHelpers;

namespace Content.Client._Sunrise;

public sealed partial class NetTexturesManager
{
    private const string TransferKeyNetTextures = "TransferKeyNetTextures";
    private const string UploadedPrefix = "/Uploaded";

    [Dependency] private readonly IClientNetManager _netManager = default!;
    [Dependency] private readonly IResourceManager _resourceManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IBaseClient _baseClient = default!;
    [Dependency] private readonly ITransferManager _transferManager = default!;
    [Dependency] private readonly ITaskManager _taskManager = default!;
    [Dependency] private readonly IClyde _clyde = default!;

    private readonly MemoryContentRoot _netTexturesContentRoot = new();
    private readonly HashSet<string> _requestedResources = new();
    private readonly Dictionary<string, ResPath> _pendingResources = new();
    private readonly HashSet<string> _preparingResources = new();
    private readonly HashSet<string> _failedResources = new();
    private readonly Dictionary<string, LoadedTextureEntry> _loadedTextures = new();
    private readonly Dictionary<string, LoadedRsiEntry> _loadedRsis = new();
    private readonly Queue<PreparedUploadJob> _preparedUploads = new();
    private readonly Dictionary<ResPath, FallbackChunkAssembly> _fallbackChunkAssemblies = new();
    private readonly Queue<PreparationRequest> _prepareRequests = new();

    private CancellationTokenSource _sessionCts = new();
    private int _sessionGeneration;
    private int _activePrepareRequestId;
    private int _nextPrepareRequestId;
    private bool _prepareWorkerRunning;
    private ISawmill _sawmill = default!;

    public event Action<string>? ResourceLoaded;

    public void Initialize()
    {
        _sawmill = _logManager.GetSawmill("network.textures");

        _resourceManager.AddRoot(new ResPath(UploadedPrefix), _netTexturesContentRoot);
        _transferManager.RegisterTransferMessage(TransferKeyNetTextures, ReceiveNetTexturesTransfer);
        _netManager.RegisterNetMessage<PdaPhotoCaptureMessage>(accept: NetMessageAccept.Server);
        _netManager.RegisterNetMessage<NetTextureResourceChunkMessage>(ReceiveFallbackChunk, accept: NetMessageAccept.Server);
#pragma warning disable CS0618
        _netManager.RegisterNetMessage<NetworkResourceUploadMessage>(ReceiveFallbackUpload, accept: NetMessageAccept.Server);
#pragma warning restore CS0618
        _baseClient.RunLevelChanged += OnRunLevelChanged;
    }

    public void Update(float frameTime)
    {
        if (_pendingResources.Count != 0)
            UpdatePendingResources();

        if (_preparedUploads.Count != 0)
            ProcessPreparedUploads();
    }

    public bool EnsureResource(string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath))
            return false;

        if (IsResourceLoaded(resourcePath))
            return true;

        if (_failedResources.Contains(resourcePath))
            return false;

        var resPath = ToResPath(resourcePath);
        if (IsResourceComplete(resPath))
        {
            StartPreparingResource(resourcePath, resPath);
            return IsResourceLoaded(resourcePath);
        }

        _pendingResources[resourcePath] = resPath;

        if (!_requestedResources.Contains(resourcePath))
            RequestResource(resourcePath);

        return false;
    }

    public bool TryGetTexture(string resourcePath, out Texture? texture)
    {
        if (_loadedTextures.TryGetValue(resourcePath, out var loaded))
        {
            texture = loaded.Texture;
            return true;
        }

        texture = null;
        return false;
    }

    public bool TryGetAnimationState(string resourcePath, string stateId, out NetTextureAnimationState? state)
    {
        state = null;

        if (!_loadedRsis.TryGetValue(resourcePath, out var loaded))
            return false;

        return loaded.States.TryGetValue(stateId, out state);
    }

    public ResPath GetUploadedPath(string resourcePath)
    {
        var relativePath = ToResPath(resourcePath).ToRelativePath();
        return ((new ResPath(UploadedPrefix) / relativePath).ToRootedPath());
    }

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

    private void ReceiveNetTexturesTransfer(TransferReceivedEvent transfer)
    {
        var generation = _sessionGeneration;

        _ = Task.Run(() => ReceiveNetTexturesTransferWorker(transfer.DataStream, generation));
    }

#pragma warning disable CS0618
    private void ReceiveFallbackUpload(NetworkResourceUploadMessage message)
#pragma warning restore CS0618
    {
        var files = new List<(ResPath Relative, byte[] Data)>(1)
        {
            (message.RelativePath, message.Data)
        };

        PublishFiles(files);
    }

    internal void ReceiveFallbackChunk(NetTextureResourceChunkMessage message)
    {
        if (message.TotalChunks <= 0 || message.ChunkIndex < 0 || message.ChunkIndex >= message.TotalChunks)
        {
            _sawmill.Warning($"Rejected malformed NetTextures fallback chunk for {message.RelativePath}: chunk {message.ChunkIndex + 1}/{message.TotalChunks}");
            return;
        }

        var relativePath = new ResPath(message.RelativePath).Clean().ToRelativePath();

        if (!_fallbackChunkAssemblies.TryGetValue(relativePath, out var assembly) ||
            assembly.TotalChunks != message.TotalChunks)
        {
            assembly?.Dispose();
            assembly = new FallbackChunkAssembly(message.TotalChunks);
            _fallbackChunkAssemblies[relativePath] = assembly;
        }

        assembly.StoreChunk(message.ChunkIndex, message.Data);

        if (!assembly.IsComplete)
            return;

        _fallbackChunkAssemblies.Remove(relativePath);
        try
        {
            var files = new List<(ResPath Relative, byte[] Data)>(1)
            {
                (relativePath, assembly.Combine())
            };

            PublishFiles(files);
        }
        finally
        {
            assembly.Dispose();
        }
    }

    internal void PublishFiles(List<(ResPath Relative, byte[] Data)> files)
    {
        foreach (var (relative, data) in files)
        {
            _sawmill.Verbose($"Storing NetTexture: {relative} ({ByteHelpers.FormatBytes(data.Length)})");
            _netTexturesContentRoot.AddOrUpdateFile(relative, data);
            _failedResources.Remove(GetUploadedResourcePath(relative));
        }

        UpdatePendingResources();
    }

    private void UpdatePendingResources()
    {
        if (_pendingResources.Count == 0)
            return;

        var toPrepare = new List<(string ResourcePath, ResPath ResPath)>();

        foreach (var (resourcePath, resPath) in _pendingResources)
        {
            if (IsResourceLoaded(resourcePath) || _preparingResources.Contains(resourcePath))
                continue;

            if (IsResourceComplete(resPath))
            {
                toPrepare.Add((resourcePath, resPath));
                continue;
            }

            if (!_requestedResources.Contains(resourcePath))
                RequestResource(resourcePath);
        }

        foreach (var (resourcePath, resPath) in toPrepare)
        {
            StartPreparingResource(resourcePath, resPath);
        }
    }

    private void StartPreparingResource(string resourcePath, ResPath resPath)
    {
        if (IsResourceLoaded(resourcePath))
        {
            _pendingResources.Remove(resourcePath);
            return;
        }

        if (!_preparingResources.Add(resourcePath))
            return;

        _prepareRequests.Enqueue(new PreparationRequest(resourcePath, resPath, _sessionGeneration));
        TryStartNextPreparation();
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

    private void OnRunLevelChanged(object? sender, RunLevelChangedEventArgs e)
    {
        if (e.OldLevel is not (ClientRunLevel.Connected or ClientRunLevel.InGame))
            return;

        if (e.NewLevel is ClientRunLevel.Connected or ClientRunLevel.InGame)
            return;

        _sawmill.Debug("Clearing NetTextures state on disconnect");
        ResetState();
    }

    private void ResetState()
    {
        _sessionGeneration++;

        _sessionCts.Cancel();
        _sessionCts.Dispose();
        _sessionCts = new CancellationTokenSource();

        _requestedResources.Clear();
        _pendingResources.Clear();
        _preparingResources.Clear();
        _failedResources.Clear();
        _netTexturesContentRoot.Clear();
        _prepareRequests.Clear();
        _prepareWorkerRunning = false;
        _activePrepareRequestId = 0;

        while (_preparedUploads.Count > 0)
        {
            var upload = _preparedUploads.Dequeue();
            upload.Dispose();
        }

        foreach (var assembly in _fallbackChunkAssemblies.Values)
        {
            assembly.Dispose();
        }

        _fallbackChunkAssemblies.Clear();

        foreach (var (_, texture) in _loadedTextures)
        {
            texture.Dispose();
        }

        foreach (var (_, rsi) in _loadedRsis)
        {
            rsi.Dispose();
        }

        _loadedTextures.Clear();
        _loadedRsis.Clear();
    }

    private bool IsResourceLoaded(string resourcePath)
    {
        return _loadedTextures.ContainsKey(resourcePath) || _loadedRsis.ContainsKey(resourcePath);
    }

    private bool IsResourceComplete(ResPath resourcePath)
    {
        var relativePath = resourcePath.ToRelativePath();

        if (IsRsiPath(resourcePath))
            return CheckRsiFilesComplete(relativePath);

        var uploadedPath = (new ResPath(UploadedPrefix) / relativePath).ToRootedPath();
        return _resourceManager.ContentFileExists(uploadedPath);
    }

    private void ReceiveNetTexturesTransferWorker(Stream stream, int generation)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            using (stream)
            {
                var files = ReadTransferStream(stream);
                var totalSize = 0L;
                foreach (var (_, data) in files)
                {
                    totalSize += data.Length;
                }

                _taskManager.RunOnMainThread(() =>
                {
                    if (generation != _sessionGeneration)
                        return;

                    PublishFiles(files);
                });

                var totalTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
                _sawmill.Info($"[NetTextures] Received {files.Count} files ({ByteHelpers.FormatBytes(totalSize)}) via transfer in {totalTime:F0}ms");
            }
        }
        catch (Exception e)
        {
            _sawmill.Error($"Error while receiving NetTextures transfer: {e}");
        }
    }

    private void TryStartNextPreparation()
    {
        if (_prepareWorkerRunning)
            return;

        while (_prepareRequests.Count > 0)
        {
            var request = _prepareRequests.Dequeue();
            if (request.Generation != _sessionGeneration)
                continue;

            if (!_preparingResources.Contains(request.ResourcePath))
                continue;

            _prepareWorkerRunning = true;
            var requestId = ++_nextPrepareRequestId;
            _activePrepareRequestId = requestId;
            var cancellationToken = _sessionCts.Token;
            _ = Task.Run(() => PrepareResourceWorker(request, requestId, cancellationToken));
            return;
        }
    }

    private void PrepareResourceWorker(PreparationRequest request, int requestId, CancellationToken cancellationToken)
    {
        PreparedUploadJob? upload = null;
        Exception? error = null;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (IsRsiPath(request.ResPath))
            {
                var prepared = DecodeRsi(request.ResPath, cancellationToken);
                upload = new PreparedRsiUploadJob(request.ResourcePath, request.Generation, prepared);
            }
            else
            {
                var prepared = DecodeTexture(request.ResPath, cancellationToken);
                upload = new PreparedTextureUploadJob(request.ResourcePath, request.Generation, prepared);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            error = ex;
        }

        _taskManager.RunOnMainThread(() => FinishPreparationWorker(request, requestId, upload, error));
    }

    private void FinishPreparationWorker(
        PreparationRequest request,
        int requestId,
        PreparedUploadJob? upload,
        Exception? error)
    {
        if (_activePrepareRequestId == requestId)
        {
            _prepareWorkerRunning = false;
            _activePrepareRequestId = 0;
        }

        if (upload != null)
        {
            if (request.Generation == _sessionGeneration)
                _preparedUploads.Enqueue(upload);
            else
                upload.Dispose();
        }
        else if (error != null && request.Generation == _sessionGeneration)
        {
            MarkResourceFailed(request.ResourcePath, error.Message);
        }

        TryStartNextPreparation();
    }
}

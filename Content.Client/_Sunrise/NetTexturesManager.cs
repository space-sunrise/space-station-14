using System.IO;
using System.Threading;
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

namespace Content.Client._Sunrise;

/// <summary>
/// Coordinates client-side loading of server-provided textures and RSI resources.
/// </summary>
public sealed partial class NetTexturesManager
{
    #region Constants
    private const string TransferKeyNetTextures = "TransferKeyNetTextures";
    private const string UploadedPrefix = "/Uploaded";
    private const int MinTransferPublishBudgetBytes = 512 * 1024;
    private const int MaxTransferPublishBudgetBytes = 8 * 1024 * 1024;
    private const int TransferPublishBytesPerSecond = 64 * 1024 * 1024;
    #endregion

    #region Dependencies
    [Dependency] private readonly IClientNetManager _netManager = default!;
    [Dependency] private readonly IResourceManager _resourceManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IBaseClient _baseClient = default!;
    [Dependency] private readonly ITransferManager _transferManager = default!;
    [Dependency] private readonly ITaskManager _taskManager = default!;
    [Dependency] private readonly IClyde _clyde = default!;
    #endregion

    #region State
    private readonly MemoryContentRoot _netTexturesContentRoot = new();
    private readonly HashSet<string> _requestedResources = new();
    private readonly Dictionary<string, ResPath> _pendingResources = new();
    private readonly HashSet<string> _preparingResources = new();
    private readonly HashSet<string> _failedResources = new();
    private readonly Dictionary<string, LoadedTextureEntry> _loadedTextures = new();
    private readonly Dictionary<string, LoadedRsiEntry> _loadedRsis = new();
    private readonly Queue<PreparedUploadJob> _preparedUploads = new();
    private readonly Queue<TransferPublishBatch> _pendingTransferBatches = new();
    private readonly Dictionary<ResPath, FallbackChunkAssembly> _fallbackChunkAssemblies = new();
    private readonly Queue<PreparationRequest> _prepareRequests = new();
    private readonly List<(string ResourceKey, ResPath ResPath)> _resourcesReadyToPrepare = new();
    private readonly Dictionary<ResPath, RsiCompletenessEntry> _rsiCompleteness = new();

    private CancellationTokenSource _sessionCts = new();
    private int _sessionGeneration;
    private int _activePrepareRequestId;
    private int _nextPrepareRequestId;
    private bool _prepareWorkerRunning;
    private bool _initialized;
    private ISawmill _sawmill = default!;
    #endregion

    #region Helpers
    /// <summary>
    /// Reads the current session generation with interlocked semantics for background-worker handoff.
    /// </summary>
    private int ReadSessionGeneration()
    {
        return Interlocked.CompareExchange(ref _sessionGeneration, 0, 0);
    }

    /// <summary>
    /// Advances the reconnect generation and returns the new value.
    /// </summary>
    private int AdvanceSessionGeneration()
    {
        return Interlocked.Increment(ref _sessionGeneration);
    }

    /// <summary>
    /// Returns whether an exception is an expected RSI metadata parse failure without hard-linking banned types.
    /// </summary>
    private static bool IsHandledRsiMetadataException(Exception ex)
    {
        for (var type = ex.GetType(); type != null; type = type.BaseType)
        {
            if (type == typeof(InvalidDataException))
                return true;

            if (type.FullName
                is "YamlDotNet.Core.YamlException"
                or "System.FormatException"
                or "System.OverflowException")
            {
                return true;
            }
        }

        return false;
    }
    #endregion

    #region Events
    /// <summary>
    /// Raised after a network resource becomes ready for consumer use.
    /// </summary>
    public event Action<string>? ResourceLoaded;
    #endregion

    #region Lifecycle
    /// <summary>
    /// Registers transfer handlers and mounts the in-memory uploaded resource root.
    /// </summary>
    public void Initialize()
    {
        if (_initialized)
            return;

        _initialized = true;
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

    /// <summary>
    /// Advances pending resource preparation and staged GPU upload work.
    /// </summary>
    /// <param name="frameTime">The elapsed frame time in seconds.</param>
    public void Update(float frameTime)
    {
        lock (_pendingTransferBatches)
        {
            if (_pendingTransferBatches.Count != 0)
                ProcessPendingTransferBatches(frameTime);
        }

        if (_pendingResources.Count != 0)
            UpdatePendingResources();

        if (_preparedUploads.Count != 0)
            ProcessPreparedUploads(frameTime);
    }
    #endregion
}

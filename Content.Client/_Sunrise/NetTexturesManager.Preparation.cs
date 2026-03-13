using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Content.Shared._Sunrise.NetTextures;
using Robust.Client;
using Robust.Shared.Utility;

namespace Content.Client._Sunrise;

public sealed partial class NetTexturesManager
{
    #region Resource Tracking
    /// <summary>
    /// Revisits all pending resources and advances any entries that became complete since the last update.
    /// </summary>
    private void UpdatePendingResources()
    {
        if (_pendingResources.Count == 0)
            return;

        _resourcesReadyToPrepare.Clear();

        foreach (var (resourceKey, resPath) in _pendingResources)
        {
            if (IsResourceLoaded(resourceKey) || _preparingResources.Contains(resourceKey))
                continue;

            if (!TryCheckResourceComplete(resourceKey, resPath, out var isComplete))
                continue;

            if (isComplete)
            {
                _resourcesReadyToPrepare.Add((resourceKey, resPath));
                continue;
            }

            if (!_requestedResources.Contains(resourceKey))
                RequestResource(resourceKey);
        }

        foreach (var (resourceKey, resPath) in _resourcesReadyToPrepare)
        {
            StartPreparingResource(resourceKey, resPath);
        }

        _resourcesReadyToPrepare.Clear();
    }

    /// <summary>
    /// Queues a complete resource for decode and upload preparation.
    /// </summary>
    /// <param name="resourceKey">The normalized resource key.</param>
    /// <param name="resPath">The normalized resource path.</param>
    private void StartPreparingResource(string resourceKey, ResPath resPath)
    {
        if (IsResourceLoaded(resourceKey))
        {
            _pendingResources.Remove(resourceKey);
            return;
        }

        if (!_preparingResources.Add(resourceKey))
            return;

        _prepareRequests.Enqueue(new PreparationRequest(resourceKey, resPath, ReadSessionGeneration()));
        TryStartNextPreparation();
    }

    /// <summary>
    /// Sends a one-time network request for a resource that is not yet present locally.
    /// </summary>
    /// <param name="resourceKey">The normalized requested resource path.</param>
    private void RequestResource(string resourceKey)
    {
        if (_requestedResources.Contains(resourceKey))
            return;

        if (!_netManager.IsConnected)
        {
            _sawmill.Debug($"Cannot request resource {resourceKey}: client not connected to server");
            return;
        }

        _requestedResources.Add(resourceKey);

        var msg = new RequestNetworkResourceMessage
        {
            ResourcePath = resourceKey
        };

        _netManager.ClientSendMessage(msg);
    }

    /// <summary>
    /// Checks whether the resource already has a fully prepared ready-to-use representation.
    /// </summary>
    /// <param name="resourceKey">The normalized resource key.</param>
    /// <returns><see langword="true"/> if the resource is already loaded.</returns>
    private bool IsResourceLoaded(string resourceKey)
    {
        return _loadedTextures.ContainsKey(resourceKey) || _loadedRsis.ContainsKey(resourceKey);
    }

    /// <summary>
    /// Checks whether the raw uploaded files required for a resource are present locally.
    /// </summary>
    /// <remarks>
    /// For RSI resources this requires full directory completeness, not just the presence of <c>meta.json</c>.
    /// </remarks>
    /// <param name="resourcePath">The normalized resource path.</param>
    /// <returns><see langword="true"/> if the raw uploaded resource is complete enough to prepare.</returns>
    private bool IsResourceComplete(ResPath resourcePath)
    {
        var relativePath = resourcePath.ToRelativePath();

        if (IsRsiPath(resourcePath))
            return CheckRsiFilesComplete(relativePath);

        var uploadedPath = (new ResPath(UploadedPrefix) / relativePath).ToRootedPath();
        return _resourceManager.ContentFileExists(uploadedPath);
    }

    /// <summary>
    /// Checks resource completeness and converts corrupt payloads into terminal failures.
    /// </summary>
    /// <param name="resourceKey">The normalized resource key.</param>
    /// <param name="resPath">The normalized resource path.</param>
    /// <param name="isComplete">Whether the resource files are complete enough to prepare.</param>
    /// <returns>
    /// <see langword="true"/> when the completeness check succeeded; otherwise <see langword="false"/> after the
    /// resource has been marked failed.
    /// </returns>
    private bool TryCheckResourceComplete(string resourceKey, ResPath resPath, out bool isComplete)
    {
        try
        {
            isComplete = IsResourceComplete(resPath);
            return true;
        }
        catch (InvalidDataException ex)
        {
            MarkResourceFailed(resourceKey, ex.Message);
            isComplete = false;
            return false;
        }
    }
    #endregion

    #region Connection State
    /// <summary>
    /// Clears all session-local NetTextures state when the client leaves the connected game flow.
    /// </summary>
    /// <param name="sender">The event source.</param>
    /// <param name="e">The run level transition.</param>
    private void OnRunLevelChanged(object? sender, RunLevelChangedEventArgs e)
    {
        if (e.OldLevel is not (ClientRunLevel.Connected or ClientRunLevel.InGame))
            return;

        if (e.NewLevel is ClientRunLevel.Connected or ClientRunLevel.InGame)
            return;

        _sawmill.Debug("Clearing NetTextures state on disconnect");
        ResetState();
    }

    /// <summary>
    /// Resets requests, partial transfers, staged uploads, and loaded resources for the current session.
    /// </summary>
    /// <remarks>
    /// This method is the reconnect safety boundary. Anything that could affect a later connect attempt must be
    /// cleared here, including fallback chunk assemblies and partially published uploaded content.
    /// </remarks>
    private void ResetState()
    {
        AdvanceSessionGeneration();

        _sessionCts.Cancel();
        _sessionCts.Dispose();
        _sessionCts = new CancellationTokenSource();

        _requestedResources.Clear();
        _pendingResources.Clear();
        _preparingResources.Clear();
        _failedResources.Clear();
        _netTexturesContentRoot.Clear();
        _prepareRequests.Clear();
        _resourcesReadyToPrepare.Clear();
        _rsiCompleteness.Clear();
        _prepareWorkerRunning = false;
        _activePrepareRequestId = 0;

        lock (_pendingTransferBatches)
        {
            _pendingTransferBatches.Clear();
        }

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
    #endregion

    #region Preparation Queue
    /// <summary>
    /// Starts the next queued preparation job if the single preparation worker is idle.
    /// </summary>
    private void TryStartNextPreparation()
    {
        if (_prepareWorkerRunning)
            return;

        var currentGeneration = ReadSessionGeneration();

        while (_prepareRequests.Count > 0)
        {
            var request = _prepareRequests.Dequeue();
            if (request.Generation != currentGeneration)
                continue;

            if (!_preparingResources.Contains(request.ResourceKey))
                continue;

            _prepareWorkerRunning = true;
            var requestId = ++_nextPrepareRequestId;
            _activePrepareRequestId = requestId;
            var cancellationToken = _sessionCts.Token;
            _ = Task.Run(() => PrepareResourceWorker(request, requestId, cancellationToken));
            return;
        }
    }

    /// <summary>
    /// Decodes one queued resource on a background worker and converts it into a staged upload job.
    /// </summary>
    /// <param name="request">The resource to prepare.</param>
    /// <param name="requestId">The unique identifier of the active worker request.</param>
    /// <param name="cancellationToken">The current session cancellation token.</param>
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
                upload = new PreparedRsiUploadJob(request.ResourceKey, request.Generation, prepared);
            }
            else
            {
                var prepared = DecodeTexture(request.ResPath, cancellationToken);
                upload = new PreparedTextureUploadJob(request.ResourceKey, request.Generation, prepared);
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

    /// <summary>
    /// Finalizes the worker result on the main thread and either enqueues the staged upload or records a failure.
    /// </summary>
    /// <param name="request">The request completed by the worker.</param>
    /// <param name="requestId">The unique identifier of the active worker request.</param>
    /// <param name="upload">The staged upload job produced by the worker, if any.</param>
    /// <param name="error">The decode failure, if the worker did not produce an upload.</param>
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

        var currentGeneration = ReadSessionGeneration();

        if (upload != null)
        {
            if (request.Generation == currentGeneration)
                _preparedUploads.Enqueue(upload);
            else
                upload.Dispose();
        }
        else if (error != null && request.Generation == currentGeneration)
        {
            MarkResourceFailed(request.ResourceKey, error.Message);
        }

        TryStartNextPreparation();
    }
    #endregion
}

using System.Collections.Generic;
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

    /// <summary>
    /// Queues a complete resource for decode and upload preparation.
    /// </summary>
    /// <param name="resourcePath">The consumer-facing resource path.</param>
    /// <param name="resPath">The normalized resource path.</param>
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

    /// <summary>
    /// Sends a one-time network request for a resource that is not yet present locally.
    /// </summary>
    /// <param name="resourcePath">The requested resource path.</param>
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
    /// Checks whether the resource already has a fully prepared ready-to-use representation.
    /// </summary>
    /// <param name="resourcePath">The consumer-facing resource path.</param>
    /// <returns><see langword="true"/> if the resource is already loaded.</returns>
    private bool IsResourceLoaded(string resourcePath)
    {
        return _loadedTextures.ContainsKey(resourcePath) || _loadedRsis.ContainsKey(resourcePath);
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
    #endregion

    #region Preparation Queue
    /// <summary>
    /// Starts the next queued preparation job if the single preparation worker is idle.
    /// </summary>
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
    #endregion
}

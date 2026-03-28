using System.Buffers.Binary;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Content.Shared._Sunrise.NetTextures;
using Robust.Shared.Network.Transfer;
using Robust.Shared.Upload;
using Robust.Shared.Utility;
using ByteHelpers = Robust.Shared.Utility.ByteHelpers;

namespace Content.Client._Sunrise;

public sealed partial class NetTexturesManager
{
    #region Transfer Intake
    /// <summary>
    /// Starts asynchronous processing for a high-bandwidth NetTextures transfer.
    /// </summary>
    /// <remarks>
    /// The transfer callback can run during a connect-sensitive window, so the stream is handed off to a
    /// background worker before any file parsing begins.
    /// </remarks>
    /// <param name="transfer">The transfer payload received from the server.</param>
    private void ReceiveNetTexturesTransfer(TransferReceivedEvent transfer)
    {
        var generation = ReadSessionGeneration();

        _ = Task.Run(() => ReceiveNetTexturesTransferWorker(transfer.DataStream, generation));
    }

#pragma warning disable CS0618
    /// <summary>
    /// Accepts the legacy whole-file fallback message and publishes it through the normal client pipeline.
    /// </summary>
    /// <param name="message">The legacy upload message containing a single resource file.</param>
    private void ReceiveFallbackUpload(NetworkResourceUploadMessage message)
#pragma warning restore CS0618
    {
        var files = new List<(ResPath Relative, byte[] Data)>(1)
        {
            (message.RelativePath, message.Data)
        };

        PublishFiles(files);
    }

    /// <summary>
    /// Accepts one chunk from the chunked fallback transport and assembles it into a complete uploaded file.
    /// </summary>
    /// <remarks>
    /// Assemblies are keyed by normalized relative path and are discarded on session reset, so partial fallback
    /// state cannot leak into a later reconnect attempt.
    /// </remarks>
    /// <param name="message">The incoming fallback chunk.</param>
    internal void ReceiveFallbackChunk(NetTextureResourceChunkMessage message)
    {
        var totalChunks = message.TotalChunks;
        var chunkIndex = message.ChunkIndex;
        var totalLength = message.TotalLength;
        var chunkOffset = message.ChunkOffset;
        var chunkLength = message.Data.Length;
        var chunkEnd = (long) chunkOffset + chunkLength;

        if (totalChunks <= 0 ||
            totalChunks > NetTextureConstants.MaxFallbackChunkCount ||
            chunkIndex < 0 ||
            chunkIndex >= totalChunks)
        {
            _sawmill.Warning(
                $"Rejected malformed NetTextures fallback chunk for {message.RelativePath}: chunk {(long) chunkIndex + 1}/{totalChunks}, limit {NetTextureConstants.MaxFallbackChunkCount}");
            return;
        }

        if (totalLength < 0 || (uint) totalLength > NetTextureConstants.MaxTransferFileSize)
        {
            _sawmill.Warning(
                $"Rejected malformed NetTextures fallback chunk length for {message.RelativePath}: total {totalLength}, limit {NetTextureConstants.MaxTransferFileSize}");
            return;
        }

        if (chunkLength > NetTextureConstants.MaxChunkSize ||
            chunkOffset < 0 ||
            chunkLength > totalLength ||
            chunkEnd > totalLength)
        {
            _sawmill.Warning(
                $"Rejected malformed NetTextures fallback chunk bounds for {message.RelativePath}: offset {chunkOffset}, length {chunkLength}, total {totalLength}");
            return;
        }

        var relativePath = new ResPath(message.RelativePath).Clean().ToRelativePath();

        if (!_fallbackChunkAssemblies.TryGetValue(relativePath, out var assembly) ||
            assembly.TotalChunks != totalChunks ||
            assembly.TotalLength != totalLength)
        {
            assembly?.Dispose();
            assembly = new FallbackChunkAssembly(totalChunks, totalLength);
            _fallbackChunkAssemblies[relativePath] = assembly;
        }

        assembly.StoreChunk(chunkIndex, chunkOffset, message.Data);

        if (!assembly.IsComplete)
            return;

        _fallbackChunkAssemblies.Remove(relativePath);
        try
        {
            var files = new List<(ResPath Relative, byte[] Data)>(1)
            {
                (relativePath, assembly.TakeCompletedData())
            };

            PublishFiles(files);
        }
        finally
        {
            assembly.Dispose();
        }
    }

    /// <summary>
    /// Publishes received raw files into the mounted in-memory uploaded root and refreshes pending consumers.
    /// </summary>
    /// <param name="files">The files to publish under <c>/Uploaded</c>.</param>
    internal void PublishFiles(List<(ResPath Relative, byte[] Data)> files)
    {
        PublishFiles(files, updatePendingResources: true);
    }

    /// <summary>
    /// Publishes received raw files into the mounted in-memory uploaded root and optionally refreshes pending consumers.
    /// </summary>
    /// <param name="files">The files to publish under <c>/Uploaded</c>.</param>
    /// <param name="updatePendingResources">Whether to revisit pending resource readiness immediately.</param>
    internal void PublishFiles(List<(ResPath Relative, byte[] Data)> files, bool updatePendingResources)
    {
        foreach (var (relative, data) in files)
        {
            _sawmill.Verbose($"Storing NetTexture: {relative} ({ByteHelpers.FormatBytes(data.Length)})");
            _netTexturesContentRoot.AddOrUpdateFile(relative, data);
            TrackPublishedFile(relative, data);
            _failedResources.Remove(GetUploadedResourcePath(relative));
        }

        if (updatePendingResources)
            UpdatePendingResources();
    }
    #endregion

    #region Transfer Workers
    /// <summary>
    /// Parses a transfer stream on a worker thread and marshals publication back to the main thread.
    /// </summary>
    /// <param name="stream">The transfer stream returned by the HBT subsystem.</param>
    /// <param name="generation">The session generation captured when the transfer started.</param>
    private void ReceiveNetTexturesTransferWorker(Stream stream, int generation)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            using (stream)
            {
                var (fileCount, totalSize) = ReadTransferStream(stream, generation);
                if (fileCount > 0)
                    _taskManager.RunOnMainThread(() => ProcessPendingTransferBatches(0f));

                var totalTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
                _sawmill.Info($"[NetTextures] Received {fileCount} files ({ByteHelpers.FormatBytes(totalSize)}) via transfer in {totalTime:F0}ms");
            }
        }
        catch (Exception e)
        {
            _sawmill.Error($"Error while receiving NetTextures transfer: {e}");
        }
    }
    #endregion

    #region Transfer Parsing
    /// <summary>
    /// Reads the NetTextures transfer stream into publish batches that can be drained incrementally on the main thread.
    /// </summary>
    /// <param name="stream">The transfer stream to parse.</param>
    /// <param name="generation">The session generation captured when the transfer started.</param>
    /// <returns>The total file count and total byte size parsed from the stream.</returns>
    private (int FileCount, long TotalBytes) ReadTransferStream(Stream stream, int generation)
    {
        var files = new List<(ResPath Relative, byte[] Data)>();
        var lengthBytes = new byte[4];
        var continueByte = new byte[1];
        var totalSize = 0L;
        var totalFiles = 0;
        var batchBytes = 0;

        while (true)
        {
            ReadExactly(stream, lengthBytes);
            var pathLength = BinaryPrimitives.ReadUInt32LittleEndian(lengthBytes);
            if (pathLength > int.MaxValue)
                throw new InvalidDataException($"NetTextures transfer path length is too large: {pathLength}");
            if (pathLength > NetTextureConstants.MaxTransferPathLength)
                throw new InvalidDataException($"NetTextures transfer path length exceeds protocol limit: {pathLength}");

            ReadExactly(stream, lengthBytes);
            var dataLength = BinaryPrimitives.ReadUInt32LittleEndian(lengthBytes);
            if (dataLength > int.MaxValue)
                throw new InvalidDataException($"NetTextures transfer file length is too large: {dataLength}");
            if (dataLength > NetTextureConstants.MaxTransferFileSize)
                throw new InvalidDataException($"NetTextures transfer file length exceeds protocol limit: {dataLength}");
            if ((ulong) pathLength + dataLength > NetTextureConstants.MaxTransferPayloadLength)
            {
                throw new InvalidDataException(
                    $"NetTextures transfer entry payload exceeds protocol limit: path={pathLength}, data={dataLength}");
            }

            var pathData = new byte[(int) pathLength];
            ReadExactly(stream, pathData);

            var data = new byte[(int) dataLength];
            ReadExactly(stream, data);

            files.Add((new ResPath(Encoding.UTF8.GetString(pathData)), data));
            totalFiles++;
            totalSize += data.Length;
            batchBytes += data.Length;

            if (batchBytes >= MaxTransferPublishBudgetBytes)
            {
                EnqueueTransferPublishBatch(new TransferPublishBatch(generation, files, batchBytes));
                files = new List<(ResPath Relative, byte[] Data)>();
                batchBytes = 0;
            }

            ReadExactly(stream, continueByte);
            if (continueByte[0] == 0)
                break;
        }

        if (files.Count > 0)
            EnqueueTransferPublishBatch(new TransferPublishBatch(generation, files, batchBytes));

        return (totalFiles, totalSize);
    }

    /// <summary>
    /// Reads the exact number of bytes required for one transfer field.
    /// </summary>
    /// <param name="stream">The input stream.</param>
    /// <param name="buffer">The destination buffer that must be filled completely.</param>
    private static void ReadExactly(Stream stream, byte[] buffer)
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var read = stream.Read(buffer, offset, buffer.Length - offset);
            if (read == 0)
                throw new InvalidDataException("Unexpected end of NetTextures transfer stream");

            offset += read;
        }
    }
    #endregion

    #region Transfer Publication
    /// <summary>
    /// Queues one parsed transfer batch for later publication on the main thread.
    /// </summary>
    /// <param name="batch">The parsed transfer batch.</param>
    private void EnqueueTransferPublishBatch(TransferPublishBatch batch)
    {
        lock (_pendingTransferBatches)
        {
            _pendingTransferBatches.Enqueue(batch);
        }
    }

    /// <summary>
    /// Publishes a bounded amount of already parsed transfer data on the main thread.
    /// </summary>
    /// <param name="frameTime">The current frame time used to derive a per-frame publish budget.</param>
    private void ProcessPendingTransferBatches(float frameTime)
    {
        var budgetRemaining = Math.Clamp(
            (int) (frameTime * TransferPublishBytesPerSecond),
            MinTransferPublishBudgetBytes,
            MaxTransferPublishBudgetBytes);
        var publishedAny = false;

        while (true)
        {
            TransferPublishBatch? batch;
            lock (_pendingTransferBatches)
            {
                if (_pendingTransferBatches.Count == 0 || (publishedAny && budgetRemaining <= 0))
                    break;

                batch = _pendingTransferBatches.Dequeue();
            }

            if (batch.Generation != ReadSessionGeneration())
                continue;

            PublishFiles(batch.Files, updatePendingResources: false);
            budgetRemaining -= Math.Max(1, batch.TotalBytes);
            publishedAny = true;
        }

        if (publishedAny)
            UpdatePendingResources();
    }
    #endregion

    #region Completeness Tracking
    /// <summary>
    /// Updates incremental completeness state for uploaded RSI resources as individual files arrive.
    /// </summary>
    /// <param name="relativePath">The uploaded relative file path.</param>
    /// <param name="data">The uploaded file bytes.</param>
    private void TrackPublishedFile(ResPath relativePath, byte[] data)
    {
        if (!TryGetRsiFile(relativePath, out var rsiRelativePath, out var fileName))
            return;

        if (!_rsiCompleteness.TryGetValue(rsiRelativePath, out var completeness))
        {
            completeness = new RsiCompletenessEntry();
            _rsiCompleteness[rsiRelativePath] = completeness;
        }

        completeness.MarkPresent(fileName);

        if (!fileName.Equals("meta.json", StringComparison.Ordinal))
            return;

        try
        {
            using var metaStream = new MemoryStream(data, writable: false);
            completeness.SetMetadata(LoadRsiMetadata(metaStream));
        }
        catch (Exception ex)
        {
            if (!IsHandledRsiMetadataException(ex))
                throw;

            var reason = $"Failed to parse RSI metadata for {rsiRelativePath}: {ex.Message}";
            _sawmill.Debug(reason);
            completeness.SetMetadataFailure(reason);
        }
    }

    /// <summary>
    /// Determines whether an uploaded file belongs to an RSI directory and returns its directory-local file name.
    /// </summary>
    /// <param name="relativePath">The uploaded relative file path.</param>
    /// <param name="rsiRelativePath">The uploaded RSI directory path when the method returns <see langword="true"/>.</param>
    /// <param name="fileName">The file name inside the RSI directory.</param>
    /// <returns><see langword="true"/> if the file belongs to an RSI directory.</returns>
    private static bool TryGetRsiFile(ResPath relativePath, out ResPath rsiRelativePath, out string fileName)
    {
        fileName = relativePath.Filename;
        rsiRelativePath = relativePath.Directory.ToRelativePath();

        if (fileName == "." || !IsRsiPath(rsiRelativePath.ToRootedPath()))
        {
            rsiRelativePath = default;
            fileName = string.Empty;
            return false;
        }

        return true;
    }
    #endregion
}

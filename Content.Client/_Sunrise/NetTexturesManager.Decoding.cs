using System.Collections.Generic;
using System.IO;
using System.Threading;
using Robust.Shared.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Content.Client._Sunrise;

public sealed partial class NetTexturesManager
{
    #region Upload Completion
    private const int MaxUploadStepsPerFrame = 2;

    /// <summary>
    /// Commits a fully uploaded texture into the ready resource map and notifies listeners.
    /// </summary>
    /// <param name="resourcePath">The consumer-facing resource path.</param>
    /// <param name="loadedTexture">The uploaded texture entry.</param>
    private void FinishPreparedTexture(string resourcePath, LoadedTextureEntry loadedTexture)
    {
        _preparingResources.Remove(resourcePath);
        _pendingResources.Remove(resourcePath);

        if (_loadedTextures.Remove(resourcePath, out var oldTexture))
            oldTexture.Dispose();

        _loadedTextures[resourcePath] = loadedTexture;
        ResourceLoaded?.Invoke(resourcePath);
    }

    /// <summary>
    /// Commits a fully uploaded RSI animation set into the ready resource map and notifies listeners.
    /// </summary>
    /// <param name="resourcePath">The consumer-facing resource path.</param>
    /// <param name="loadedRsi">The uploaded RSI entry.</param>
    private void FinishPreparedRsi(string resourcePath, LoadedRsiEntry loadedRsi)
    {
        _preparingResources.Remove(resourcePath);
        _pendingResources.Remove(resourcePath);

        if (_loadedRsis.Remove(resourcePath, out var oldRsi))
            oldRsi.Dispose();

        _loadedRsis[resourcePath] = loadedRsi;
        ResourceLoaded?.Invoke(resourcePath);
    }

    /// <summary>
    /// Processes a bounded number of staged GPU upload steps for the current frame.
    /// </summary>
    /// <remarks>
    /// Upload work is intentionally spread across frames so large RSI resources do not monopolize the main thread.
    /// </remarks>
    private void ProcessPreparedUploads()
    {
        var stepsRemaining = MaxUploadStepsPerFrame;

        while (stepsRemaining > 0 && _preparedUploads.Count > 0)
        {
            var upload = _preparedUploads.Peek();

            if (upload.Generation != _sessionGeneration)
            {
                _preparedUploads.Dequeue().Dispose();
                continue;
            }

            try
            {
                var completed = upload.ProcessStep(this, _sessionCts.Token);
                stepsRemaining--;

                if (!completed)
                    continue;

                upload.Commit(this);
                _preparedUploads.Dequeue();
            }
            catch (OperationCanceledException)
            {
                _preparedUploads.Dequeue().Dispose();
                _preparingResources.Remove(upload.ResourcePath);
            }
            catch (Exception ex)
            {
                _preparedUploads.Dequeue().Dispose();
                MarkResourceFailed(upload.ResourcePath, ex.Message);
            }
        }
    }

    /// <summary>
    /// Records a resource failure and prevents it from being treated as ready.
    /// </summary>
    /// <param name="resourcePath">The failing resource path.</param>
    /// <param name="reason">The failure reason used for logging.</param>
    private void MarkResourceFailed(string resourcePath, string reason)
    {
        _preparingResources.Remove(resourcePath);
        _pendingResources.Remove(resourcePath);
        _failedResources.Add(resourcePath);
        _sawmill.Warning($"Failed to prepare NetTexture {resourcePath}: {reason}");
    }
    #endregion

    #region Resource Decoding
    /// <summary>
    /// Verifies that an uploaded RSI directory contains all files required for safe decode.
    /// </summary>
    /// <param name="relativePath">The relative uploaded RSI path.</param>
    /// <returns><see langword="true"/> if all required RSI files are present.</returns>
    private bool CheckRsiFilesComplete(ResPath relativePath)
    {
        try
        {
            var uploadedPath = (new ResPath(UploadedPrefix) / relativePath).ToRootedPath();
            var metaPath = (uploadedPath / "meta.json").ToRootedPath();

            if (!_resourceManager.TryContentFileRead(metaPath, out var metaStream))
                return false;

            using (metaStream)
            {
                var metadata = LoadRsiMetadata(metaStream);
                if (metadata.States.Length == 0)
                    return false;

                foreach (var state in metadata.States)
                {
                    if (string.IsNullOrWhiteSpace(state.Name))
                        return false;

                    var pngPath = (uploadedPath / $"{state.Name}.png").ToRootedPath();
                    if (!_resourceManager.ContentFileExists(pngPath))
                        return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _sawmill.Debug($"Error checking RSI completeness for {relativePath}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Decodes a non-RSI uploaded image into an intermediate texture payload.
    /// </summary>
    /// <param name="resourcePath">The normalized uploaded resource path.</param>
    /// <param name="cancellationToken">The current session cancellation token.</param>
    /// <returns>The decoded texture payload ready for staged upload.</returns>
    private PreparedTexture DecodeTexture(ResPath resourcePath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var uploadedPath = GetUploadedPath(resourcePath.ToString());
        using var stream = _resourceManager.ContentFileRead(uploadedPath);
        var image = Image.Load<Rgba32>(stream);
        return new PreparedTexture(image);
    }

    /// <summary>
    /// Decodes an uploaded RSI directory into per-frame image payloads ready for staged upload.
    /// </summary>
    /// <remarks>
    /// The decode path validates metadata, image dimensions, frame references, and direction counts before any
    /// uploaded state is exposed to consumers.
    /// </remarks>
    /// <param name="resourcePath">The normalized uploaded RSI path.</param>
    /// <param name="cancellationToken">The current session cancellation token.</param>
    /// <returns>The prepared RSI payload ready for staged upload.</returns>
    private PreparedRsi DecodeRsi(ResPath resourcePath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var uploadedPath = GetUploadedPath(resourcePath.ToString());
        var metaPath = (uploadedPath / "meta.json").ToRootedPath();

        using var metaStream = _resourceManager.ContentFileRead(metaPath);
        var metadata = LoadRsiMetadata(metaStream);

        if (metadata.States.Length == 0)
            throw new InvalidDataException($"RSI metadata for {resourcePath} is incomplete");

        var frameSize = metadata.Size;
        if (frameSize.X <= 0 || frameSize.Y <= 0)
            throw new InvalidDataException($"RSI metadata for {resourcePath} has invalid frame size {frameSize}");

        var states = new List<PreparedRsiState>(metadata.States.Length);
        foreach (var state in metadata.States)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(state.Name))
                throw new InvalidDataException($"RSI metadata for {resourcePath} contains an empty state name");

            var dirCount = state.Directions ?? 1;
            if (dirCount is not (1 or 4 or 8))
                throw new InvalidDataException($"RSI state {state.Name} in {resourcePath} has invalid direction count {dirCount}");

            var delays = NormalizeDelays(state.Delays, dirCount);
            var (foldedDelays, foldedIndices) = FoldDelays(delays);

            var pngPath = (uploadedPath / $"{state.Name}.png").ToRootedPath();
            using var stateStream = _resourceManager.ContentFileRead(pngPath);
            var image = Image.Load<Rgba32>(stateStream);

            if (image.Width % frameSize.X != 0 || image.Height % frameSize.Y != 0)
                throw new InvalidDataException($"RSI state {state.Name} in {resourcePath} has invalid image size {image.Width}x{image.Height}");

            var sourceColumns = image.Width / frameSize.X;
            var sourceRows = image.Height / frameSize.Y;
            var sourceFrameCount = sourceColumns * sourceRows;

            var uniqueIndices = new HashSet<int>();
            var frames = new List<PreparedRsiFrame>();

            foreach (var dirIndices in foldedIndices)
            {
                foreach (var index in dirIndices)
                {
                    if (!uniqueIndices.Add(index))
                        continue;

                    if (index < 0 || index >= sourceFrameCount)
                        throw new InvalidDataException($"RSI state {state.Name} in {resourcePath} references frame {index}, but only has {sourceFrameCount} frames");

                    var column = index % sourceColumns;
                    var row = index / sourceColumns;
                    var frameBounds = new Rectangle(column * frameSize.X, row * frameSize.Y, frameSize.X, frameSize.Y);
                    frames.Add(new PreparedRsiFrame(index, image.Clone(ctx => ctx.Crop(frameBounds))));
                }
            }

            states.Add(new PreparedRsiState(state.Name, dirCount, foldedDelays, foldedIndices, frames));
        }

        return new PreparedRsi(metadata.LoadParameters, states);
    }
    #endregion
}

using System;
using System.Collections.Generic;
using System.Buffers.Binary;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Robust.Client.Graphics;
using Robust.Shared.Graphics;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Content.Client._Sunrise;

public sealed partial class NetTexturesManager
{
    private static float[][] NormalizeDelays(float[][]? delays, int dirCount)
    {
        if (delays == null)
        {
            var result = new float[dirCount][];
            for (var i = 0; i < dirCount; i++)
            {
                result[i] = [1];
            }

            return result;
        }

        if (delays.Length != dirCount)
            throw new InvalidDataException($"Direction count {dirCount} does not match delay rows {delays.Length}");

        var normalized = new float[dirCount][];
        for (var i = 0; i < dirCount; i++)
        {
            normalized[i] = delays[i].Length == 0 ? [1] : delays[i];
        }

        return normalized;
    }

    private static (float[] Delays, int[][] Indices) FoldDelays(float[][] delays)
    {
        if (delays.Length == 1)
        {
            var delayList = delays[0];
            var output = new float[delayList.Length];
            var singleIndices = new int[delayList.Length];

            for (var i = 0; i < delayList.Length; i++)
            {
                output[i] = delayList[i];
                singleIndices[i] = i;
            }

            return (output, [singleIndices]);
        }

        const float fixedPointResolution = 1000;

        var dirCount = delays.Length;
        var iDelays = new int[dirCount][];
        Span<int> dirLengths = stackalloc int[dirCount];
        var maxLength = 0;

        for (var d = 0; d < dirCount; d++)
        {
            var length = 0;
            var delayList = new int[delays[d].Length];
            iDelays[d] = delayList;

            for (var i = 0; i < delayList.Length; i++)
            {
                var delay = (int) (delays[d][i] * fixedPointResolution);
                delayList[i] = delay;
                length += delay;
            }

            maxLength = Math.Max(length, maxLength);
            dirLengths[d] = length;
        }

        for (var d = 0; d < dirCount; d++)
        {
            var diff = maxLength - dirLengths[d];
            iDelays[d][^1] += diff;
        }

        Span<int> dirIndexOffsets = stackalloc int[dirCount];
        dirIndexOffsets.Fill(0);
        for (var i = 0; i < dirCount - 1; i++)
        {
            dirIndexOffsets[i + 1] = dirIndexOffsets[i] + delays[i].Length;
        }

        Span<int> dirDelayOffsets = stackalloc int[dirCount];
        dirDelayOffsets.Fill(0);

        var newDelays = new List<int>();
        var newIndices = new List<int>[dirCount];
        for (var d = 0; d < dirCount; d++)
        {
            newIndices[d] = new List<int>();
        }

        while (true)
        {
            var minDelay = int.MaxValue;

            for (var d = 0; d < dirCount; d++)
            {
                var offset = dirDelayOffsets[d];
                var delay = iDelays[d][offset];
                minDelay = Math.Min(delay, minDelay);
                newIndices[d].Add(dirIndexOffsets[d] + offset);
            }

            newDelays.Add(minDelay);

            for (var d = 0; d < dirCount; d++)
            {
                ref var offset = ref dirDelayOffsets[d];
                ref var delay = ref iDelays[d][offset];
                delay -= minDelay;

                if (delay == 0)
                    offset += 1;

                if (offset == iDelays[d].Length)
                    goto done;
            }
        }

        done:

        var floatDelays = new float[newDelays.Count];
        for (var i = 0; i < newDelays.Count; i++)
        {
            floatDelays[i] = newDelays[i] / fixedPointResolution;
        }

        var indices = new int[dirCount][];
        for (var d = 0; d < dirCount; d++)
        {
            indices[d] = newIndices[d].ToArray();
        }

        return (floatDelays, indices);
    }

    private static NetTextureAnimationState CreateAnimationState(PreparedRsiState state)
    {
        var frames = new Texture[state.FoldedIndices.Length][];

        for (var dir = 0; dir < state.FoldedIndices.Length; dir++)
        {
            var indices = state.FoldedIndices[dir];
            var output = new Texture[indices.Length];

            for (var frame = 0; frame < indices.Length; frame++)
            {
                var index = indices[frame];
                output[frame] = state.UploadedFrames[index];
            }

            frames[dir] = output;
        }

        var directionType = state.DirectionCount switch
        {
            1 => RsiDirectionType.Dir1,
            4 => RsiDirectionType.Dir4,
            8 => RsiDirectionType.Dir8,
            _ => throw new InvalidOperationException($"Unsupported RSI direction count {state.DirectionCount}")
        };

        return new NetTextureAnimationState(state.StateId, directionType, state.FoldedDelays, frames);
    }

    private async Task<List<(ResPath Relative, byte[] Data)>> ReadTransferStream(Stream stream)
    {
        var files = new List<(ResPath Relative, byte[] Data)>();
        var lengthBytes = new byte[4];
        var continueByte = new byte[1];

        while (true)
        {
            await stream.ReadExactlyAsync(lengthBytes).ConfigureAwait(false);
            var pathLength = BinaryPrimitives.ReadUInt32LittleEndian(lengthBytes);
            if (pathLength > int.MaxValue)
                throw new InvalidDataException($"NetTextures transfer path length is too large: {pathLength}");

            await stream.ReadExactlyAsync(lengthBytes).ConfigureAwait(false);
            var dataLength = BinaryPrimitives.ReadUInt32LittleEndian(lengthBytes);
            if (dataLength > int.MaxValue)
                throw new InvalidDataException($"NetTextures transfer file length is too large: {dataLength}");

            var pathData = new byte[(int) pathLength];
            await stream.ReadExactlyAsync(pathData).ConfigureAwait(false);

            var data = new byte[(int) dataLength];
            await stream.ReadExactlyAsync(data).ConfigureAwait(false);

            files.Add((new ResPath(Encoding.UTF8.GetString(pathData)), data));

            await stream.ReadExactlyAsync(continueByte).ConfigureAwait(false);
            if (continueByte[0] == 0)
                break;
        }

        return files;
    }

    private Task RunOnMainThreadAsync(Action callback)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _taskManager.RunOnMainThread(() =>
        {
            try
            {
                callback();
                tcs.TrySetResult();
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });
        return tcs.Task;
    }

    private Task<T> RunOnMainThreadAsync<T>(Func<T> callback)
    {
        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        _taskManager.RunOnMainThread(() =>
        {
            try
            {
                tcs.TrySetResult(callback());
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });
        return tcs.Task;
    }

    private static bool IsRsiPath(ResPath path)
    {
        var pathString = path.ToRelativePath().ToString();
        return pathString.EndsWith(".rsi", StringComparison.Ordinal) ||
               pathString.EndsWith(".rsi/", StringComparison.Ordinal);
    }

    private static ResPath ToResPath(string resourcePath)
    {
        var resPath = resourcePath.StartsWith("/", StringComparison.Ordinal)
            ? new ResPath(resourcePath)
            : (new ResPath("/") / resourcePath);

        return resPath.Clean();
    }

    private static string GetUploadedResourcePath(ResPath relativePath)
    {
        var rootedPath = (ResPath.Root / relativePath.ToRelativePath()).Clean();
        var parent = rootedPath.Directory.ToString();

        if (parent.EndsWith(".rsi", StringComparison.Ordinal) || parent.EndsWith(".rsi/", StringComparison.Ordinal))
            return rootedPath.Directory.ToString().TrimEnd('/');

        return rootedPath.ToString();
    }

    private sealed class LoadedTextureEntry(OwnedTexture texture) : IDisposable
    {
        public readonly OwnedTexture Texture = texture;

        public void Dispose()
        {
            Texture.Dispose();
        }
    }

    private sealed class LoadedRsiEntry(List<OwnedTexture> textures, Dictionary<string, NetTextureAnimationState> states) : IDisposable
    {
        public readonly Dictionary<string, NetTextureAnimationState> States = states;
        private readonly List<OwnedTexture> _textures = textures;

        public void Dispose()
        {
            foreach (var texture in _textures)
            {
                texture.Dispose();
            }
        }
    }

    private sealed class PreparedTexture(Image<Rgba32> image) : IDisposable
    {
        public Image<Rgba32> Image { get; private set; } = image;

        public void Dispose()
        {
            Image?.Dispose();
            Image = null!;
        }
    }

    private abstract class PreparedUploadJob(string resourcePath, int generation) : IDisposable
    {
        public string ResourcePath { get; } = resourcePath;
        public int Generation { get; } = generation;

        public abstract bool ProcessStep(NetTexturesManager manager, CancellationToken cancellationToken);
        public abstract void Commit(NetTexturesManager manager);
        public abstract void Dispose();
    }

    private sealed class PreparedTextureUploadJob(string resourcePath, int generation, PreparedTexture prepared)
        : PreparedUploadJob(resourcePath, generation)
    {
        private PreparedTexture? _prepared = prepared;
        private LoadedTextureEntry? _loadedTexture;

        public override bool ProcessStep(NetTexturesManager manager, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var prepared = _prepared ?? throw new InvalidOperationException($"Texture upload job for {ResourcePath} has no prepared image");
            try
            {
                _loadedTexture = new LoadedTextureEntry(manager._clyde.LoadTextureFromImage(prepared.Image, ResourcePath));
                return true;
            }
            finally
            {
                prepared.Dispose();
                _prepared = null;
            }
        }

        public override void Commit(NetTexturesManager manager)
        {
            if (_loadedTexture == null)
                throw new InvalidOperationException($"Texture upload job for {ResourcePath} completed without a loaded texture");

            manager.FinishPreparedTexture(ResourcePath, _loadedTexture);
            _loadedTexture = null;
        }

        public override void Dispose()
        {
            _prepared?.Dispose();
            _prepared = null;

            _loadedTexture?.Dispose();
            _loadedTexture = null;
        }
    }

    private sealed class PreparedRsiUploadJob(string resourcePath, int generation, PreparedRsi prepared)
        : PreparedUploadJob(resourcePath, generation)
    {
        private PreparedRsi? _prepared = prepared;
        private readonly List<OwnedTexture> _textures = [];
        private LoadedRsiEntry? _loadedRsi;
        private int _stateIndex;
        private int _frameIndex;

        public override bool ProcessStep(NetTexturesManager manager, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var prepared = _prepared ?? throw new InvalidOperationException($"RSI upload job for {ResourcePath} has no prepared states");

            while (_stateIndex < prepared.States.Count)
            {
                var state = prepared.States[_stateIndex];
                if (_frameIndex >= state.Frames.Count)
                {
                    _stateIndex++;
                    _frameIndex = 0;
                    continue;
                }

                var frame = state.Frames[_frameIndex];
                try
                {
                    var texture = manager._clyde.LoadTextureFromImage(frame.Image!, $"{ResourcePath}:{state.StateId}:{frame.SourceIndex}", prepared.LoadParameters);
                    _textures.Add(texture);
                    state.UploadedFrames[frame.SourceIndex] = texture;
                }
                finally
                {
                    frame.Dispose();
                }

                _frameIndex++;
                return TryFinalize(prepared);
            }

            return TryFinalize(prepared);
        }

        private bool TryFinalize(PreparedRsi prepared)
        {
            if (_stateIndex < prepared.States.Count)
                return false;

            var states = new Dictionary<string, NetTextureAnimationState>(prepared.States.Count, StringComparer.Ordinal);
            foreach (var state in prepared.States)
            {
                states[state.StateId] = CreateAnimationState(state);
            }

            _loadedRsi = new LoadedRsiEntry(_textures, states);
            prepared.Dispose();
            _prepared = null;
            return true;
        }

        public override void Commit(NetTexturesManager manager)
        {
            if (_loadedRsi == null)
                throw new InvalidOperationException($"RSI upload job for {ResourcePath} completed without a loaded RSI");

            manager.FinishPreparedRsi(ResourcePath, _loadedRsi);
            _loadedRsi = null;
        }

        public override void Dispose()
        {
            _prepared?.Dispose();
            _prepared = null;

            if (_loadedRsi != null)
            {
                _loadedRsi.Dispose();
                _loadedRsi = null;
                return;
            }

            foreach (var texture in _textures)
            {
                texture.Dispose();
            }

            _textures.Clear();
        }
    }

    private sealed class PreparedRsi(TextureLoadParameters loadParameters, List<PreparedRsiState> states) : IDisposable
    {
        public readonly TextureLoadParameters LoadParameters = loadParameters;
        public readonly List<PreparedRsiState> States = states;

        public void Dispose()
        {
            foreach (var state in States)
            {
                state.Dispose();
            }
        }
    }

    private sealed class PreparedRsiState(
        string stateId,
        int directionCount,
        float[] foldedDelays,
        int[][] foldedIndices,
        List<PreparedRsiFrame> frames) : IDisposable
    {
        public readonly string StateId = stateId;
        public readonly int DirectionCount = directionCount;
        public readonly float[] FoldedDelays = foldedDelays;
        public readonly int[][] FoldedIndices = foldedIndices;
        public readonly List<PreparedRsiFrame> Frames = frames;
        public readonly Dictionary<int, Texture> UploadedFrames = new();

        public void Dispose()
        {
            foreach (var frame in Frames)
            {
                frame.Dispose();
            }
        }
    }

    private sealed class PreparedRsiFrame(int sourceIndex, Image<Rgba32> image) : IDisposable
    {
        public readonly int SourceIndex = sourceIndex;
        public Image<Rgba32>? Image { get; private set; } = image;

        public void Dispose()
        {
            Image?.Dispose();
            Image = null;
        }
    }

    private sealed class FallbackChunkAssembly(int totalChunks) : IDisposable
    {
        private readonly byte[]?[] _chunks = new byte[totalChunks][];

        public int TotalChunks { get; } = totalChunks;
        public bool IsComplete { get; private set; }
        private int _receivedChunks;

        public void StoreChunk(int chunkIndex, byte[] data)
        {
            if (_chunks[chunkIndex] != null)
                return;

            _chunks[chunkIndex] = data;
            _receivedChunks++;
            IsComplete = _receivedChunks == TotalChunks;
        }

        public byte[] Combine()
        {
            if (!IsComplete)
                throw new InvalidOperationException("Cannot combine incomplete fallback NetTextures chunks");

            var totalLength = 0;
            foreach (var chunk in _chunks)
            {
                totalLength += chunk!.Length;
            }

            var combined = new byte[totalLength];
            var offset = 0;

            foreach (var chunk in _chunks)
            {
                var chunkData = chunk!;
                Buffer.BlockCopy(chunkData, 0, combined, offset, chunkData.Length);
                offset += chunkData.Length;
            }

            return combined;
        }

        public void Dispose()
        {
            for (var i = 0; i < _chunks.Length; i++)
            {
                _chunks[i] = null;
            }
        }
    }

    public sealed class NetTextureAnimationState(string stateId, RsiDirectionType directions, float[] delays, Texture[][] frames)
    {
        public string StateId { get; } = stateId;
        public RsiDirectionType Directions { get; } = directions;
        public int FrameCount => delays.Length;
        public bool IsAnimated => FrameCount > 1;
        public Texture Frame0 => frames[0][0];

        public float GetDelay(int frame)
        {
            return delays[frame];
        }

        public Texture GetFrame(RsiDirection direction, int frame)
        {
            var dirIndex = Directions switch
            {
                RsiDirectionType.Dir1 => 0,
                RsiDirectionType.Dir4 => Math.Min((int) direction, 3),
                _ => (int) direction
            };

            return frames[dirIndex][frame];
        }
    }

    private sealed class RsiMetadataJson
    {
        public RsiSizeJson? Size { get; set; }
        public RsiStateJson[]? States { get; set; }
        public RsiLoadJson? Load { get; set; }
    }

    private sealed class RsiSizeJson
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    private sealed class RsiStateJson
    {
        public string Name { get; set; } = string.Empty;
        public int? Directions { get; set; }
        public float[][]? Delays { get; set; }
    }

    private sealed class RsiLoadJson
    {
        public bool Srgb { get; set; } = true;
    }
}

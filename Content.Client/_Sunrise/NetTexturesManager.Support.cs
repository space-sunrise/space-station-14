using System.Buffers.Binary;
using System.IO;
using System.Text;
using System.Threading;
using Robust.Client.Graphics;
using Robust.Shared.Graphics;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using YamlDotNet.RepresentationModel;

namespace Content.Client._Sunrise;

public sealed partial class NetTexturesManager
{
    private static readonly float[] OneFrameDelay = new float[] { 1f };

    private static float[][] NormalizeDelays(float[][]? delays, int dirCount)
    {
        if (delays == null)
        {
            var result = new float[dirCount][];
            for (var i = 0; i < dirCount; i++)
            {
                result[i] = OneFrameDelay;
            }

            return result;
        }

        if (delays.Length != dirCount)
            throw new InvalidDataException($"Direction count {dirCount} does not match delay rows {delays.Length}");

        var normalized = new float[dirCount][];
        for (var i = 0; i < dirCount; i++)
        {
            normalized[i] = delays[i].Length == 0 ? OneFrameDelay : delays[i];
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

            return (output, new[] { singleIndices });
        }

        const float fixedPointResolution = 1000;

        var dirCount = delays.Length;
        var iDelays = new int[dirCount][];
        var dirLengths = new int[dirCount];
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

        var dirIndexOffsets = new int[dirCount];
        for (var i = 0; i < dirCount - 1; i++)
        {
            dirIndexOffsets[i + 1] = dirIndexOffsets[i] + delays[i].Length;
        }

        var dirDelayOffsets = new int[dirCount];

        var newDelays = new List<int>();
        var newIndices = new List<int>[dirCount];
        for (var d = 0; d < dirCount; d++)
        {
            newIndices[d] = [];
        }

        var finished = false;
        while (!finished)
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
                var offset = dirDelayOffsets[d];
                var delay = iDelays[d][offset] - minDelay;
                iDelays[d][offset] = delay;

                if (delay == 0)
                {
                    offset += 1;
                    dirDelayOffsets[d] = offset;
                }

                if (offset == iDelays[d].Length)
                {
                    finished = true;
                    break;
                }
            }
        }

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

    private List<(ResPath Relative, byte[] Data)> ReadTransferStream(Stream stream)
    {
        var files = new List<(ResPath Relative, byte[] Data)>();
        var lengthBytes = new byte[4];
        var continueByte = new byte[1];

        while (true)
        {
            ReadExactly(stream, lengthBytes);
            var pathLength = BinaryPrimitives.ReadUInt32LittleEndian(lengthBytes);
            if (pathLength > int.MaxValue)
                throw new InvalidDataException($"NetTextures transfer path length is too large: {pathLength}");

            ReadExactly(stream, lengthBytes);
            var dataLength = BinaryPrimitives.ReadUInt32LittleEndian(lengthBytes);
            if (dataLength > int.MaxValue)
                throw new InvalidDataException($"NetTextures transfer file length is too large: {dataLength}");

            var pathData = new byte[(int) pathLength];
            ReadExactly(stream, pathData);

            var data = new byte[(int) dataLength];
            ReadExactly(stream, data);

            files.Add((new ResPath(Encoding.UTF8.GetString(pathData)), data));

            ReadExactly(stream, continueByte);
            if (continueByte[0] == 0)
                break;
        }

        return files;
    }

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

    private static RsiMetadataData LoadRsiMetadata(Stream metaStream)
    {
        using var reader = new StreamReader(metaStream, Encoding.UTF8, true, 4096, leaveOpen: true);
        var yaml = new YamlStream();
        yaml.Load(reader);

        if (yaml.Documents.Count != 1 || yaml.Documents[0].RootNode is not YamlMappingNode root)
            throw new InvalidDataException("RSI metadata root must be a mapping");

        if (!root.TryGetNode("size", out YamlMappingNode? sizeNode))
            throw new InvalidDataException("RSI metadata is missing size");

        if (!sizeNode.TryGetNode("x", out var sizeXNode) || !sizeNode.TryGetNode("y", out var sizeYNode))
            throw new InvalidDataException("RSI metadata size is incomplete");

        if (!root.TryGetNode("states", out YamlSequenceNode? statesNode) || statesNode.Children.Count == 0)
            throw new InvalidDataException("RSI metadata is missing states");

        var states = new RsiStateMetadataData[statesNode.Children.Count];
        for (var i = 0; i < statesNode.Children.Count; i++)
        {
            if (statesNode.Children[i] is not YamlMappingNode stateNode)
                throw new InvalidDataException("RSI metadata state must be a mapping");

            if (!stateNode.TryGetNode("name", out var nameNode))
                throw new InvalidDataException("RSI metadata state is missing name");

            int? directions = null;
            if (stateNode.TryGetNode("directions", out var directionsNode))
                directions = directionsNode.AsInt();

            float[][]? delays = null;
            if (stateNode.TryGetNode("delays", out YamlSequenceNode? delayRowsNode))
                delays = ReadRsiDelays(delayRowsNode);

            states[i] = new RsiStateMetadataData(nameNode.AsString(), directions, delays);
        }

        var loadParameters = TextureLoadParameters.Default;
        if (root.TryGetNode("load", out YamlMappingNode? loadNode))
            loadParameters = TextureLoadParameters.FromYaml(loadNode);

        return new RsiMetadataData(new Vector2i(sizeXNode.AsInt(), sizeYNode.AsInt()), states, loadParameters);
    }

    private static float[][] ReadRsiDelays(YamlSequenceNode delayRowsNode)
    {
        var rows = new float[delayRowsNode.Children.Count][];
        for (var rowIndex = 0; rowIndex < delayRowsNode.Children.Count; rowIndex++)
        {
            if (delayRowsNode.Children[rowIndex] is not YamlSequenceNode delayRowNode)
                throw new InvalidDataException("RSI delay rows must be sequences");

            var row = new float[delayRowNode.Children.Count];
            for (var frameIndex = 0; frameIndex < delayRowNode.Children.Count; frameIndex++)
            {
                row[frameIndex] = delayRowNode.Children[frameIndex].AsFloat();
            }

            rows[rowIndex] = row;
        }

        return rows;
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

        public void Dispose()
        {
            foreach (var texture in textures)
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
        private readonly List<OwnedTexture> _textures = new();
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

            var states = new Dictionary<string, NetTextureAnimationState>(prepared.States.Count);
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
                Array.Copy(chunkData, 0, combined, offset, chunkData.Length);
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

    private sealed class PreparationRequest(string resourcePath, ResPath resPath, int generation)
    {
        public readonly string ResourcePath = resourcePath;
        public readonly ResPath ResPath = resPath;
        public readonly int Generation = generation;
    }

    private sealed class RsiMetadataData(Vector2i size, RsiStateMetadataData[] states, TextureLoadParameters loadParameters)
    {
        public readonly Vector2i Size = size;
        public readonly RsiStateMetadataData[] States = states;
        public readonly TextureLoadParameters LoadParameters = loadParameters;
    }

    private sealed class RsiStateMetadataData(string name, int? directions, float[][]? delays)
    {
        public readonly string Name = name;
        public readonly int? Directions = directions;
        public readonly float[][]? Delays = delays;
    }
}

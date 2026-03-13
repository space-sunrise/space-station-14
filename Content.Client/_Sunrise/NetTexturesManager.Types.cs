using System.Collections.Generic;
using System.Threading;
using Robust.Client.Graphics;
using Robust.Shared.Graphics;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Content.Client._Sunrise;

public sealed partial class NetTexturesManager
{
    #region Loaded Resources
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
    #endregion

    #region Preparation Payloads
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
    #endregion

    #region Transfer State
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
    #endregion

    #region Public State Views
    /// <summary>
    /// Represents a ready-to-use animation state produced from a network-delivered RSI resource.
    /// </summary>
    public sealed class NetTextureAnimationState(string stateId, RsiDirectionType directions, float[] delays, Texture[][] frames)
    {
        /// <summary>
        /// Gets the RSI state identifier.
        /// </summary>
        public string StateId { get; } = stateId;

        /// <summary>
        /// Gets the directional layout used by the uploaded animation.
        /// </summary>
        public RsiDirectionType Directions { get; } = directions;

        /// <summary>
        /// Gets the number of folded animation frames in this state.
        /// </summary>
        public int FrameCount => delays.Length;

        /// <summary>
        /// Gets whether the state advances through more than one frame.
        /// </summary>
        public bool IsAnimated => FrameCount > 1;

        /// <summary>
        /// Gets the first frame of the south-facing animation, which is commonly used as a preview frame.
        /// </summary>
        public Texture Frame0 => frames[0][0];

        /// <summary>
        /// Gets the display delay for a folded frame.
        /// </summary>
        /// <param name="frame">The folded frame index.</param>
        /// <returns>The delay in seconds for the requested frame.</returns>
        public float GetDelay(int frame)
        {
            return delays[frame];
        }

        /// <summary>
        /// Gets the texture for a specific direction and folded frame.
        /// </summary>
        /// <param name="direction">The requested RSI direction.</param>
        /// <param name="frame">The folded frame index.</param>
        /// <returns>The uploaded texture for the requested direction and frame.</returns>
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
    #endregion

    #region Metadata Models
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
    #endregion
}

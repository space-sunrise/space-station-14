using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Utility;
using Robust.Shared.ContentPack;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using static Robust.Client.GameObjects.SpriteComponent;
using Color = Robust.Shared.Maths.Color;

namespace Content.Client._Sunrise.Particles;

/// <summary>
/// Samples current RSI frames to resolve entity-derived particle tints.
/// </summary>
public sealed partial class ParticleVisualAnchorSystem
{
    [Dependency] private readonly IResourceManager _resources = default!;

    private const float MinimumDominantAlpha = 0.18f;
    private const float MinimumDominantLuminance = 0.08f;
    private const int DominantColorShift = 4;

    private readonly Dictionary<SpriteFramePaletteKey, SpritePaletteEntry[]?> _spritePaletteCache = [];
    private readonly Dictionary<int, DominantColorBucket> _dominantColorBuckets = [];

    /// <summary>
    /// Returns the most frequent visible color of the entity's current RSI frames.
    /// </summary>
    public bool TryGetDominantSpriteColor(EntityUid source, out Color color)
    {
        color = default;
        if (TerminatingOrDeleted(source) ||
            !TryComp<SpriteComponent>(source, out var sprite) ||
            !sprite.Visible)
        {
            return false;
        }

        _dominantColorBuckets.Clear();

        var screenAngle = (_transform.GetWorldRotation(source) + _eye.CurrentEye.Rotation)
            .Reduced()
            .FlipPositive();

        foreach (var layerObject in sprite.AllLayers)
        {
            if (layerObject is not Layer { Visible: true, Blank: false } layer)
                continue;

            if (layer.CopyToShaderParameters != null || layer.ActualState is not { } state)
                continue;

            var modulation = sprite.Color * layer.Color;
            if (modulation.A <= 0f)
                continue;

            var direction = Layer.GetDirection(state.RsiDirections, screenAngle);
            if (sprite.EnableDirectionOverride)
                direction = sprite.DirectionOverride.Convert(state.RsiDirections);
            direction = direction.OffsetRsiDir(layer.DirOffset);

            var texture = state.GetFrame(direction, layer.AnimationFrame);
            if (!TryGetFramePalette(state, texture, out var palette))
                continue;

            AddPalette(palette, modulation);
        }

        var strongest = default(DominantColorBucket);
        foreach (var bucket in _dominantColorBuckets.Values)
        {
            if (bucket.Weight > strongest.Weight)
                strongest = bucket;
        }

        if (strongest.Weight <= 0f)
            return false;

        color = ParticleColorHelper.EnsureVisible(new Color(
            strongest.Red / strongest.Weight,
            strongest.Green / strongest.Weight,
            strongest.Blue / strongest.Weight,
            1f));
        return true;
    }

    private void AddPalette(SpritePaletteEntry[] palette, Color modulation)
    {
        foreach (var entry in palette)
        {
            var modulated = entry.Color * modulation;
            if (modulated.A < MinimumDominantAlpha)
                continue;

            var luminance = modulated.R * 0.2126f + modulated.G * 0.7152f + modulated.B * 0.0722f;
            if (luminance < MinimumDominantLuminance)
                continue;

            var red = Math.Clamp((int) (modulated.R * byte.MaxValue), 0, byte.MaxValue);
            var green = Math.Clamp((int) (modulated.G * byte.MaxValue), 0, byte.MaxValue);
            var blue = Math.Clamp((int) (modulated.B * byte.MaxValue), 0, byte.MaxValue);
            var bucketKey = (red >> DominantColorShift) << 8 |
                            (green >> DominantColorShift) << 4 |
                            blue >> DominantColorShift;
            var weight = entry.Count * modulated.A;

            _dominantColorBuckets.TryGetValue(bucketKey, out var bucket);
            bucket.Weight += weight;
            bucket.Red += modulated.R * weight;
            bucket.Green += modulated.G * weight;
            bucket.Blue += modulated.B * weight;
            _dominantColorBuckets[bucketKey] = bucket;
        }
    }

    private bool TryGetFramePalette(
        RSI.State state,
        Texture texture,
        out SpritePaletteEntry[] palette)
    {
        palette = [];
        if (texture is not AtlasTexture atlas ||
            state.StateId.Name is not { } stateName ||
            !TryGetRelativeFrameIndex(state, atlas, out var frameIndex))
        {
            return false;
        }

        var key = new SpriteFramePaletteKey(state.RSI.Path, stateName, frameIndex);
        if (!_spritePaletteCache.TryGetValue(key, out var cached))
        {
            cached = LoadFramePalette(state, atlas, frameIndex);
            _spritePaletteCache[key] = cached;
        }

        if (cached == null)
            return false;

        palette = cached;
        return true;
    }

    private SpritePaletteEntry[]? LoadFramePalette(RSI.State state, AtlasTexture atlas, int frameIndex)
    {
        var frameSize = state.Size;
        var statePath = state.RSI.Path / $"{state.StateId.Name}.png";
        if (_resources.TryContentFileRead(statePath, out var stateStream))
        {
            using (stateStream)
            using (var image = Image.Load<Rgba32>(stateStream))
            {
                var framesPerRow = image.Width / frameSize.X;
                if (framesPerRow <= 0)
                    return null;

                var origin = new Vector2i(
                    frameIndex % framesPerRow * frameSize.X,
                    frameIndex / framesPerRow * frameSize.Y);
                return ExtractPalette(image, origin, frameSize);
            }
        }

        var compiledPath = state.RSI.Path.WithExtension("rsic");
        if (!_resources.TryContentFileRead(compiledPath, out var compiledStream))
            return null;

        using (compiledStream)
        using (var image = Image.Load<Rgba32>(compiledStream))
        {
            var origin = new Vector2i((int) atlas.SubRegion.Left, (int) atlas.SubRegion.Top);
            return ExtractPalette(image, origin, frameSize);
        }
    }

    private static SpritePaletteEntry[]? ExtractPalette(
        Image<Rgba32> image,
        Vector2i origin,
        Vector2i frameSize)
    {
        if (origin.X < 0 ||
            origin.Y < 0 ||
            origin.X + frameSize.X > image.Width ||
            origin.Y + frameSize.Y > image.Height)
        {
            return null;
        }

        var counts = new Dictionary<uint, int>();
        var pixels = image.GetPixelSpan();
        for (var y = 0; y < frameSize.Y; y++)
        {
            for (var x = 0; x < frameSize.X; x++)
            {
                var pixelIndex = (origin.Y + y) * image.Width + origin.X + x;
                var pixel = pixels[pixelIndex];
                if (pixel.A < MinimumDominantAlpha * byte.MaxValue)
                    continue;

                var packed = (uint) pixel.R |
                             (uint) pixel.G << 8 |
                             (uint) pixel.B << 16 |
                             (uint) pixel.A << 24;
                counts.TryGetValue(packed, out var count);
                counts[packed] = count + 1;
            }
        }

        if (counts.Count == 0)
            return null;

        var palette = new SpritePaletteEntry[counts.Count];
        var index = 0;
        foreach (var (packed, count) in counts)
        {
            palette[index++] = new SpritePaletteEntry(
                new Color(
                    (byte) packed,
                    (byte) (packed >> 8),
                    (byte) (packed >> 16),
                    (byte) (packed >> 24)),
                count);
        }

        return palette;
    }

    /// <summary>
    /// Converts the current atlas frame into its source-state image index without reading pixels back from the GPU.
    /// </summary>
    private static bool TryGetRelativeFrameIndex(RSI.State state, AtlasTexture current, out int frameIndex)
    {
        frameIndex = 0;
        if (!TryGetAtlasFrameIndex(current, state.Size, out var currentIndex))
            return false;

        var firstIndex = int.MaxValue;
        foreach (var directionFrames in state.Icons)
        {
            foreach (var frame in directionFrames)
            {
                if (frame is not AtlasTexture atlas ||
                    !ReferenceEquals(atlas.SourceTexture, current.SourceTexture) ||
                    !TryGetAtlasFrameIndex(atlas, state.Size, out var atlasIndex))
                {
                    continue;
                }

                firstIndex = Math.Min(firstIndex, atlasIndex);
            }
        }

        if (firstIndex == int.MaxValue || currentIndex < firstIndex)
            return false;

        frameIndex = currentIndex - firstIndex;
        return true;
    }

    private static bool TryGetAtlasFrameIndex(AtlasTexture texture, Vector2i frameSize, out int frameIndex)
    {
        frameIndex = 0;
        if (frameSize.X <= 0 || frameSize.Y <= 0)
            return false;

        var framesPerRow = texture.SourceTexture.Width / frameSize.X;
        if (framesPerRow <= 0)
            return false;

        var column = (int) texture.SubRegion.Left / frameSize.X;
        var row = (int) texture.SubRegion.Top / frameSize.Y;
        frameIndex = row * framesPerRow + column;
        return true;
    }

    private readonly record struct SpriteFramePaletteKey(ResPath RsiPath, string State, int FrameIndex);

    private readonly record struct SpritePaletteEntry(Color Color, int Count);

    private struct DominantColorBucket
    {
        public float Weight;
        public float Red;
        public float Green;
        public float Blue;
    }
}

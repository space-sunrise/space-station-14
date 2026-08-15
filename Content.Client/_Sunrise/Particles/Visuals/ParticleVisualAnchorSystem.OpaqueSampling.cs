using System.Numerics;
using Content.Client.Clickable;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Utility;
using Robust.Shared.Graphics.RSI;
using static Robust.Client.GameObjects.SpriteComponent;

namespace Content.Client._Sunrise.Particles;

public sealed partial class ParticleVisualAnchorSystem
{
    [Dependency] private readonly IClickMapManager _clickMap = default!;

    private const int OpaqueSampleStep = 2;
    private const float PixelsPerSample = 5.5f;
    private const float DuplicateSampleDistanceSquared = 0.0004f;

    private readonly List<Vector2> _opaqueLayerSamples = new(1024);

    /// <summary>
    /// Adds world-space offsets distributed over the opaque silhouette of the selected sprite layer.
    /// </summary>
    public int AddOpaqueLayerSampleOffsets(
        Entity<SpriteComponent> source,
        string layerKey,
        List<Vector2> offsets,
        int maxSamples)
    {
        if (maxSamples <= 0)
            return 0;

        if (!source.Comp.Visible)
            return 0;

        if (!_sprite.TryGetLayer(source.AsNullable(), layerKey, out var layer, false))
            return 0;

        if (!layer.Visible)
            return 0;

        if (layer.Blank)
            return 0;

        if (layer.CopyToShaderParameters != null)
            return 0;

        if ((source.Comp.Color * layer.Color).A <= 0f)
            return 0;

        var worldRotation = _transform.GetWorldRotation(source);
        var eyeRotation = _eye.CurrentEye.Rotation;
        var screenAngle = (worldRotation + eyeRotation).Reduced().FlipPositive();
        var state = layer.ActualState;
        var baseDirection = state == null
            ? RsiDirection.South
            : Layer.GetDirection(state.RsiDirections, screenAngle);

        layer.GetLayerDrawMatrix(baseDirection, out var layerMatrix);

        var direction = baseDirection;
        if (source.Comp.EnableDirectionOverride && state != null)
            direction = source.Comp.DirectionOverride.Convert(state.RsiDirections);
        direction = direction.OffsetRsiDir(layer.DirOffset);

        var entityRotation = GetLayerEntityRotation(
            source.Comp,
            layer,
            worldRotation,
            eyeRotation,
            screenAngle);
        var entityMatrix = Matrix3Helpers.CreateTransform(Vector2.Zero, entityRotation);
        var spriteMatrix = Matrix3x2.Multiply(source.Comp.LocalMatrix, entityMatrix);
        var renderMatrix = Matrix3x2.Multiply(layerMatrix, spriteMatrix);

        if (state != null && layer.ActualRsi is { } rsi && layer.State.IsValid)
        {
            var pixelSource = new RsiOpaquePixelSource(
                _clickMap,
                rsi,
                layer.State,
                direction,
                layer.AnimationFrame);
            return AddOpaqueSamples(pixelSource, renderMatrix, offsets, maxSamples);
        }

        if (layer.Texture != null)
        {
            var pixelSource = new TextureOpaquePixelSource(_clickMap, layer.Texture);
            return AddOpaqueSamples(pixelSource, renderMatrix, offsets, maxSamples);
        }

        return 0;
    }

    private int AddOpaqueSamples<TSource>(
        TSource source,
        Matrix3x2 renderMatrix,
        List<Vector2> offsets,
        int maxSamples)
        where TSource : struct, IOpaquePixelSource
    {
        _opaqueLayerSamples.Clear();

        var size = source.Size;
        if (size.X <= 0 || size.Y <= 0)
            return 0;

        var halfWidth = size.X * 0.5f;
        var halfHeight = size.Y * 0.5f;
        var pixelsPerMeter = (float) EyeManager.PixelsPerMeter;

        for (var y = 0; y < size.Y; y += OpaqueSampleStep)
        {
            for (var x = 0; x < size.X; x += OpaqueSampleStep)
            {
                if (!source.IsOpaque(new Vector2i(x, y)))
                    continue;

                var localPoint = new Vector2(
                    (x + 0.5f - halfWidth) / pixelsPerMeter,
                    (halfHeight - y - 0.5f) / pixelsPerMeter);
                _opaqueLayerSamples.Add(Vector2.Transform(localPoint, renderMatrix));
            }
        }

        if (_opaqueLayerSamples.Count == 0)
            return 0;

        var centroid = Vector2.Zero;
        foreach (var point in _opaqueLayerSamples)
            centroid += point;
        centroid /= _opaqueLayerSamples.Count;

        // Главная ось силуэта даёт устойчивое распределение точек и для прямых, и для изогнутых объектов.
        var covarianceX = 0f;
        var covarianceY = 0f;
        var covarianceXY = 0f;
        foreach (var point in _opaqueLayerSamples)
        {
            var relative = point - centroid;
            covarianceX += relative.X * relative.X;
            covarianceY += relative.Y * relative.Y;
            covarianceXY += relative.X * relative.Y;
        }

        var principalAngle = 0.5f * MathF.Atan2(2f * covarianceXY, covarianceX - covarianceY);
        var principalAxis = new Vector2(MathF.Cos(principalAngle), MathF.Sin(principalAngle));
        var minimumProjection = float.MaxValue;
        var maximumProjection = float.MinValue;
        foreach (var point in _opaqueLayerSamples)
        {
            var projection = Vector2.Dot(point, principalAxis);
            minimumProjection = MathF.Min(minimumProjection, projection);
            maximumProjection = MathF.Max(maximumProjection, projection);
        }

        var lengthPixels = (maximumProjection - minimumProjection) * pixelsPerMeter;
        var sampleCount = Math.Clamp(
            (int) MathF.Ceiling(lengthPixels / PixelsPerSample),
            1,
            maxSamples);
        var initialOffsetCount = offsets.Count;

        for (var i = 0; i < sampleCount; i++)
        {
            var factor = sampleCount == 1
                ? 0.5f
                : i / (float) (sampleCount - 1);
            var targetProjection = MathHelper.Lerp(minimumProjection, maximumProjection, factor);
            var closestPoint = _opaqueLayerSamples[0];
            var closestDistance = float.MaxValue;

            foreach (var point in _opaqueLayerSamples)
            {
                var distance = MathF.Abs(Vector2.Dot(point, principalAxis) - targetProjection);
                if (distance >= closestDistance)
                    continue;

                closestDistance = distance;
                closestPoint = point;
            }

            var duplicate = false;
            for (var j = 0; j < offsets.Count; j++)
            {
                if (Vector2.DistanceSquared(offsets[j], closestPoint) >= DuplicateSampleDistanceSquared)
                    continue;

                duplicate = true;
                break;
            }

            if (!duplicate)
                offsets.Add(closestPoint);
        }

        return offsets.Count - initialOffsetCount;
    }

    private static Angle GetLayerEntityRotation(
        SpriteComponent sprite,
        Layer layer,
        Angle worldRotation,
        Angle eyeRotation,
        Angle screenAngle)
    {
        if (!sprite.GranularLayersRendering || layer.RenderingStrategy == LayerRenderingStrategy.UseSpriteStrategy)
        {
            var cardinal = sprite is { NoRotation: false, SnapCardinals: true }
                ? screenAngle.RoundToCardinalAngle()
                : Angle.Zero;
            return sprite.NoRotation
                ? -eyeRotation
                : worldRotation - cardinal;
        }

        return layer.RenderingStrategy switch
        {
            LayerRenderingStrategy.Default => worldRotation,
            LayerRenderingStrategy.NoRotation => -eyeRotation,
            LayerRenderingStrategy.SnapToCardinals => worldRotation - screenAngle.RoundToCardinalAngle(),
            _ => worldRotation,
        };
    }

    private interface IOpaquePixelSource
    {
        Vector2i Size { get; }

        bool IsOpaque(Vector2i pixel);
    }

    private readonly struct TextureOpaquePixelSource(IClickMapManager clickMap, Texture texture) : IOpaquePixelSource
    {
        public Vector2i Size => texture.Size;

        public bool IsOpaque(Vector2i pixel)
        {
            return clickMap.IsOccluding(texture, pixel);
        }
    }

    private readonly struct RsiOpaquePixelSource(
        IClickMapManager clickMap,
        RSI rsi,
        RSI.StateId state,
        RsiDirection direction,
        int animationFrame) : IOpaquePixelSource
    {
        public Vector2i Size => rsi.Size;

        public bool IsOpaque(Vector2i pixel)
        {
            return clickMap.IsOccluding(rsi, state, direction, animationFrame, pixel);
        }
    }
}

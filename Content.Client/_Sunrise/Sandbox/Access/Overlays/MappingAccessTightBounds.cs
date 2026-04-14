using System.Numerics;
using System.Runtime.CompilerServices;
using Content.Client.Clickable;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Utility;
using Robust.Shared.Graphics.RSI;
using static Robust.Client.GameObjects.SpriteComponent;

namespace Content.Client._Sunrise.Sandbox.Access.Overlays;

internal sealed class MappingAccessTightBounds(IClickMapManager clickMapManager)
{
    private const int MaxCacheSize = 2048;

    private const int TrimCacheSize = MaxCacheSize - (MaxCacheSize / 4);

    private readonly BoundedCache<Texture> _textureBounds = new(MaxCacheSize);
    private readonly BoundedCache<RsiFrameCacheKey> _rsiBounds = new(MaxCacheSize);

    public void ClearCache()
    {
        _textureBounds.Clear();
        _rsiBounds.Clear();
    }

    public void TrimCache()
    {
        _textureBounds.TrimCache(TrimCacheSize);
        _rsiBounds.TrimCache(TrimCacheSize);
    }

    public bool TryGetSpriteWorldAabb(
        Entity<SpriteComponent> sprite,
        Vector2 worldPosition,
        Angle worldRotation,
        Angle eyeRotation,
        out Box2 worldBounds)
    {
        worldBounds = default;

        if (!sprite.Comp.Visible)
            return false;

        var angle = (worldRotation + eyeRotation).Reduced().FlipPositive();
        var overrideDirection = sprite.Comp.EnableDirectionOverride
            ? sprite.Comp.DirectionOverride
            : (Direction?) null;
        var cardinal = sprite.Comp is { NoRotation: false, SnapCardinals: true }
            ? angle.RoundToCardinalAngle()
            : Angle.Zero;

        var spriteMatrix = Matrix3x2.Multiply(
            sprite.Comp.LocalMatrix,
            Matrix3Helpers.CreateTransform(
                worldPosition,
                sprite.Comp.NoRotation ? -eyeRotation : worldRotation - cardinal));

        var foundBounds = false;

        if (!sprite.Comp.GranularLayersRendering)
        {
            foreach (var spriteLayer in sprite.Comp.AllLayers)
            {
                if (spriteLayer is not Layer layer)
                    continue;

                if (!TryGetLayerWorldAabb(
                        layer,
                        ref spriteMatrix,
                        angle,
                        overrideDirection,
                        sprite.Comp.Color,
                        out var layerBounds))
                {
                    continue;
                }

                worldBounds = foundBounds
                    ? worldBounds.Union(layerBounds)
                    : layerBounds;
                foundBounds = true;
            }

            return foundBounds;
        }

        var transformDefault = Matrix3x2.Multiply(
            sprite.Comp.LocalMatrix,
            Matrix3Helpers.CreateTransform(worldPosition, worldRotation));
        var transformSnap = Matrix3x2.Multiply(
            sprite.Comp.LocalMatrix,
            Matrix3Helpers.CreateTransform(worldPosition, worldRotation - angle.RoundToCardinalAngle()));
        var transformNoRotation = Matrix3x2.Multiply(
            sprite.Comp.LocalMatrix,
            Matrix3Helpers.CreateTransform(worldPosition, -eyeRotation));

        foreach (var spriteLayer in sprite.Comp.AllLayers)
        {
            if (spriteLayer is not Layer layer)
                continue;

            var layerBaseMatrix = layer.RenderingStrategy switch
            {
                LayerRenderingStrategy.Default => transformDefault,
                LayerRenderingStrategy.NoRotation => transformNoRotation,
                LayerRenderingStrategy.SnapToCardinals => transformSnap,
                _ => spriteMatrix,
            };

            if (!TryGetLayerWorldAabb(
                    layer,
                    ref layerBaseMatrix,
                    angle,
                    overrideDirection,
                    sprite.Comp.Color,
                    out var layerBounds))
            {
                continue;
            }

            worldBounds = foundBounds
                ? worldBounds.Union(layerBounds)
                : layerBounds;
            foundBounds = true;
        }

        return foundBounds;
    }

    internal static bool TryGetOpaqueLocalBounds(
        Vector2i textureSize,
        Func<Vector2i, bool> isOpaque,
        out Box2 localBounds)
    {
        var source = new DelegateOpaquePixelSource(textureSize, isOpaque);
        return TryGetOpaqueLocalBounds(source, out localBounds);
    }

    private bool TryGetLayerWorldAabb(
        Layer layer,
        ref Matrix3x2 spriteMatrix,
        Angle angle,
        Direction? overrideDirection,
        Color spriteColor,
        out Box2 worldBounds)
    {
        worldBounds = default;

        if (!layer.Visible || layer.Blank || layer.CopyToShaderParameters != null)
            return false;

        var modulate = spriteColor * layer.Color;
        if (modulate.A <= 0f)
            return false;

        var state = layer.ActualState;
        var baseDirection = state == null
            ? RsiDirection.South
            : Layer.GetDirection(state.RsiDirections, angle);

        layer.GetLayerDrawMatrix(baseDirection, out var layerMatrix);

        var direction = baseDirection;
        if (overrideDirection != null && state != null)
            direction = overrideDirection.Value.Convert(state.RsiDirections);

        direction = direction.OffsetRsiDir(layer.DirOffset);

        if (!TryGetLayerLocalBounds(layer, direction, out var localBounds))
            return false;

        var transformMatrix = Matrix3x2.Multiply(layerMatrix, spriteMatrix);
        worldBounds = transformMatrix.TransformBox(localBounds);
        return true;
    }

    private bool TryGetLayerLocalBounds(Layer layer, RsiDirection direction, out Box2 localBounds)
    {
        if (layer.ActualState != null &&
            layer.ActualRsi is { } rsi &&
            layer.State.IsValid)
        {
            return TryGetRsiLocalBounds(rsi, layer.State, direction, layer.AnimationFrame, out localBounds);
        }

        if (layer.Texture != null)
            return TryGetTextureLocalBounds(layer.Texture, out localBounds);

        localBounds = default;
        return false;
    }

    private bool TryGetTextureLocalBounds(Texture texture, out Box2 localBounds)
    {
        if (_textureBounds.TryGetValue(texture, out var cachedBounds))
        {
            localBounds = cachedBounds.LocalBounds;
            return cachedBounds.HasBounds;
        }

        var source = new TextureOpaquePixelSource(clickMapManager, texture);
        var hasBounds = TryGetOpaqueLocalBounds(source, out localBounds);
        EnsureCapacity(_textureBounds);
        _textureBounds.Set(texture, new CachedLocalBounds(hasBounds, localBounds), TrimCacheSize);
        return hasBounds;
    }

    private bool TryGetRsiLocalBounds(
        RSI rsi,
        RSI.StateId stateId,
        RsiDirection direction,
        int animationFrame,
        out Box2 localBounds)
    {
        var key = new RsiFrameCacheKey(rsi, stateId, direction, animationFrame);
        if (_rsiBounds.TryGetValue(key, out var cachedBounds))
        {
            localBounds = cachedBounds.LocalBounds;
            return cachedBounds.HasBounds;
        }

        var source = new RsiOpaquePixelSource(clickMapManager, rsi, stateId, direction, animationFrame);
        var hasBounds = TryGetOpaqueLocalBounds(source, out localBounds);
        EnsureCapacity(_rsiBounds);
        _rsiBounds.Set(key, new CachedLocalBounds(hasBounds, localBounds), TrimCacheSize);
        return hasBounds;
    }

    private static void EnsureCapacity<TKey>(BoundedCache<TKey> cache)
        where TKey : notnull
    {
        cache.EnsureCapacity(TrimCacheSize);
    }

    private static bool TryGetOpaqueLocalBounds<TSource>(TSource source, out Box2 localBounds)
        where TSource : struct, IOpaquePixelSource
    {
        localBounds = default;

        var textureSize = source.Size;
        if (textureSize.X <= 0 || textureSize.Y <= 0)
            return false;

        var foundOpaque = false;
        var minX = textureSize.X;
        var minY = textureSize.Y;
        var maxX = -1;
        var maxY = -1;

        for (var y = 0; y < textureSize.Y; y++)
        {
            for (var x = 0; x < textureSize.X; x++)
            {
                if (!source.IsOpaque(new Vector2i(x, y)))
                    continue;

                foundOpaque = true;

                if (x < minX)
                    minX = x;

                if (y < minY)
                    minY = y;

                if (x > maxX)
                    maxX = x;

                if (y > maxY)
                    maxY = y;
            }
        }

        if (!foundOpaque)
            return false;

        localBounds = PixelBoundsToLocalBounds(textureSize, minX, minY, maxX, maxY);
        return true;
    }

    private static Box2 PixelBoundsToLocalBounds(
        Vector2i textureSize,
        int minX,
        int minY,
        int maxX,
        int maxY)
    {
        var ppm = (float) EyeManager.PixelsPerMeter;
        var halfWidth = textureSize.X * 0.5f;
        var halfHeight = textureSize.Y * 0.5f;

        var left = (minX - halfWidth) / ppm;
        var right = (maxX + 1 - halfWidth) / ppm;
        var bottom = (halfHeight - (maxY + 1)) / ppm;
        var top = (halfHeight - minY) / ppm;

        return Box2.FromDimensions(
            new Vector2(left, bottom),
            new Vector2(right - left, top - bottom));
    }

    private interface IOpaquePixelSource
    {
        Vector2i Size { get; }

        bool IsOpaque(Vector2i pixel);
    }

    private readonly struct DelegateOpaquePixelSource(Vector2i size, Func<Vector2i, bool> isOpaque) : IOpaquePixelSource
    {
        public Vector2i Size { get; } = size;

        public bool IsOpaque(Vector2i pixel)
        {
            return isOpaque(pixel);
        }
    }

    private readonly struct TextureOpaquePixelSource(IClickMapManager clickMapManager, Texture texture): IOpaquePixelSource
    {
        public Vector2i Size => texture.Size;

        public bool IsOpaque(Vector2i pixel)
        {
            return clickMapManager.IsOccluding(texture, pixel);
        }
    }

    private readonly struct RsiOpaquePixelSource(
        IClickMapManager clickMapManager,
        RSI rsi,
        RSI.StateId stateId,
        RsiDirection direction,
        int animationFrame) : IOpaquePixelSource
    {
        public Vector2i Size => rsi.Size;

        public bool IsOpaque(Vector2i pixel)
        {
            return clickMapManager.IsOccluding(rsi, stateId, direction, animationFrame, pixel);
        }
    }

    private readonly record struct RsiFrameCacheKey(
        RSI Rsi,
        RSI.StateId StateId,
        RsiDirection Direction,
        int AnimationFrame)
    {
        public bool Equals(RsiFrameCacheKey other)
        {
            return ReferenceEquals(Rsi, other.Rsi) &&
                   StateId.Equals(other.StateId) &&
                   Direction == other.Direction &&
                   AnimationFrame == other.AnimationFrame;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                RuntimeHelpers.GetHashCode(Rsi),
                StateId,
                (int) Direction,
                AnimationFrame);
        }
    }

    private readonly record struct CachedLocalBounds(bool HasBounds, Box2 LocalBounds);

    private sealed class BoundedCache<TKey>(int maxSize) where TKey : notnull
    {
        private readonly Dictionary<TKey, CacheEntry> _entries = new();
        private readonly object _lock = new();
        private readonly LinkedList<TKey> _usageOrder = [];

        public void Clear()
        {
            lock (_lock)
            {
                _entries.Clear();
                _usageOrder.Clear();
            }
        }

        public void EnsureCapacity(int trimToSize)
        {
            lock (_lock)
            {
                if (_entries.Count < maxSize)
                    return;

                TrimCacheNoLock(trimToSize);
            }
        }

        public void TrimCache(int trimToSize)
        {
            lock (_lock)
            {
                TrimCacheNoLock(trimToSize);
            }
        }

        public bool TryGetValue(TKey key, out CachedLocalBounds value)
        {
            lock (_lock)
            {
                if (!_entries.TryGetValue(key, out var entry))
                {
                    value = default;
                    return false;
                }

                TouchNoLock(entry);
                value = entry.Value;
                return true;
            }
        }

        public void Set(TKey key, CachedLocalBounds value, int trimToSize)
        {
            lock (_lock)
            {
                if (_entries.TryGetValue(key, out var existing))
                {
                    existing.Value = value;
                    TouchNoLock(existing);
                    return;
                }

                if (_entries.Count >= maxSize)
                    TrimCacheNoLock(trimToSize);

                var node = _usageOrder.AddLast(key);
                _entries[key] = new CacheEntry(node, value);
            }
        }

        private void TouchNoLock(CacheEntry entry)
        {
            if (entry.Node == _usageOrder.Last)
                return;

            _usageOrder.Remove(entry.Node);
            _usageOrder.AddLast(entry.Node);
        }

        private void TrimCacheNoLock(int trimToSize)
        {
            trimToSize = Math.Clamp(trimToSize, 0, maxSize);

            while (_entries.Count > trimToSize &&
                   _usageOrder.First is { } oldest)
            {
                _usageOrder.RemoveFirst();
                _entries.Remove(oldest.Value);
            }
        }

        private sealed class CacheEntry(LinkedListNode<TKey> node, CachedLocalBounds value)
        {
            public LinkedListNode<TKey> Node { get; } = node;
            public CachedLocalBounds Value { get; set; } = value;
        }
    }
}

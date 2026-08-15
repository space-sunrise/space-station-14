using System.Numerics;
using Content.Shared._Sunrise.Particles;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Utility;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Client._Sunrise.Particles;

/// <summary>
/// Resolves screen-oriented semantic particle anchors and emission geometry on entity sprites.
/// </summary>
public sealed partial class ParticleVisualAnchorSystem : EntitySystem
{
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private const float FallbackHeight = 1f;
    private const float MinimumUsableWidth = 0.05f;
    private const float MinimumUsableHeight = 0.05f;

    private const float HeadForwardFactor = 0.05f;
    private const float EyesSouthForwardFactor = 0.02f;
    private const float EyesSideForwardFactor = 0.07f;
    private const float EyesNorthForwardFactor = 0.14f;
    private const float MouthSouthForwardFactor = 0.04f;
    private const float MouthSideForwardFactor = 0.11f;
    private const float MouthNorthForwardFactor = 0.2f;
    private const float HandsForwardFactor = 0.22f;
    private const float ItemTipHorizontalFactor = 0.859375f;
    private const float ItemTipVerticalFactor = 0.890625f;

    /// <summary>
    /// Returns the center and screen-space half-extents of an emission box matching the current sprite bounds.
    /// </summary>
    public bool TryGetEmissionBox(
        EntityUid source,
        Vector2 localOffset,
        float coverage,
        out Vector2 worldOffset,
        out Vector2 screenHalfExtents)
    {
        worldOffset = Vector2.Zero;
        screenHalfExtents = Vector2.Zero;

        if (!TryGetVisualBounds(source, out var coordinates, out var bounds))
            return false;

        var center = bounds.Center + bounds.Rotation.RotateVec(localOffset);
        worldOffset = center - coordinates.Position;

        var halfSize = new Vector2(bounds.Box.Width, bounds.Box.Height) * 0.5f;
        halfSize *= Math.Clamp(coverage, 0.05f, 1f);

        // Частицы симулируются в экранном пространстве, поэтому учитываем также поворот камеры.
        var screenRotation = bounds.Rotation + _eye.CurrentEye.Rotation;
        var cos = MathF.Abs(MathF.Cos((float) screenRotation));
        var sin = MathF.Abs(MathF.Sin((float) screenRotation));
        screenHalfExtents = new Vector2(
            cos * halfSize.X + sin * halfSize.Y,
            sin * halfSize.X + cos * halfSize.Y);
        return true;
    }

    /// <summary>
    /// Finds a point on the visible sprite edge facing the supplied world position.
    /// </summary>
    public Vector2 GetVisualEdgeOffset(EntityUid source, Vector2 towardWorldPosition, float inset = 0.82f)
    {
        if (!TryGetVisualBounds(source, out var coordinates, out var bounds))
            return Vector2.Zero;

        var localToward = bounds.Origin
            + (-bounds.Rotation).RotateVec(towardWorldPosition - bounds.Origin);
        var localCenter = bounds.Box.Center;
        var direction = localToward - localCenter;
        if (direction.LengthSquared() < 0.0001f)
            direction = Vector2.UnitY;

        var halfSize = new Vector2(bounds.Box.Width, bounds.Box.Height) * 0.5f;
        var scaleX = MathF.Abs(direction.X) > 0.0001f
            ? halfSize.X / MathF.Abs(direction.X)
            : float.PositiveInfinity;
        var scaleY = MathF.Abs(direction.Y) > 0.0001f
            ? halfSize.Y / MathF.Abs(direction.Y)
            : float.PositiveInfinity;
        var edgeScale = MathF.Min(scaleX, scaleY) * Math.Clamp(inset, 0f, 1f);
        var localPoint = localCenter + direction * edgeScale;
        var worldPoint = bounds.Origin + bounds.Rotation.RotateVec(localPoint - bounds.Origin);
        return worldPoint - coordinates.Position;
    }

    /// <summary>
    /// Converts a world direction to emitter angle space, where zero points toward the top of the screen.
    /// </summary>
    public Angle GetEmitAngle(Vector2 worldDirection)
    {
        if (worldDirection.LengthSquared() < 0.0001f)
            return Angle.Zero;

        var screenDirection = _eye.CurrentEye.Rotation.RotateVec(worldDirection);
        return new Angle(MathF.Atan2(screenDirection.X, screenDirection.Y));
    }

    /// <summary>
    /// Returns an offset from the entity center accounting for sprite size, rotation, and selected RSI direction.
    /// </summary>
    public Vector2 GetOffset(EntityUid source, ParticleVisualAnchor anchor, float lateralOffset = 0f)
    {
        var width = FallbackHeight;
        var height = FallbackHeight;
        var left = -FallbackHeight * 0.5f;
        var bottom = -FallbackHeight * 0.5f;
        SpriteComponent? sprite = null;
        if (!TerminatingOrDeleted(source) && TryComp(source, out sprite))
        {
            var bounds = _sprite.GetLocalBounds((source, sprite));
            if (bounds.Width > MinimumUsableWidth)
            {
                width = bounds.Width;
                left = bounds.Left;
            }

            if (bounds.Height > MinimumUsableHeight)
            {
                height = bounds.Height;
                bottom = bounds.Bottom;
            }
        }

        var localOffset = GetLocalAnchorOffset(anchor, left, bottom, width, height, lateralOffset);
        var offset = TransformLocalOffset(source, sprite, localOffset);

        var facing = GetFacingDirection(source, sprite);
        var forwardFactor = GetForwardFactor(anchor, facing);

        if (forwardFactor > 0f)
        {
            var screenOffset = GetFacingScreenDirection(facing, sprite) * width * forwardFactor;
            offset += (-_eye.CurrentEye.Rotation).RotateVec(screenOffset);
        }

        return offset;
    }

    /// <summary>
    /// Returns a camera-corrected anchor offset when the source entity is not available on the client.
    /// </summary>
    public Vector2 GetFallbackOffset(ParticleVisualAnchor anchor, float lateralOffset = 0f)
    {
        var localOffset = GetLocalAnchorOffset(
            anchor,
            -FallbackHeight * 0.5f,
            -FallbackHeight * 0.5f,
            FallbackHeight,
            FallbackHeight,
            lateralOffset);
        var eyeRotation = _eye.CurrentEye.Rotation;
        var offset = (-eyeRotation).RotateVec(localOffset);
        var forwardFactor = GetForwardFactor(anchor, Direction.South);

        if (forwardFactor > 0f)
        {
            var screenOffset = Direction.South.ToVec() * FallbackHeight * forwardFactor;
            offset += (-eyeRotation).RotateVec(screenOffset);
        }

        return offset;
    }

    /// <summary>
    /// Returns the RSI frame direction that visually represents the entity facing.
    /// </summary>
    public Direction GetFacingDirection(EntityUid source)
    {
        SpriteComponent? sprite = null;
        if (!TerminatingOrDeleted(source))
            TryComp(source, out sprite);

        return GetFacingDirection(source, sprite);
    }

    /// <summary>
    /// Converts a sprite-local offset to world space using the current visual transform.
    /// </summary>
    public Vector2 TransformLocalOffset(EntityUid source, Vector2 localOffset)
    {
        SpriteComponent? sprite = null;
        if (!TerminatingOrDeleted(source))
            TryComp(source, out sprite);

        return TransformLocalOffset(source, sprite, localOffset);
    }

    private Vector2 TransformLocalOffset(EntityUid source, SpriteComponent? sprite, Vector2 localOffset)
    {
        if (sprite == null)
            return (-_eye.CurrentEye.Rotation).RotateVec(localOffset);

        // SpriteComponent.Rotation используется, в частности, для визуального поворота лежащего персонажа.
        var spriteOffset = sprite.Offset + sprite.Rotation.RotateVec(localOffset);
        var rotation = sprite.NoRotation
            ? -_eye.CurrentEye.Rotation
            : _transform.GetWorldRotation(source);
        return rotation.RotateVec(spriteOffset);
    }

    /// <summary>
    /// Returns the entity's visual facing in emitter angle space.
    /// </summary>
    public Angle GetFacingEmitAngle(EntityUid source)
    {
        if (TerminatingOrDeleted(source))
            return Angle.Zero;

        TryComp<SpriteComponent>(source, out var sprite);
        var facing = GetFacingDirection(source, sprite);
        var screenDirection = GetFacingScreenDirection(facing, sprite);

        return new Angle(MathF.Atan2(screenDirection.X, screenDirection.Y));
    }

    /// <summary>
    /// Returns the visual facing direction in world space, including RSI direction, lying rotation, and eye rotation.
    /// </summary>
    public Vector2 GetFacingWorldDirection(EntityUid source)
    {
        if (TerminatingOrDeleted(source))
            return Vector2.Zero;

        TryComp<SpriteComponent>(source, out var sprite);
        var facing = GetFacingDirection(source, sprite);
        var screenDirection = GetFacingScreenDirection(facing, sprite);
        return (-_eye.CurrentEye.Rotation).RotateVec(screenDirection);
    }

    /// <summary>
    /// Возвращает направление вверх по экрану в мировых координатах.
    /// </summary>
    public Vector2 GetScreenUpWorldDirection()
        => (-_eye.CurrentEye.Rotation).RotateVec(Vector2.UnitY);

    private Direction GetFacingDirection(EntityUid source, SpriteComponent? sprite)
    {
        if (TerminatingOrDeleted(source))
            return Direction.South;

        var directionType = GetDirectionType(sprite);
        if (sprite is { EnableDirectionOverride: true })
            return sprite.DirectionOverride.Convert(directionType).Convert();

        var screenAngle = (_transform.GetWorldRotation(source) + _eye.CurrentEye.Rotation)
            .Reduced()
            .FlipPositive();
        return SpriteComponent.Layer.GetDirection(directionType, screenAngle).Convert();
    }

    private static Vector2 GetFacingScreenDirection(Direction facing, SpriteComponent? sprite)
    {
        var direction = facing.ToVec();

        // Rotation задаёт итоговый поворот лежащего спрайта уже после выбора RSI-кадра.
        return sprite == null
            ? direction
            : sprite.Rotation.RotateVec(direction);
    }

    private static float GetForwardFactor(ParticleVisualAnchor anchor, Direction facing)
    {
        return anchor switch
        {
            ParticleVisualAnchor.Head => HeadForwardFactor,
            ParticleVisualAnchor.Eyes => facing switch
            {
                Direction.North or Direction.NorthEast or Direction.NorthWest => EyesNorthForwardFactor,
                Direction.South => EyesSouthForwardFactor,
                _ => EyesSideForwardFactor,
            },
            ParticleVisualAnchor.Mouth => facing switch
            {
                Direction.North or Direction.NorthEast or Direction.NorthWest => MouthNorthForwardFactor,
                Direction.South => MouthSouthForwardFactor,
                _ => MouthSideForwardFactor,
            },
            ParticleVisualAnchor.Hands => HandsForwardFactor,
            _ => 0f,
        };
    }

    private static float GetHeightFactor(ParticleVisualAnchor anchor)
    {
        return anchor switch
        {
            ParticleVisualAnchor.Head => 0.9f,
            ParticleVisualAnchor.Eyes => 0.84f,
            ParticleVisualAnchor.Mouth => 0.76f,
            ParticleVisualAnchor.Hands => 0.58f,
            ParticleVisualAnchor.Feet => 0.08f,
            ParticleVisualAnchor.ItemTip => ItemTipVerticalFactor,
            _ => 0.5f,
        };
    }

    private static Vector2 GetLocalAnchorOffset(
        ParticleVisualAnchor anchor,
        float left,
        float bottom,
        float width,
        float height,
        float lateralOffset)
    {
        var horizontalPosition = anchor == ParticleVisualAnchor.ItemTip
            ? left + width * ItemTipHorizontalFactor + lateralOffset
            : lateralOffset;
        var verticalPosition = bottom + height * GetHeightFactor(anchor);
        return new Vector2(horizontalPosition, verticalPosition);
    }

    private RsiDirectionType GetDirectionType(SpriteComponent? sprite)
    {
        if (sprite == null)
            return RsiDirectionType.Dir4;

        var directionType = RsiDirectionType.Dir1;
        foreach (var spriteLayer in sprite.AllLayers)
        {
            if (spriteLayer is not SpriteComponent.Layer { Visible: true } layer)
                continue;

            var layerDirections = _sprite.LayerGetDirections(layer);
            if (layerDirections == RsiDirectionType.Dir8)
                return RsiDirectionType.Dir8;

            if (layerDirections == RsiDirectionType.Dir4)
                directionType = RsiDirectionType.Dir4;
        }

        // Однонаправленный слой не отменяет смысловой поворот сущности для привязанных эффектов.
        return directionType == RsiDirectionType.Dir1
            ? RsiDirectionType.Dir4
            : directionType;
    }

    private bool TryGetVisualBounds(EntityUid source, out MapCoordinates coordinates, out Box2Rotated bounds)
    {
        coordinates = default;
        bounds = default;

        if (TerminatingOrDeleted(source) || !TryComp<SpriteComponent>(source, out var sprite))
            return false;

        coordinates = _transform.GetMapCoordinates(source);
        bounds = _sprite.CalculateBounds(
            (source, sprite),
            coordinates.Position,
            _transform.GetWorldRotation(source),
            _eye.CurrentEye.Rotation);
        return bounds.Box.Width > MinimumUsableWidth && bounds.Box.Height > MinimumUsableHeight;
    }
}

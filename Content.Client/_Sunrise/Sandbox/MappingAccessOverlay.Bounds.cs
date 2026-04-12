using System.Numerics;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Physics;

namespace Content.Client._Sunrise.Sandbox;

public sealed partial class MappingAccessOverlay
{
    /*
     * World-bounds and body-filter helpers used by the overlay renderer.
     */
    private Box2 GetWorldBounds(
        EntityUid uid,
        SpriteComponent sprite,
        TransformComponent transform,
        in OverlayDrawArgs args)
    {
        if (sprite.Visible)
        {
            var (worldPos, worldRot) = _transformSystem.GetWorldPositionRotation(transform);
            return _spriteSystem.CalculateBounds((uid, sprite), worldPos, worldRot, args.Viewport.Eye?.Rotation ?? Angle.Zero)
                .CalcBoundingBox();
        }

        return _entityLookup.GetWorldAABB(uid);
    }

    private static bool IntersectsViewport(
        Vector2 viewportSize,
        Vector2 topLeft,
        Vector2 topRight,
        Vector2 bottomLeft,
        Vector2 bottomRight)
    {
        var minX = MathF.Min(MathF.Min(topLeft.X, topRight.X), MathF.Min(bottomLeft.X, bottomRight.X));
        var maxX = MathF.Max(MathF.Max(topLeft.X, topRight.X), MathF.Max(bottomLeft.X, bottomRight.X));
        var minY = MathF.Min(MathF.Min(topLeft.Y, topRight.Y), MathF.Min(bottomLeft.Y, bottomRight.Y));
        var maxY = MathF.Max(MathF.Max(topLeft.Y, topRight.Y), MathF.Max(bottomLeft.Y, bottomRight.Y));

        return maxX > 0f &&
               maxY > 0f &&
               minX < viewportSize.X &&
               minY < viewportSize.Y;
    }

    private static bool MatchesBodyFilter(BodyType bodyType, MappingAccessBodyFilter filter)
    {
        return filter switch
        {
            MappingAccessBodyFilter.Static => (bodyType & BodyType.Static) != 0,
            MappingAccessBodyFilter.Dynamic => (bodyType & BodyType.Dynamic) != 0,
            _ => (bodyType & (BodyType.Static | BodyType.Dynamic)) != 0,
        };
    }
}

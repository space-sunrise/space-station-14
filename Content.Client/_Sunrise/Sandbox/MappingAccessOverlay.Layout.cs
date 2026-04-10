using System.Numerics;

namespace Content.Client._Sunrise.Sandbox;

public sealed partial class MappingAccessOverlay
{
    private bool TryGetBackgroundRect(
        EntityUid uid,
        UIBox2 outlineRect,
        Vector2 backgroundSize,
        Vector2 viewportSize,
        float screenPadding,
        float horizontalMargin,
        float verticalMargin,
        out UIBox2 backgroundRect)
    {
        var placement = _placementCache.GetValueOrDefault(uid, LabelPlacement.Below);
        if (TryResolvePlacement(
                outlineRect,
                backgroundSize,
                viewportSize,
                screenPadding,
                horizontalMargin,
                verticalMargin,
                placement,
                checkOverlap: true,
                out backgroundRect))
        {
            return true;
        }

        if (TryResolveFallbackPlacement(
                uid,
                outlineRect,
                backgroundSize,
                viewportSize,
                screenPadding,
                horizontalMargin,
                verticalMargin,
                placement,
                out backgroundRect))
        {
            return true;
        }

        if (TryResolvePlacement(
                outlineRect,
                backgroundSize,
                viewportSize,
                screenPadding,
                horizontalMargin,
                verticalMargin,
                placement,
                checkOverlap: false,
                out backgroundRect))
        {
            return true;
        }

        if (placement != LabelPlacement.Below &&
            TryResolvePlacement(
                outlineRect,
                backgroundSize,
                viewportSize,
                screenPadding,
                horizontalMargin,
                verticalMargin,
                LabelPlacement.Below,
                checkOverlap: false,
                out backgroundRect))
        {
            _placementCache[uid] = LabelPlacement.Below;
            return true;
        }

        return false;
    }

    private bool TryResolveFallbackPlacement(
        EntityUid uid,
        UIBox2 outlineRect,
        Vector2 backgroundSize,
        Vector2 viewportSize,
        float screenPadding,
        float horizontalMargin,
        float verticalMargin,
        LabelPlacement placement,
        out UIBox2 backgroundRect)
    {
        switch (placement)
        {
            case LabelPlacement.Below:
                if (TryResolvePlacement(outlineRect, backgroundSize, viewportSize, screenPadding, horizontalMargin, verticalMargin, LabelPlacement.Above, true, out backgroundRect))
                {
                    _placementCache[uid] = LabelPlacement.Above;
                    return true;
                }

                if (TryResolvePlacement(outlineRect, backgroundSize, viewportSize, screenPadding, horizontalMargin, verticalMargin, LabelPlacement.Right, true, out backgroundRect))
                {
                    _placementCache[uid] = LabelPlacement.Right;
                    return true;
                }

                if (TryResolvePlacement(outlineRect, backgroundSize, viewportSize, screenPadding, horizontalMargin, verticalMargin, LabelPlacement.Left, true, out backgroundRect))
                {
                    _placementCache[uid] = LabelPlacement.Left;
                    return true;
                }
                break;

            case LabelPlacement.Above:
                if (TryResolvePlacement(outlineRect, backgroundSize, viewportSize, screenPadding, horizontalMargin, verticalMargin, LabelPlacement.Right, true, out backgroundRect))
                {
                    _placementCache[uid] = LabelPlacement.Right;
                    return true;
                }

                if (TryResolvePlacement(outlineRect, backgroundSize, viewportSize, screenPadding, horizontalMargin, verticalMargin, LabelPlacement.Left, true, out backgroundRect))
                {
                    _placementCache[uid] = LabelPlacement.Left;
                    return true;
                }

                if (TryResolvePlacement(outlineRect, backgroundSize, viewportSize, screenPadding, horizontalMargin, verticalMargin, LabelPlacement.Below, true, out backgroundRect))
                {
                    _placementCache[uid] = LabelPlacement.Below;
                    return true;
                }
                break;

            case LabelPlacement.Right:
                if (TryResolvePlacement(outlineRect, backgroundSize, viewportSize, screenPadding, horizontalMargin, verticalMargin, LabelPlacement.Left, true, out backgroundRect))
                {
                    _placementCache[uid] = LabelPlacement.Left;
                    return true;
                }

                if (TryResolvePlacement(outlineRect, backgroundSize, viewportSize, screenPadding, horizontalMargin, verticalMargin, LabelPlacement.Below, true, out backgroundRect))
                {
                    _placementCache[uid] = LabelPlacement.Below;
                    return true;
                }

                if (TryResolvePlacement(outlineRect, backgroundSize, viewportSize, screenPadding, horizontalMargin, verticalMargin, LabelPlacement.Above, true, out backgroundRect))
                {
                    _placementCache[uid] = LabelPlacement.Above;
                    return true;
                }
                break;

            case LabelPlacement.Left:
                if (TryResolvePlacement(outlineRect, backgroundSize, viewportSize, screenPadding, horizontalMargin, verticalMargin, LabelPlacement.Below, true, out backgroundRect))
                {
                    _placementCache[uid] = LabelPlacement.Below;
                    return true;
                }

                if (TryResolvePlacement(outlineRect, backgroundSize, viewportSize, screenPadding, horizontalMargin, verticalMargin, LabelPlacement.Above, true, out backgroundRect))
                {
                    _placementCache[uid] = LabelPlacement.Above;
                    return true;
                }

                if (TryResolvePlacement(outlineRect, backgroundSize, viewportSize, screenPadding, horizontalMargin, verticalMargin, LabelPlacement.Right, true, out backgroundRect))
                {
                    _placementCache[uid] = LabelPlacement.Right;
                    return true;
                }
                break;
        }

        backgroundRect = default;
        return false;
    }

    private bool TryResolvePlacement(
        UIBox2 outlineRect,
        Vector2 backgroundSize,
        Vector2 viewportSize,
        float screenPadding,
        float horizontalMargin,
        float verticalMargin,
        LabelPlacement placement,
        bool checkOverlap,
        out UIBox2 backgroundRect)
    {
        backgroundRect = GetBackgroundRect(outlineRect, backgroundSize, placement, horizontalMargin, verticalMargin);

        if (!FitsViewport(backgroundRect, viewportSize, screenPadding))
            return false;

        return !checkOverlap || !IntersectsOccupied(backgroundRect);
    }

    private static UIBox2 GetBackgroundRect(
        UIBox2 outlineRect,
        Vector2 backgroundSize,
        LabelPlacement placement,
        float horizontalMargin,
        float verticalMargin)
    {
        var center = outlineRect.Center;

        return placement switch
        {
            LabelPlacement.Above => UIBox2.FromDimensions(
                new Vector2(center.X - backgroundSize.X * 0.5f, outlineRect.Top - verticalMargin - backgroundSize.Y),
                backgroundSize),
            LabelPlacement.Right => UIBox2.FromDimensions(
                new Vector2(outlineRect.Right + horizontalMargin, center.Y - backgroundSize.Y * 0.5f),
                backgroundSize),
            LabelPlacement.Left => UIBox2.FromDimensions(
                new Vector2(outlineRect.Left - horizontalMargin - backgroundSize.X, center.Y - backgroundSize.Y * 0.5f),
                backgroundSize),
            _ => UIBox2.FromDimensions(
                new Vector2(center.X - backgroundSize.X * 0.5f, outlineRect.Bottom + verticalMargin),
                backgroundSize),
        };
    }

    private bool IntersectsOccupied(UIBox2 rect)
    {
        foreach (var occupiedRect in _occupiedRects)
        {
            if (occupiedRect.Intersects(rect))
                return true;
        }

        return false;
    }

    private static bool FitsViewport(UIBox2 rect, Vector2 viewportSize, float screenPadding)
    {
        return rect.Left >= screenPadding &&
               rect.Top >= screenPadding &&
               rect.Right <= viewportSize.X - screenPadding &&
               rect.Bottom <= viewportSize.Y - screenPadding;
    }
}

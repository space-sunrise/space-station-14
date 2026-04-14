using System.Numerics;

namespace Content.Client._Sunrise.Sandbox.Access.Overlays;

public sealed partial class MappingAccessOverlay
{
    /*
     * Placement helpers that keep labels inside the viewport and avoid overlaps.
     */
    private bool TryGetBackgroundRect(
        UIBox2 outlineRect,
        Vector2 backgroundSize,
        Vector2 viewportSize,
        float screenPadding,
        float horizontalMargin,
        float verticalMargin,
        out UIBox2 backgroundRect,
        out LabelPlacement placement)
    {
        if (TryResolvePlacement(
                outlineRect,
                backgroundSize,
                viewportSize,
                screenPadding,
                horizontalMargin,
                verticalMargin,
                LabelPlacement.Below,
                checkOverlap: true,
                out backgroundRect))
        {
            placement = LabelPlacement.Below;
            return true;
        }

        if (TryResolveFallbackPlacement(
                outlineRect,
                backgroundSize,
                viewportSize,
                screenPadding,
                horizontalMargin,
                verticalMargin,
                checkOverlap: true,
                out backgroundRect,
                out placement))
        {
            return true;
        }

        return TryResolveFallbackPlacement(
            outlineRect,
            backgroundSize,
            viewportSize,
            screenPadding,
            horizontalMargin,
            verticalMargin,
            checkOverlap: false,
            out backgroundRect,
            out placement);
    }

    private bool TryResolveFallbackPlacement(
        UIBox2 outlineRect,
        Vector2 backgroundSize,
        Vector2 viewportSize,
        float screenPadding,
        float horizontalMargin,
        float verticalMargin,
        bool checkOverlap,
        out UIBox2 backgroundRect,
        out LabelPlacement placement)
    {
        if (TryResolvePlacement(
                outlineRect,
                backgroundSize,
                viewportSize,
                screenPadding,
                horizontalMargin,
                verticalMargin,
                LabelPlacement.Above,
                checkOverlap,
                out backgroundRect))
        {
            placement = LabelPlacement.Above;
            return true;
        }

        if (TryResolvePlacement(
                outlineRect,
                backgroundSize,
                viewportSize,
                screenPadding,
                horizontalMargin,
                verticalMargin,
                LabelPlacement.Right,
                checkOverlap,
                out backgroundRect))
        {
            placement = LabelPlacement.Right;
            return true;
        }

        if (TryResolvePlacement(
                outlineRect,
                backgroundSize,
                viewportSize,
                screenPadding,
                horizontalMargin,
                verticalMargin,
                LabelPlacement.Left,
                checkOverlap,
                out backgroundRect))
        {
            placement = LabelPlacement.Left;
            return true;
        }

        if (TryResolvePlacement(
                outlineRect,
                backgroundSize,
                viewportSize,
                screenPadding,
                horizontalMargin,
                verticalMargin,
                LabelPlacement.Below,
                checkOverlap,
                out backgroundRect))
        {
            placement = LabelPlacement.Below;
            return true;
        }

        backgroundRect = default;
        placement = LabelPlacement.Below;
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

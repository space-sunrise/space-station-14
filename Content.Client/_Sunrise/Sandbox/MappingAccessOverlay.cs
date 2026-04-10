using System.Numerics;
using System.Text;
using Robust.Client.GameObjects;
using Content.Client.Stylesheets;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client._Sunrise.Sandbox;

public sealed class MappingAccessOverlay : Overlay
{
    private const string OrLocKey = "mapping-access-overlay-or";
    private const float HorizontalMargin = 6f;
    private const float VerticalMargin = 4f;
    private const float ScreenPadding = 4f;
    private const float OutlinePadding = 1f;

    private static readonly Vector2 BackgroundPadding = new(4f, 3f);
    private static readonly Color TitleColor = Color.Aquamarine;
    private static readonly Color AccessColor = Color.Gold;
    private static readonly Color BackgroundColor = new Color(10, 12, 16).WithAlpha(0.72f);
    private static readonly Color OutlineColor = TitleColor.WithAlpha(0.85f);

    private readonly IEntityManager _entityManager;
    private readonly EntityLookupSystem _entityLookup;
    private readonly SpriteSystem _spriteSystem;
    private readonly SharedTransformSystem _transformSystem;
    private readonly IPrototypeManager _prototypeManager;
    private readonly ILocalizationManager _loc;
    private readonly IUserInterfaceManager _uiManager;
    private readonly Font _font;
    private readonly Font _fontBold;

    private readonly List<string> _accessLines = new(8);
    private readonly List<string> _groupAccessNames = new(8);
    private readonly List<UIBox2> _occupiedRects = new(16);
    private readonly StringBuilder _accessBuffer = new();
    private readonly Dictionary<EntityUid, LabelPlacement> _placementCache = new();

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    public MappingAccessOverlay(
        IEntityManager entityManager,
        EntityLookupSystem entityLookup,
        SpriteSystem spriteSystem,
        IPrototypeManager prototypeManager,
        ILocalizationManager loc,
        IResourceCache resourceCache,
        IUserInterfaceManager uiManager)
    {
        _entityManager = entityManager;
        _entityLookup = entityLookup;
        _spriteSystem = spriteSystem;
        _transformSystem = _entityManager.System<SharedTransformSystem>();
        _prototypeManager = prototypeManager;
        _loc = loc;
        _uiManager = uiManager;
        _font = resourceCache.NotoStack();
        _fontBold = resourceCache.NotoStack(variation: "Bold");
        ZIndex = 210;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (args.ViewportControl == null)
            return;

        var uiScale = _uiManager.RootControl.UIScale;
        var orText = _loc.GetString(OrLocKey);
        var lineHeight = MathF.Max(
            args.ScreenHandle.GetDimensions(_font, "Hg", uiScale).Y,
            args.ScreenHandle.GetDimensions(_fontBold, "Hg", uiScale).Y);
        var lineOffset = new Vector2(0f, lineHeight);
        var screenPadding = ScreenPadding * uiScale;
        var horizontalMargin = HorizontalMargin * uiScale;
        var verticalMargin = VerticalMargin * uiScale;
        _occupiedRects.Clear();
        var query = _entityManager.AllEntityQueryEnumerator<AccessReaderComponent, SpriteComponent, TransformComponent, MetaDataComponent>();

        while (query.MoveNext(out var uid, out var accessReader, out var sprite, out var transform, out var meta))
        {
            if (transform.MapID != args.MapId || !accessReader.Enabled || accessReader.AccessLists.Count == 0)
                continue;

            var aabb = GetWorldBounds(uid, sprite, transform, in args);
            if (!aabb.Intersects(in args.WorldAABB))
                continue;

            BuildAccessLines(accessReader, orText);
            if (_accessLines.Count == 0)
                continue;

            if (meta.EntityPrototype?.ID is not { } protoId)
                continue;

            var title = protoId;

            var blockWidth = args.ScreenHandle.GetDimensions(_fontBold, title, uiScale).X;
            foreach (var line in _accessLines)
            {
                var lineWidth = args.ScreenHandle.GetDimensions(_font, line, uiScale).X;
                if (lineWidth > blockWidth)
                    blockWidth = lineWidth;
            }

            var blockHeight = lineHeight * (_accessLines.Count + 1);
            var topLeft = args.ViewportControl.WorldToScreen(aabb.TopLeft);
            var topRight = args.ViewportControl.WorldToScreen(aabb.TopRight);
            var bottomLeft = args.ViewportControl.WorldToScreen(aabb.BottomLeft);
            var bottomRight = args.ViewportControl.WorldToScreen(aabb.BottomRight);

            if (!IntersectsViewport(args.Viewport.Size, topLeft, topRight, bottomLeft, bottomRight))
                continue;

            var minX = MathF.Min(MathF.Min(topLeft.X, topRight.X), MathF.Min(bottomLeft.X, bottomRight.X));
            var maxX = MathF.Max(MathF.Max(topLeft.X, topRight.X), MathF.Max(bottomLeft.X, bottomRight.X));
            var minY = MathF.Min(MathF.Min(topLeft.Y, topRight.Y), MathF.Min(bottomLeft.Y, bottomRight.Y));
            var maxY = MathF.Max(MathF.Max(topLeft.Y, topRight.Y), MathF.Max(bottomLeft.Y, bottomRight.Y));
            var outlinePadding = OutlinePadding * uiScale;
            var outlineRect = UIBox2.FromDimensions(
                new Vector2(minX - outlinePadding, minY - outlinePadding),
                new Vector2(maxX - minX, maxY - minY) + Vector2.One * (outlinePadding * 2f));
            args.ScreenHandle.DrawRect(outlineRect, OutlineColor, false);

            var scaledBackgroundPadding = BackgroundPadding * uiScale;
            var contentSize = new Vector2(blockWidth, blockHeight);
            var backgroundSize = contentSize + scaledBackgroundPadding * 2f;
            if (!TryGetBackgroundRect(
                    uid,
                    outlineRect,
                    backgroundSize,
                    args.Viewport.Size,
                    screenPadding,
                    horizontalMargin,
                    verticalMargin,
                    out var backgroundRect))
                continue;

            args.ScreenHandle.DrawRect(backgroundRect, BackgroundColor);
            _occupiedRects.Add(backgroundRect);

            var startPos = backgroundRect.TopLeft + scaledBackgroundPadding;

            var currentPos = startPos;
            args.ScreenHandle.DrawString(_fontBold, currentPos, title, uiScale, TitleColor);
            currentPos += lineOffset;

            foreach (var line in _accessLines)
            {
                args.ScreenHandle.DrawString(_font, currentPos, line, uiScale, AccessColor);
                currentPos += lineOffset;
            }
        }
    }

    private void BuildAccessLines(AccessReaderComponent reader, string orText)
    {
        _accessLines.Clear();

        foreach (var accessGroup in reader.AccessLists)
        {
            if (accessGroup.Count == 0)
                continue;

            _groupAccessNames.Clear();
            foreach (var access in accessGroup)
            {
                _groupAccessNames.Add(GetAccessName(access));
            }

            _groupAccessNames.Sort(CompareAccessText);
            _accessBuffer.Clear();

            for (var i = 0; i < _groupAccessNames.Count; i++)
            {
                if (i > 0)
                    _accessBuffer.Append(" + ");

                _accessBuffer.Append(_groupAccessNames[i]);
            }

            _accessLines.Add(_accessBuffer.ToString());
        }

        _accessLines.Sort(CompareAccessText);

        if (_accessLines.Count <= 1)
            return;

        _accessBuffer.Clear();
        for (var i = 0; i < _accessLines.Count; i++)
        {
            if (i > 0)
                _accessBuffer.Append(' ').Append(orText).Append(' ');

            _accessBuffer.Append(_accessLines[i]);
        }

        _accessLines.Clear();
        _accessLines.Add(_accessBuffer.ToString());
    }

    private string GetAccessName(ProtoId<AccessLevelPrototype> access)
    {
        if (_prototypeManager.Resolve(access, out var accessPrototype) &&
            !string.IsNullOrWhiteSpace(accessPrototype.Name))
        {
            return _loc.GetString(accessPrototype.Name);
        }

        return access.Id;
    }

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

    private static int CompareAccessText(string? left, string? right)
    {
        return string.Compare(left, right, StringComparison.CurrentCultureIgnoreCase);
    }

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

    private enum LabelPlacement : byte
    {
        Below,
        Above,
        Right,
        Left,
    }
}

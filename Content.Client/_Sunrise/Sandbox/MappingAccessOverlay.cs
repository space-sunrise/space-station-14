using System.Numerics;
using Robust.Client.GameObjects;
using Content.Client.Stylesheets;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Enums;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;

namespace Content.Client._Sunrise.Sandbox;

public sealed partial class MappingAccessOverlay : Overlay
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
    private readonly EntityQuery<PhysicsComponent> _physicsQuery;
    private readonly SpriteSystem _spriteSystem;
    private readonly SharedTransformSystem _transformSystem;
    private readonly IPrototypeManager _prototypeManager;
    private readonly ILocalizationManager _loc;
    private readonly IUserInterfaceManager _uiManager;
    private readonly Font _font;
    private readonly Font _fontBold;

    private readonly List<string> _accessLines = new(8);
    private readonly List<UIBox2> _occupiedRects = new(16);
    private readonly Dictionary<EntityUid, LabelPlacement> _placementCache = new();

    public MappingAccessBodyFilter BodyFilter { get; set; } = MappingAccessBodyFilter.Both;

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
        _physicsQuery = _entityManager.GetEntityQuery<PhysicsComponent>();
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
            if (transform.MapID != args.MapId ||
                !accessReader.Enabled ||
                accessReader.AccessLists.Count == 0)
            {
                continue;
            }

            if (!_physicsQuery.TryComp(uid, out var physics) || !MatchesBodyFilter(physics.BodyType, BodyFilter))
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

    private enum LabelPlacement : byte
    {
        Below,
        Above,
        Right,
        Left,
    }
}

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
    private readonly StringBuilder _accessBuffer = new();

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
        var lineHeight = MathF.Max(
            args.ScreenHandle.GetDimensions(_font, "Hg", uiScale).Y,
            args.ScreenHandle.GetDimensions(_fontBold, "Hg", uiScale).Y);
        var lineOffset = new Vector2(0f, lineHeight);
        var query = _entityManager.AllEntityQueryEnumerator<AccessReaderComponent, SpriteComponent, TransformComponent, MetaDataComponent>();

        while (query.MoveNext(out var uid, out var accessReader, out var sprite, out var transform, out var meta))
        {
            if (transform.MapID != args.MapId || !accessReader.Enabled || accessReader.AccessLists.Count == 0)
                continue;

            var aabb = GetWorldBounds(uid, sprite, transform, in args);
            if (!aabb.Intersects(in args.WorldAABB))
                continue;

            BuildAccessLines(accessReader);
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

            var anchor = new Vector2((minX + maxX) * 0.5f, maxY);
            var verticalOffset = VerticalMargin * uiScale;
            var screenPadding = ScreenPadding * uiScale;
            var scaledBackgroundPadding = BackgroundPadding * uiScale;
            var contentSize = new Vector2(blockWidth, blockHeight);
            var backgroundSize = contentSize + scaledBackgroundPadding * 2f;
            var backgroundPos = new Vector2(
                anchor.X - backgroundSize.X * 0.5f,
                anchor.Y + verticalOffset);

            if (backgroundPos.X < screenPadding ||
                backgroundPos.Y < screenPadding ||
                backgroundPos.X + backgroundSize.X > args.Viewport.Size.X - screenPadding ||
                backgroundPos.Y + backgroundSize.Y > args.Viewport.Size.Y - screenPadding)
            {
                continue;
            }

            var backgroundRect = UIBox2.FromDimensions(backgroundPos, backgroundSize);
            args.ScreenHandle.DrawRect(backgroundRect, BackgroundColor);

            var startPos = backgroundPos + scaledBackgroundPadding;

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

    private void BuildAccessLines(AccessReaderComponent reader)
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
                _accessBuffer.Append(" ИЛИ ");

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
}

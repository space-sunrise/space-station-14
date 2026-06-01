using System.Numerics;
using Content.Shared._Sunrise.Guardian;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._Sunrise.Guardian;

public sealed class GuardianLightningArcOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> UnshadedShader = "unshaded";

    [Dependency] private readonly IEntityManager _ent = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly EntityQuery<TransformComponent> _transformQuery;
    private readonly SharedTransformSystem _transform;
    private readonly SpriteSystem _sprite;
    private readonly ShaderInstance _unshadedShader;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    public GuardianLightningArcOverlay()
    {
        IoCManager.InjectDependencies(this);

        _transformQuery = _ent.GetEntityQuery<TransformComponent>();
        _transform = _ent.System<SharedTransformSystem>();
        _sprite = _ent.System<SpriteSystem>();
        _unshadedShader = _prototype.Index(UnshadedShader).InstanceUnique();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        var query = _ent.EntityQueryEnumerator<GuardianLightningArcComponent, TransformComponent>();
        while (query.MoveNext(out _, out var arc, out var xform))
        {
            if (CanDrawArc(args, arc, xform))
                return true;
        }

        return false;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;
        handle.SetTransform(Matrix3x2.Identity);
        handle.UseShader(_unshadedShader);

        var query = _ent.EntityQueryEnumerator<GuardianLightningArcComponent, TransformComponent>();
        while (query.MoveNext(out _, out var arc, out var xform))
        {
            if (!TryGetArcPoints(args, arc, xform, out var start, out var end))
                continue;

            DrawArc(handle, arc, start, end);
        }

        handle.UseShader(null);
        handle.SetTransform(Matrix3x2.Identity);
    }

    protected override void DisposeBehavior()
    {
        base.DisposeBehavior();

        _unshadedShader.Dispose();
    }

    private bool CanDrawArc(in OverlayDrawArgs args, GuardianLightningArcComponent arc, TransformComponent xform)
    {
        return TryGetArcPoints(args, arc, xform, out _, out _);
    }

    private bool TryGetArcPoints(
        in OverlayDrawArgs args,
        GuardianLightningArcComponent arc,
        TransformComponent xform,
        out Vector2 start,
        out Vector2 end)
    {
        start = default;
        end = default;

        if (!arc.VisualActive ||
            arc.VisualHost is not { } host ||
            xform.MapID != args.MapId ||
            !_transformQuery.TryComp(host, out var hostXform) ||
            hostXform.MapID != args.MapId)
        {
            return false;
        }

        start = _transform.GetWorldPosition(xform, _transformQuery);
        end = _transform.GetWorldPosition(hostXform, _transformQuery);
        return true;
    }

    private void DrawArc(
        DrawingHandleWorld handle,
        GuardianLightningArcComponent arc,
        Vector2 start,
        Vector2 end)
    {
        var diff = end - start;
        var length = diff.Length();

        if (length <= 0.01f)
            return;

        var texture = _sprite.GetFrame(arc.ArcSprite, _timing.CurTime);
        var textureSize = texture.Size / (float) EyeManager.PixelsPerMeter;

        if (textureSize.X <= 0f || textureSize.Y <= 0f)
            return;

        var direction = diff / length;
        var segmentCount = Math.Max(1, (int) MathF.Ceiling(length / textureSize.Y));
        var segmentLength = length / segmentCount;
        var angle = diff.ToWorldAngle() + arc.ArcSpriteRotation;
        var localBox = new Box2(
            -textureSize.X / 2f,
            -segmentLength / 2f,
            textureSize.X / 2f,
            segmentLength / 2f);

        for (var i = 0; i < segmentCount; i++)
        {
            var center = start + direction * (segmentLength * (i + 0.5f));
            var box = new Box2Rotated(localBox.Translated(center), angle, center);
            handle.DrawTextureRect(texture, box, arc.ArcColor);
        }
    }
}

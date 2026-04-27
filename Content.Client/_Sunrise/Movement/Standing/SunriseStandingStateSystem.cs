using System.Diagnostics.CodeAnalysis;
using Content.Client.Rotation;
using Content.Shared._Sunrise.Movement.Standing.Components;
using Content.Shared._Sunrise.Movement.Standing.Systems;
using Content.Shared.Rotation;
using Content.Shared.Standing;
using Content.Shared.Throwing;
using Robust.Client.GameObjects;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client._Sunrise.Movement.Standing;

public sealed class SunriseStandingStateSystem : SharedSunriseStandingStateSystem
{
    [Dependency] private readonly RotationVisualizerSystem _rotationVisualizer = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private static readonly Angle EastProneCrawlRotation = Angle.FromDegrees(-90);
    private static readonly Angle WestProneCrawlRotation = Angle.FromDegrees(90);

    private EntityQuery<ActiveProneCrawlVisualsComponent> _activeProneCrawlVisualsQuery;
    private EntityQuery<RotationVisualsComponent> _rotationVisualsQuery;
    private EntityQuery<SpriteComponent> _spriteQuery;
    private EntityQuery<TransformComponent> _transformQuery;

    public override void Initialize()
    {
        base.Initialize();

        _activeProneCrawlVisualsQuery = GetEntityQuery<ActiveProneCrawlVisualsComponent>();
        _rotationVisualsQuery = GetEntityQuery<RotationVisualsComponent>();
        _spriteQuery = GetEntityQuery<SpriteComponent>();
        _transformQuery = GetEntityQuery<TransformComponent>();

        SubscribeLocalEvent<StandingStateComponent, AppearanceChangeEvent>(
            OnAppearanceChanged,
            after: [typeof(RotationVisualizerSystem)]);
        SubscribeLocalEvent<ActiveProneCrawlVisualsComponent, ComponentStartup>(OnProneCrawlVisualsStartup);
        SubscribeLocalEvent<ActiveProneCrawlVisualsComponent, MoveEvent>(OnMove);
        SubscribeLocalEvent<ThrownItemComponent, ComponentStartup>(OnThrownItemStartup);
        SubscribeLocalEvent<ActiveProneCrawlVisualsComponent, ComponentShutdown>(OnProneCrawlVisualsShutdown);
    }

    private void OnAppearanceChanged(Entity<StandingStateComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        var drawDepth = !ent.Comp.Standing ? (int)DrawDepth.SmallMobs : (int)DrawDepth.Mobs;

        var sprite = (ent, args.Sprite);
        _sprite.SetDrawDepth(sprite, drawDepth);

        if (!TryGetActiveProneCrawlVisuals(ent, out var rotationVisuals))
        {
            RestoreProneCrawlVisuals(sprite);
            return;
        }

        ApplyProneCrawlVisuals(sprite, Transform(ent).LocalRotation, rotationVisuals);
    }

    private void OnProneCrawlVisualsStartup(Entity<ActiveProneCrawlVisualsComponent> ent, ref ComponentStartup args)
    {
        TryApplyActiveProneCrawlVisuals(ent);
    }

    private void OnMove(Entity<ActiveProneCrawlVisualsComponent> ent, ref MoveEvent args)
    {
        if (args.OldRotation.Equals(args.NewRotation) ||
            !TryGetActiveProneCrawlVisuals(ent, out var rotationVisuals) ||
            !_spriteQuery.TryComp(ent, out var sprite))
        {
            return;
        }

        ApplyProneCrawlVisuals((ent, sprite), args.NewRotation, rotationVisuals, false);
    }

    private void OnThrownItemStartup(Entity<ThrownItemComponent> ent, ref ComponentStartup args)
    {
        TryApplyActiveProneCrawlVisuals(ent);
    }

    private void OnProneCrawlVisualsShutdown(Entity<ActiveProneCrawlVisualsComponent> ent, ref ComponentShutdown args)
    {
        if (_spriteQuery.TryComp(ent, out var sprite))
            RestoreProneCrawlVisuals((ent, sprite));
        else
            RemComp<ProneCrawlVisualsComponent>(ent);
    }

    private bool TryGetActiveProneCrawlVisuals(
        EntityUid uid,
        [NotNullWhen(true)] out RotationVisualsComponent? rotationVisuals)
    {
        rotationVisuals = null;

        return _activeProneCrawlVisualsQuery.HasComponent(uid) &&
               _rotationVisualsQuery.TryComp(uid, out rotationVisuals);
    }

    private bool TryApplyActiveProneCrawlVisuals(EntityUid uid)
    {
        if (!TryGetActiveProneCrawlVisuals(uid, out var rotationVisuals) ||
            !_spriteQuery.TryComp(uid, out var sprite) ||
            !_transformQuery.TryComp(uid, out var xform))
        {
            return false;
        }

        ApplyProneCrawlVisuals((uid, sprite), xform.LocalRotation, rotationVisuals);
        return true;
    }

    private void ApplyProneCrawlVisuals(
        Entity<SpriteComponent> ent,
        Angle localRotation,
        RotationVisualsComponent rotationVisuals,
        bool animate = true)
    {
        // Use local facing: world rotation includes the randomized grid rotation.
        var direction = localRotation.GetDir();
        ApplyProneCrawlDirectionOverride(ent, direction);

        var rotation = GetProneCrawlRotation(direction);

        if (animate)
            _rotationVisualizer.AnimateSpriteRotation(ent, ent, rotation, rotationVisuals.AnimationTime);
        else
            _sprite.SetRotation(ent.AsNullable(), rotation);
    }

    private void ApplyProneCrawlDirectionOverride(Entity<SpriteComponent> ent, Direction direction)
    {
        if (!TryComp<ProneCrawlVisualsComponent>(ent.Owner, out var proneCrawlVisuals))
        {
            proneCrawlVisuals = EnsureComp<ProneCrawlVisualsComponent>(ent);
            proneCrawlVisuals.HadDirectionOverride = ent.Comp.EnableDirectionOverride;
            proneCrawlVisuals.DirectionOverride = ent.Comp.DirectionOverride;
        }

        ent.Comp.EnableDirectionOverride = true;
        ent.Comp.DirectionOverride = direction;
    }

    private void RestoreProneCrawlVisuals(Entity<SpriteComponent> ent)
    {
        if (!TryComp<ProneCrawlVisualsComponent>(ent, out var proneCrawlVisuals))
            return;

        ent.Comp.EnableDirectionOverride = proneCrawlVisuals.HadDirectionOverride;
        ent.Comp.DirectionOverride = proneCrawlVisuals.DirectionOverride;
        RemComp<ProneCrawlVisualsComponent>(ent);
    }

    private static Angle GetProneCrawlRotation(Direction direction)
    {
        return direction is Direction.East or Direction.NorthEast or Direction.SouthEast
            ? EastProneCrawlRotation
            : WestProneCrawlRotation;
    }
}

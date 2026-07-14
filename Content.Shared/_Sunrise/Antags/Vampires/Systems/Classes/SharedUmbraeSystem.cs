using Content.Shared._Sunrise.Antags.Vampires.Events;
using Content.Shared._Sunrise.Antags.Vampires.Components;
using Content.Shared._Sunrise.Antags.Vampires.Components.Effects;
using Content.Shared._Sunrise.Antags.Vampires.Components.Classes;
using Content.Shared.Actions;
using Content.Shared.Damage.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Stealth;
using Content.Shared.Stealth.Components;
using Robust.Shared.Timing;

namespace Content.Shared._Sunrise.Antags.Vampires.Systems.Classes;

public sealed class SharedUmbraeSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStealthSystem _stealth = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedVampireActionUseSystem _vampireActions = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VampireCloakOfDarknessActionEvent>(OnCloakOfDarkness);
        SubscribeLocalEvent<VampireShadowBoxingActionEvent>(OnShadowBoxing);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;

        var query = EntityQueryEnumerator<UmbraeComponent, TransformComponent, StealthComponent>();
        while (query.MoveNext(out var uid, out var umbrae, out var xform, out var stealth))
        {
            Entity<UmbraeComponent> ent = (uid, umbrae);

            if (!ent.Comp.CloakOfDarknessActive)
                continue;

            if (now < ent.Comp.NextCloakOfDarknessVisibilityUpdate)
                continue;

            ent.Comp.NextCloakOfDarknessVisibilityUpdate = now + ent.Comp.CloakOfDarknessVisibilityUpdateInterval;
            Dirty(ent);

            var visibility = GetCloakOfDarknessVisibility(ent, xform);
            _stealth.SetVisibility(ent, visibility, stealth);
        }

        var shadowBoxingQuery = EntityQueryEnumerator<ActiveVampireShadowBoxingComponent, UmbraeComponent>();
        while (shadowBoxingQuery.MoveNext(out var uid, out var active, out var umbrae))
        {
            if (now < active.EndTime && umbrae.ShadowBoxingActive)
                continue;

            StopShadowBoxing((uid, umbrae), "action-vampire-shadow-boxing-ends");
        }
    }

    private void OnCloakOfDarkness(VampireCloakOfDarknessActionEvent args)
    {
        var actionEntity = args.Action.Owner;

        if (args.Handled)
            return;

        if (!Exists(actionEntity))
            return;

        if (!_vampireActions.TryUse(args.Performer, actionEntity))
            return;

        Entity<UmbraeComponent> ent = (args.Performer, EnsureComp<UmbraeComponent>(args.Performer));
        if (ent.Comp.CloakOfDarknessActive)
        {
            DeactivateCloakOfDarkness(ent);
            _popup.PopupPredicted(Loc.GetString("action-vampire-cloak-of-darkness-stop"), ent, ent);
        }
        else
        {
            ActivateCloakOfDarkness(ent);
            _popup.PopupPredicted(Loc.GetString("action-vampire-cloak-of-darkness-start"), ent, ent);
        }

        if (_actions.GetAction(actionEntity) is { } action)
            _actions.SetToggled(action.AsNullable(), ent.Comp.CloakOfDarknessActive);

        args.Handled = true;
    }

    public void ActivateCloakOfDarkness(Entity<UmbraeComponent> ent)
    {
        ent.Comp.CloakOfDarknessActive = true;
        ent.Comp.NextCloakOfDarknessVisibilityUpdate = _timing.CurTime;

        ent.Comp.CloakHadStealthComponent = TryComp<StealthComponent>(ent, out var existingStealth);
        ent.Comp.CloakPreviousStealthEnabled = existingStealth?.Enabled ?? false;
        ent.Comp.CloakPreviousStealthVisibility = ent.Comp.CloakHadStealthComponent
            ? _stealth.GetVisibility(ent, existingStealth)
            : 1f;
        Dirty(ent);

        var stealth = existingStealth ?? EnsureComp<StealthComponent>(ent);
        _stealth.SetEnabled(ent, true, stealth);
        _stealth.SetVisibility(ent, ent.Comp.CloakOfDarknessMinVisibility, stealth);
    }

    public void DeactivateCloakOfDarkness(Entity<UmbraeComponent> ent)
    {
        ent.Comp.CloakOfDarknessActive = false;
        Dirty(ent);

        RestoreCloakStealth(ent);
    }

    private void RestoreCloakStealth(Entity<UmbraeComponent> ent)
    {
        if (!TryComp<StealthComponent>(ent, out var stealth))
            return;

        if (!ent.Comp.CloakHadStealthComponent)
        {
            RemComp<StealthComponent>(ent);
            return;
        }

        _stealth.SetEnabled(ent, ent.Comp.CloakPreviousStealthEnabled, stealth);
        _stealth.SetVisibility(ent, ent.Comp.CloakPreviousStealthVisibility, stealth);
    }

    private float GetCloakOfDarknessVisibility(Entity<UmbraeComponent> ent, TransformComponent xform)
    {
        var range = ent.Comp.CloakOfDarknessRevealRange;
        if (range <= 0f)
            return ent.Comp.CloakOfDarknessMinVisibility;

        var center = _transform.GetWorldPosition(xform);
        var closest = range;

        foreach (var target in _lookup.GetEntitiesInRange(xform.Coordinates, range))
        {
            if (target == ent.Owner)
                continue;

            if (!HasComp<HumanoidAppearanceComponent>(target) || HasComp<VampireComponent>(target))
                continue;

            if (TryComp<MobStateComponent>(target, out var mob)
                && mob.CurrentState == MobState.Dead)
                continue;

            var dist = (_transform.GetWorldPosition(Transform(target)) - center).Length();
            closest = MathF.Min(closest, dist);
        }

        var t = 1f - Math.Clamp(closest / range, 0f, 1f);
        return MathHelper.Lerp(ent.Comp.CloakOfDarknessMinVisibility, ent.Comp.CloakOfDarknessMaxVisibility, t);
    }

    private void OnShadowBoxing(VampireShadowBoxingActionEvent args)
    {
        var uid = args.Performer;
        var actionEntity = args.Action.Owner;

        if (args.Handled)
            return;

        if (!Exists(actionEntity))
            return;

        if (TryComp<UmbraeComponent>(uid, out var umbrae) && umbrae.ShadowBoxingActive)
        {
            StopShadowBoxing((uid, umbrae), "action-vampire-shadow-boxing-ends");
            args.Handled = true;
            return;
        }

        var target = args.Target;
        if (target == uid)
            return;

        if (!IsValidShadowBoxingTarget(target))
            return;

        if (!_vampireActions.TryUse(uid, actionEntity))
            return;

        var attempt = new VampireShadowBoxingStartAttemptEvent(uid, target);
        RaiseLocalEvent(uid, ref attempt, true);
        if (attempt.Cancelled)
            return;

        umbrae = EnsureComp<UmbraeComponent>(uid);
        Entity<UmbraeComponent> ent = (uid, umbrae);
        var now = _timing.CurTime;
        ent.Comp.ShadowBoxingActive = true;
        ent.Comp.ShadowBoxingEndTime = now + args.Duration;
        ent.Comp.ShadowBoxingTarget = target;
        Dirty(ent);

        var active = EnsureComp<ActiveVampireShadowBoxingComponent>(ent);
        active.Target = target;
        active.Range = args.Range;
        active.BrutePerTick = args.BrutePerTick;
        active.HitSound = args.HitSound;
        active.PunchEffectPrototype = args.PunchEffectPrototype;
        active.TickInterval = args.Interval;
        active.NextTick = now + args.Interval;
        active.EndTime = now + args.Duration;

        _popup.PopupPredicted(Loc.GetString("action-vampire-shadow-boxing-start"), ent, ent);
        args.Handled = true;
    }

    public void StopShadowBoxing(Entity<UmbraeComponent> ent, string popup)
    {
        ent.Comp.ShadowBoxingActive = false;
        ent.Comp.ShadowBoxingTarget = null;
        ent.Comp.ShadowBoxingEndTime = null;
        RemComp<ActiveVampireShadowBoxingComponent>(ent);
        Dirty(ent);
        _popup.PopupPredicted(Loc.GetString(popup), ent, ent);
    }

    private bool IsValidShadowBoxingTarget(EntityUid target)
    {
        if (!Exists(target))
            return false;

        if (!HasComp<HumanoidAppearanceComponent>(target))
            return false;

        if (!HasComp<DamageableComponent>(target))
            return false;

        if (TryComp<MobStateComponent>(target, out var mob) && mob.CurrentState == MobState.Dead)
            return false;

        return true;
    }
}

using System;
using Content.Shared.Movement.Systems;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Robust.Shared.Timing;

namespace Content.Shared._Fish.Abilities.Milira;

/// <summary>
/// Шейред система для полёта расы милира, оно использует другую систему для изменения масштаба крыльев, а также изменяет маркинг, и тратит стамину.
/// </summary>
public abstract class SharedWingFlightSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WingFlightComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeed);
        SubscribeLocalEvent<WingFlightComponent, RefreshFrictionModifiersEvent>(OnRefreshFriction);
        SubscribeLocalEvent<WingFlightComponent, DownAttemptEvent>(OnDownAttempt);
        SubscribeLocalEvent<WingFlightComponent, KnockDownAttemptEvent>(OnKnockDownAttempt);
        SubscribeLocalEvent<WingFlightComponent, DownedEvent>(OnDowned);
        SubscribeLocalEvent<WingFlightComponent, KnockedDownEvent>(OnKnockedDown);
    }

    /// <summary>
    /// Получение целевого масштаба при активном полёте.
    /// </summary>
    public float GetTargetScale(WingFlightComponent component, float staminaPercent)
    {
        staminaPercent = Math.Clamp(staminaPercent, 0f, 1f);
        var bonus = component.MaxScaleMultiplier - component.MinScaleMultiplier;
        return component.MinScaleMultiplier + bonus * staminaPercent;
    }

    private void OnRefreshMovementSpeed(Entity<WingFlightComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (!ent.Comp.FlightEnabled)
            return;

        args.ModifySpeed(ent.Comp.SpeedModifier);
    }

    private void OnRefreshFriction(Entity<WingFlightComponent> ent, ref RefreshFrictionModifiersEvent args)
    {
        if (!ent.Comp.FlightEnabled && !ent.Comp.InertiaActive)
            return;

        args.ModifyFriction(ent.Comp.FrictionModifier);
    }

    public void SetFlightEnabled(EntityUid uid, bool enabled, WingFlightComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        if (component.FlightEnabled == enabled)
            return;

        component.FlightEnabled = enabled;

        if (enabled)
            EnsureComp<ActiveWingFlightComponent>(uid);
        else
        {
            RemComp<ActiveWingFlightComponent>(uid);
            StartInertia(uid, component);
        }

        _movement.RefreshMovementSpeedModifiers(uid);
        _movement.RefreshFrictionModifiers(uid);

        Dirty(uid, component);
    }

    public void StartInertia(EntityUid uid, WingFlightComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        component.InertiaActive = true;
        component.InertiaEndTime = _timing.CurTime + component.InertiaDuration;
        EnsureComp<ActiveWingFlightComponent>(uid);

        _movement.RefreshFrictionModifiers(uid);

        Dirty(uid, component);
    }

    public void StopInertia(EntityUid uid, WingFlightComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        if (!component.InertiaActive)
            return;

        component.InertiaActive = false;
        component.InertiaEndTime = null;

        if (!component.FlightEnabled)
            RemComp<ActiveWingFlightComponent>(uid);

        _movement.RefreshFrictionModifiers(uid);
        Dirty(uid, component);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<ActiveWingFlightComponent, WingFlightComponent>();

        while (query.MoveNext(out var uid, out _, out var flightComp))
        {
            if (!flightComp.InertiaActive)
                continue;

            if (flightComp.InertiaEndTime == null)
                continue;

            if (curTime < flightComp.InertiaEndTime)
                continue;

            flightComp.InertiaActive = false;
            flightComp.InertiaEndTime = null;

            if (!flightComp.FlightEnabled)
                RemComp<ActiveWingFlightComponent>(uid);

            _movement.RefreshFrictionModifiers(uid);
            Dirty(uid, flightComp);
        }
    }

    private bool IsFlightOrInertiaActive(WingFlightComponent component)
    {
        return component.FlightEnabled || component.InertiaActive;
    }

    private void OnDownAttempt(Entity<WingFlightComponent> ent, ref DownAttemptEvent args)
    {
        if (IsFlightOrInertiaActive(ent.Comp))
            args.Cancel();
    }

    private void OnKnockDownAttempt(Entity<WingFlightComponent> ent, ref KnockDownAttemptEvent args)
    {
        if (IsFlightOrInertiaActive(ent.Comp))
            args.Cancelled = true;
    }

    private void OnDowned(Entity<WingFlightComponent> ent, ref DownedEvent args)
    {
        if (IsFlightOrInertiaActive(ent.Comp))
            _standing.Stand(ent.Owner, force: true);
    }

    private void OnKnockedDown(Entity<WingFlightComponent> ent, ref KnockedDownEvent args)
    {
        if (IsFlightOrInertiaActive(ent.Comp))
        {
            RemComp<KnockedDownComponent>(ent.Owner);
            _standing.Stand(ent.Owner, force: true);
        }
    }
}


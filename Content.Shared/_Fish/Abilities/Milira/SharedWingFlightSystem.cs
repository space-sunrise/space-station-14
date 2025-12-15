using System;
using Content.Shared.Movement.Systems;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Robust.Shared.Timing;

namespace Content.Shared._Fish.Abilities.Milira;

/// <summary>
/// Шейред система для полёта расы милира, оно использует другую систему для изменения масштаба крыльев, а также изменяет маркинг, и тратит стамину.
/// </summary>
public sealed class SharedWingFlightSystem : EntitySystem
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
    public float GetTargetScale(in WingFlightComponent component, float staminaPercent)
    {
        staminaPercent = Math.Clamp(staminaPercent, 0f, 1f);
        var bonus = component.MaxScaleMultiplier - component.MinScaleMultiplier;
        return component.MinScaleMultiplier + bonus * staminaPercent;
    }

    private void OnRefreshMovementSpeed(EntityUid uid, WingFlightComponent component, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (!component.FlightEnabled)
            return;

        args.ModifySpeed(component.SpeedModifier);
    }

    private void OnRefreshFriction(EntityUid uid, WingFlightComponent component, ref RefreshFrictionModifiersEvent args)
    {
        if (!component.FlightEnabled && !component.InertiaActive)
            return;

        args.ModifyFriction(component.FrictionModifier);
    }

    public void SetFlightEnabled(EntityUid uid, bool enabled, WingFlightComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        if (component.FlightEnabled == enabled)
            return;

        component.FlightEnabled = enabled;

        if (!enabled)
            StartInertia(uid, component);

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
        _movement.RefreshFrictionModifiers(uid);
        Dirty(uid, component);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<WingFlightComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.InertiaActive)
                continue;

            if (comp.InertiaEndTime == null)
                continue;

            if (curTime < comp.InertiaEndTime)
                continue;

            comp.InertiaActive = false;
            comp.InertiaEndTime = null;
            _movement.RefreshFrictionModifiers(uid);
            Dirty(uid, comp);
        }
    }

    private bool IsFlightOrInertiaActive(WingFlightComponent component)
    {
        return component.FlightEnabled || component.InertiaActive;
    }

    private void OnDownAttempt(EntityUid uid, WingFlightComponent component, ref DownAttemptEvent args)
    {
        if (IsFlightOrInertiaActive(component))
            args.Cancel();
    }

    private void OnKnockDownAttempt(EntityUid uid, WingFlightComponent component, ref KnockDownAttemptEvent args)
    {
        if (IsFlightOrInertiaActive(component))
            args.Cancelled = true;
    }

    private void OnDowned(EntityUid uid, WingFlightComponent component, ref DownedEvent args)
    {
        if (IsFlightOrInertiaActive(component))
            _standing.Stand(uid, force: true);
    }

    private void OnKnockedDown(EntityUid uid, WingFlightComponent component, ref KnockedDownEvent args)
    {
        if (IsFlightOrInertiaActive(component))
        {
            RemComp<KnockedDownComponent>(uid);
            _standing.Stand(uid, force: true);
        }
    }
}


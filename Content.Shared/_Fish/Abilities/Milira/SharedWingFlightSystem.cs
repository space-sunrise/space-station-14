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
    public float GetTargetScale(Entity<WingFlightComponent> ent, float staminaPercent)
    {
        staminaPercent = Math.Clamp(staminaPercent, 0f, 1f);
        var bonus = ent.Comp.MaxScaleMultiplier - ent.Comp.MinScaleMultiplier;
        return ent.Comp.MinScaleMultiplier + bonus * staminaPercent;
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

    public void SetFlightEnabled(Entity<WingFlightComponent> ent, bool enabled)
    {
        if (ent.Comp.FlightEnabled == enabled)
            return;

        ent.Comp.FlightEnabled = enabled;

        if (enabled)
            EnsureComp<ActiveWingFlightComponent>(ent.Owner);
        else
        {
            RemComp<ActiveWingFlightComponent>(ent.Owner);
            StartInertia(ent);
        }

        _movement.RefreshMovementSpeedModifiers(ent);
        _movement.RefreshFrictionModifiers(ent);

        Dirty(ent.Owner, ent.Comp);
    }

    public void StartInertia(Entity<WingFlightComponent> ent)
    {
        ent.Comp.InertiaActive = true;
        ent.Comp.InertiaEndTime = _timing.CurTime + ent.Comp.InertiaDuration;
        EnsureComp<ActiveWingFlightComponent>(ent.Owner);

        _movement.RefreshFrictionModifiers(ent);

        Dirty(ent.Owner, ent.Comp);
    }

    public void StopInertia(Entity<WingFlightComponent> ent)
    {
        if (!ent.Comp.InertiaActive)
            return;

        ent.Comp.InertiaActive = false;
        ent.Comp.InertiaEndTime = null;

        if (!ent.Comp.FlightEnabled)
            RemComp<ActiveWingFlightComponent>(ent.Owner);

        _movement.RefreshFrictionModifiers(ent.Owner);
        Dirty(ent.Owner, ent.Comp);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<ActiveWingFlightComponent, WingFlightComponent>();

        while (query.MoveNext(out var uid, out var activeComp, out var wingComp))
        {
            if (!wingComp.InertiaActive)
                continue;

            if (wingComp.InertiaEndTime == null)
                continue;

            if (curTime < wingComp.InertiaEndTime)
                continue;

            wingComp.InertiaActive = false;
            wingComp.InertiaEndTime = null;

            if (!wingComp.FlightEnabled)
                RemComp<ActiveWingFlightComponent>(uid);

            _movement.RefreshFrictionModifiers(uid);
            Dirty(uid, wingComp);
        }
    }

    private bool IsFlightOrInertiaActive(Entity<WingFlightComponent> ent)
    {
        return ent.Comp.FlightEnabled || ent.Comp.InertiaActive;
    }

    private void OnDownAttempt(Entity<WingFlightComponent> ent, ref DownAttemptEvent args)
    {
        if (IsFlightOrInertiaActive(ent))
            args.Cancel();
    }

    private void OnKnockDownAttempt(Entity<WingFlightComponent> ent, ref KnockDownAttemptEvent args)
    {
        if (IsFlightOrInertiaActive(ent))
            args.Cancelled = true;
    }

    private void OnDowned(Entity<WingFlightComponent> ent, ref DownedEvent args)
    {
        if (IsFlightOrInertiaActive(ent))
            _standing.Stand(ent.Owner, force: true);
    }

    private void OnKnockedDown(Entity<WingFlightComponent> ent, ref KnockedDownEvent args)
    {
        if (IsFlightOrInertiaActive(ent))
        {
            RemComp<KnockedDownComponent>(ent.Owner);
            _standing.Stand(ent.Owner, force: true);
        }
    }
}


using Content.Shared._Sunrise.Jump;
using Content.Shared._Sunrise.Movement.Standing.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Gravity;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Movement.Standing.Systems;

public abstract partial class SharedSunriseStandingStateSystem : EntitySystem
{
    [Dependency] private readonly SharedGravitySystem _gravity = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;

    private static readonly EntProtoId FallStatusEffectKey = "StatusEffectFall";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CanFallComponent, KnockedDownEvent>(OnDown);
        SubscribeLocalEvent<CanFallComponent, MoveInputEvent>(OnMoveInput);

        InitializeCrawlingFootstepModifier();
        InitializeProneCrawlMovement();
        InitializePronePulling();
    }

    private void OnMoveInput(Entity<CanFallComponent> ent, ref MoveInputEvent args)
    {
        ent.Comp.IsMoving = args.HasDirectionalMovement;
    }

    private void OnDown(Entity<CanFallComponent> ent, ref KnockedDownEvent ev)
    {
        TryFall(ent);
    }

    /// <summary>
    /// Checks whether the entity can enter the falling state.
    /// Returns false when the entity is weightless, has no movement input, is auto-standing,
    /// actively leaping, lacks enough stamina, has the jump status effect, or when
    /// <see cref="FallAttemptEvent"/> is cancelled.
    /// </summary>
    /// <param name="ent">Entity with the fall configuration to check.</param>
    /// <param name="autoStand">Prevents falling while the caller is auto-standing the entity.</param>
    /// <param name="quiet">Suppresses predicted failure popups when true.</param>
    /// <returns>True when all checks pass and the entity can fall; otherwise false.</returns>
    public bool CanFall(Entity<CanFallComponent> ent, bool autoStand, bool quiet = false)
    {
        if (_gravity.IsWeightless(ent.Owner) || !HasMovementInput(ent) || autoStand)
            return false;

        if (HasComp<ActiveLeaperComponent>(ent))
            return false;

        if (!TryComp<StaminaComponent>(ent, out var stamina))
            return false;

        var threshold = stamina.CritThreshold * (1 - ent.Comp.MinimumStamina);

        if (stamina.StaminaDamage >= threshold)
        {
            if (!quiet)
                _popup.PopupPredicted(Loc.GetString("cant-fall-no-stamina"), null, ent, ent);

            return false;
        }

        if (_statusEffects.HasEffectComp<JumpStatusEffectComponent>(ent))
            return false;

        var ev = new FallAttemptEvent();
        RaiseLocalEvent(ent, ref ev);

        return !ev.Cancelled;
    }

    private bool HasMovementInput(Entity<CanFallComponent> ent)
    {
        if (ent.Comp.IsMoving)
            return true;

        return TryComp<InputMoverComponent>(ent.Owner, out var mover) &&
               mover.HasDirectionalMovement;
    }

    /// <summary>
    /// Attempts to make the entity fall by throwing it with <see cref="ThrowingSystem.TryThrow"/>
    /// using <see cref="CanFallComponent.FallDistance"/> and <see cref="CanFallComponent.FallVelocity"/>,
    /// adding the fall status effect for <see cref="CanFallComponent.Duration"/>, and dealing stamina
    /// damage equal to <c>stamina.CritThreshold * ent.Comp.StaminaDamage</c> with resistance ignored.
    /// </summary>
    /// <param name="ent">Entity with the fall configuration to execute.</param>
    /// <returns>True when the fall side effects were applied; false when <see cref="CanFall"/> rejects the action.</returns>
    public bool TryFall(Entity<CanFallComponent> ent)
    {
        if (!CanFall(ent, autoStand: false))
            return false;

        if (!TryComp<StaminaComponent>(ent, out var stamina))
            return false;

        var xform = Transform(ent);
        var throwing = xform.LocalRotation.ToWorldVec() * ent.Comp.FallDistance;
        var direction = xform.Coordinates.Offset(throwing); // to make the character jump in the direction he's looking

        _throwing.TryThrow(ent, direction, ent.Comp.FallVelocity, doSpin: false);

        _statusEffects.TryAddStatusEffectDuration(ent,
            FallStatusEffectKey,
            ent.Comp.Duration);

        _stamina.TakeStaminaDamage(ent, stamina.CritThreshold * ent.Comp.StaminaDamage, null, ent, ent, ignoreResist: true);
        return true;
    }
}

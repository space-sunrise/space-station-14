using Content.Shared._Sunrise.Jump;
using Content.Shared._Sunrise.Standing.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Gravity;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Standing.Systems;

public abstract partial class SharedSunriseStandingStateSystem : EntitySystem
{
    [Dependency] private readonly SharedGravitySystem _gravity = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;

    private readonly EntProtoId _fallStatusEffectKey = "StatusEffectFall";

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
        if (!CanFall(ent, autoStand: false))
            return;

        TryFall(ent);
    }

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

    public bool TryFall(Entity<CanFallComponent> ent)
    {
        if (!TryComp<StaminaComponent>(ent, out var stamina))
            return false;

        var xform = Transform(ent);
        var throwing = xform.LocalRotation.ToWorldVec() * ent.Comp.FallDistance;
        var direction = xform.Coordinates.Offset(throwing); // to make the character jump in the direction he's looking

        _throwing.TryThrow(ent, direction, ent.Comp.FallVelocity, doSpin: false);

        _statusEffects.TryAddStatusEffectDuration(ent,
            _fallStatusEffectKey,
            ent.Comp.Duration);

        _stamina.TakeStaminaDamage(ent, stamina.CritThreshold * ent.Comp.StaminaDamage, null, ent, ent, ignoreResist: true);
        return true;
    }
}

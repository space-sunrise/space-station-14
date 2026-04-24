using Content.Shared._Sunrise.Movement.Carrying.Slowdown;
using Content.Shared.ActionBlocker;
using Content.Shared.Coordinates;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using JetBrains.Annotations;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;

namespace Content.Shared._Sunrise.Movement.Carrying;

public abstract partial class SharedCarryingSystem
{
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly CarryingSlowdownSystem _slowdown = default!;
    [Dependency] private readonly SharedVirtualItemSystem _virtualItem = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    #region Start carrying

    [PublicAPI]
    public bool TryStartCarry(EntityUid carrier, Entity<CarriableComponent?> target)
    {
        if (!Resolve(target, ref target.Comp, false))
            return false;

        if (!CanCarry(carrier, target))
            return false;

        StartCarry(carrier, target!);
        return true;
    }

    protected bool CanCarry(EntityUid carrier, Entity<CarriableComponent?> target)
    {
        if (carrier == target.Owner)
            return false;

        if (!Resolve(target, ref target.Comp, false))
            return false;

        var targetEv = new StartBeingCarryAttemptEvent(carrier);
        RaiseLocalEvent(target, ref targetEv);
        if (targetEv.Cancelled)
            return false;

        var carrierEv = new StartCarryAttemptEvent(target);
        RaiseLocalEvent(target, ref carrierEv);
        if (carrierEv.Cancelled)
            return false;

        if (!HasComp<MapGridComponent>(Transform(carrier).ParentUid))
            return false;

        if (HasComp<BeingCarriedComponent>(carrier) || HasComp<BeingCarriedComponent>(target))
            return false;

        if (_hands.CountFreeHands(carrier) < target.Comp.FreeHandsRequired)
            return false;

        if (!_interaction.InRangeUnobstructed(carrier, target.Owner, CarryInteractionRange))
            return false;

        if (!_mobState.IsAlive(carrier))
            return false;

        return true;
    }

    private void StartCarry(EntityUid carrier, Entity<CarriableComponent> target)
    {
        if (HasComp<BeingCarriedComponent>(carrier))
            TryDropCarried(carrier);

        if (TryComp<PullableComponent>(target, out var pullable))
            _pulling.TryStopPull(target, pullable, carrier);

        _transform.AttachToGridOrMap(carrier);
        _transform.SetCoordinates(target, carrier.ToCoordinates());
        _transform.SetParent(target, carrier);

        for (var i = 0; i < target.Comp.FreeHandsRequired; i++)
        {
            _virtualItem.TrySpawnVirtualItemInHand(target, carrier);
        }

        var carryingComp = EnsureComp<CarryingComponent>(carrier);
        ApplyCarrySlowdown(carrier, target);
        var carriedComp = EnsureComp<BeingCarriedComponent>(target);
        EnsureComp<KnockedDownComponent>(target);

        carryingComp.Target = target;
        carriedComp.Carrier = carrier;
        Dirty(target);
        Dirty(target, carriedComp);

        _actionBlocker.UpdateCanMove(target);
    }

    #endregion

    #region Drop carryied

    [PublicAPI]
    public bool TryDropCarriedByTarget(Entity<BeingCarriedComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        if (ent.Comp.Carrier == null)
            return false;

        return TryDropCarried(ent.Comp.Carrier.Value);
    }

    [PublicAPI]
    public bool TryDropCarried(Entity<CarryingComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        if (ent.Comp.Target == null)
            return false;

        DropCarried(ent, ent.Comp.Target.Value);
        return true;
    }

    private void DropCarried(EntityUid carrier, EntityUid target)
    {
        RemComp<KnockedDownComponent>(target);
        RemComp<CarryingComponent>(carrier); // get rid of this first so we don't recusrively fire that event
        RemComp<CarryingSlowdownComponent>(carrier);
        RemComp<BeingCarriedComponent>(target);

        _actionBlocker.UpdateCanMove(target);
        _virtualItem.DeleteInHandsMatching(carrier, target);

        _transform.AttachToGridOrMap(target);

        _standing.Stand(target);
        _movementSpeed.RefreshMovementSpeedModifiers(carrier);

        var ev = new CarryDroppedEvent();
        RaiseLocalEvent(target, ref ev);
    }

    #endregion

    #region Other APIs

    protected void ApplyCarrySlowdown(EntityUid carrier, EntityUid carried)
    {
        var massRatio = MassContest(carrier, carried);
        var mobState = TryComp<MobStateComponent>(carried, out var mobStateComp)
            ? mobStateComp.CurrentState
            : MobState.Invalid;

        var modifier = CalculateCarrySlowdownModifier(mobState, massRatio);
        _slowdown.SetModifier(carrier, modifier, modifier);
    }

    protected float MassContest(Entity<PhysicsComponent?> roller, Entity<PhysicsComponent?> target)
    {
        if (!Resolve(roller, ref roller.Comp, false) || !Resolve(target, ref target.Comp, false))
            return 1f;

        if (MathHelper.CloseTo(target.Comp.FixturesMass, 0f) || MathHelper.CloseTo(roller.Comp.FixturesMass, 0f))
            return 1f;

        return roller.Comp.FixturesMass / target.Comp.FixturesMass;
    }

    protected static float CalculateCarryThrowSpeed(float baseThrowSpeed, float massRatio)
    {
        var massModifier = MathF.Pow(MathF.Max(massRatio, 0f), CarryThrowMassExponent);
        var speed = baseThrowSpeed * CarryThrowSpeedModifier * massModifier;
        return Math.Clamp(speed, MinCarryThrowSpeed, MaxCarryThrowSpeed);
    }

    protected static float CalculateCarryThrowDistance(float throwSpeed)
    {
        var distance = throwSpeed * throwSpeed / CarryThrowGravity;
        return Math.Clamp(distance, MinCarryThrowDistance, MaxCarryThrowDistance);
    }

    protected static float CalculateCarrySlowdownModifier(MobState mobState, float massRatio)
    {
        var baseModifier = mobState is MobState.Critical or MobState.Dead
            ? IncapacitatedCarrySlowdownModifier
            : DefaultCarrySlowdownModifier;

        var massModifier = CalculateCarrySlowdownMassModifier(massRatio);
        var modifier = baseModifier * massModifier;

        return Math.Clamp(modifier, MinimumSpeedModifier, 1f);
    }

    private static float CalculateCarrySlowdownMassModifier(float massRatio)
    {
        if (massRatio <= 0f)
            return MinCarrySlowdownMassModifier;

        var modifier = 1f + MathF.Log2(massRatio) * CarrySlowdownMassInfluence;
        return Math.Clamp(modifier, MinCarrySlowdownMassModifier, MaxCarrySlowdownMassModifier);
    }

    private void ShowCarryPopup(string locString, Filter filter, PopupType type, EntityUid carrier, EntityUid target)
    {
        var message = Loc.GetString(locString,
            ("carrier", Identity.Name(carrier, EntityManager)),
            ("target", Identity.Name(target, EntityManager)));

        _popup.PopupPredicted(message, carrier, target, filter, true, type);
    }

    #endregion
}

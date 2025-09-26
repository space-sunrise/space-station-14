using Content.Shared.ActionBlocker;
using Content.Shared.DoAfter;
using Content.Shared.Hands.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Verbs;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Content.Shared.Carrying;
using Robust.Shared.Physics.Components;
using Content.Shared.Throwing;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs;
using Content.Shared.Hands;
using Content.Shared.Standing;
using Content.Shared.Movement.Events;
using Content.Shared.Interaction.Events;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Climbing.Events;
using Content.Shared.Buckle.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Coordinates;
using Content.Shared.Hands.EntitySystems;

namespace Content.Shared._Sunrise.Carrying;

public sealed class SharedCarryingSystem : EntitySystem
{
    private readonly float _maxThrowSpeed = 15f;
    private const float CarryDistanceThreshold = 0.1f;
    private const float BaseCarryTime = 1f;
    private const float MaxCarryTime = 5f;
    private const float SlowdownCoefficient = 0.15f;
    private const float MinimumSpeedModifier = 0.1f;
    private const float CarryInteractionRange = 0.75f;
    private const float BaseThrowSpeed = 3f;

    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly CarryingSlowdownSystem _slowdown = default!;
    [Dependency] private readonly SharedVirtualItemSystem _virtualItemSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
    [Dependency] private readonly PullingSystem _pullingSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ThrowingSystem _throwingSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly StandingStateSystem _standingState = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CarriableComponent, GetVerbsEvent<AlternativeVerb>>(AddCarryVerb);
        SubscribeLocalEvent<CarriableComponent, CarryDoAfterEvent>(OnDoAfter);

        SubscribeLocalEvent<CarryingComponent, BeforeThrowEvent>(OnBeforeThrow);
        SubscribeLocalEvent<CarryingComponent, VirtualItemDeletedEvent>(OnVirtualItemDeleted);
        SubscribeLocalEvent<CarryingComponent, EntParentChangedMessage>(OnParentChanged);
        SubscribeLocalEvent<CarryingComponent, MobStateChangedEvent>(OnMobStateChanged);

        SubscribeLocalEvent<BeingCarriedComponent, UpdateCanMoveEvent>(OnMoveAttempt);
        SubscribeLocalEvent<BeingCarriedComponent, StandAttemptEvent>(OnStandAttempt);
        SubscribeLocalEvent<BeingCarriedComponent, GettingInteractedWithAttemptEvent>(OnInteractedWith);
        SubscribeLocalEvent<BeingCarriedComponent, PullAttemptEvent>(OnPullAttempt);
        SubscribeLocalEvent<BeingCarriedComponent, StartClimbEvent>(OnStartClimb);
        SubscribeLocalEvent<BeingCarriedComponent, BuckledEvent>(OnBuckleChange);
        SubscribeLocalEvent<BeingCarriedComponent, InteractionAttemptEvent>(OnInteractionAttempt);
    }

    public override void Update(float frametime)
    {
        base.Update(frametime);

        var query = EntityQueryEnumerator<CarryingComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var carrier, out var carrierXform))
        {
            if (!TryComp(uid, out StandingStateComponent? standing))
                continue;

            if (!standing.Standing)
            {
                _popupSystem.PopupClient(Loc.GetString("carry-lying-cancel"), carrier.Carried, uid, PopupType.MediumCaution);
                DropCarried(uid, carrier.Carried);
                continue;
            }

            if (!TryComp(carrier.Carried, out TransformComponent? carriedXform))
                continue;

            if (!carrierXform.Coordinates.TryDistance(EntityManager, carriedXform.Coordinates, out var distance))
                continue;

            if (distance > CarryDistanceThreshold)
                DropCarried(uid, carrier.Carried);
        }
    }
    private void AddCarryVerb(EntityUid uid, CarriableComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (!CanCarry(args.User, uid, component))
            return;

        if (HasComp<CarryingComponent>(args.User)) // yeah not dealing with that
            return;

        if (HasComp<BeingCarriedComponent>(args.User) || HasComp<BeingCarriedComponent>(args.Target))
            return;

        if (!_interactionSystem.InRangeUnobstructed(args.User, uid, CarryInteractionRange))
            return;

        if (!_mobStateSystem.IsAlive(args.User))
            return;

        if (args.User == args.Target)
            return;

        AlternativeVerb verb = new()
        {
            Act = () =>
            {
                StartCarryDoAfter(args.User, uid);
            },
            Text = Loc.GetString("carry-verb"),
            Priority = 2
        };
        args.Verbs.Add(verb);
    }

    private void StartCarryDoAfter(EntityUid carrier, EntityUid carried)
    {
        TimeSpan length = TimeSpan.FromSeconds(BaseCarryTime);

        var mod = MassContest(carrier, carried);

        if (mod != 0)
            length /= mod;

        if (!HasComp<KnockedDownComponent>(carried))
            length *= 2f;

        if (TryComp<MobStateComponent>(carried, out var mobState) && mobState.CurrentState != MobState.Alive)
            length /= 2f;

        if (length >= TimeSpan.FromSeconds(MaxCarryTime))
        {
            _popupSystem.PopupPredicted(Loc.GetString("carry-too-heavy"), carried, carrier, PopupType.SmallCaution);
            return;
        }

        var ev = new CarryDoAfterEvent();
        var args = new DoAfterArgs(EntityManager, carrier, length, ev, carried, target: carried)
        {
            BreakOnMove = true,
            NeedHand = true,
            MovementThreshold = 0.01f
        };

        _doAfterSystem.TryStartDoAfter(args);


        ShowCarryPopup("carry-starting", Filter.Entities(carrier), PopupType.Medium, carrier, carried);
        ShowCarryPopup("carry-started", Filter.Entities(carried), PopupType.Medium, carrier, carried);
        ShowCarryPopup("carry-observed", Filter.PvsExcept(carrier).RemoveWhereAttachedEntity(e => e == carried), PopupType.MediumCaution, carrier, carried);
    }
    private void OnDoAfter(EntityUid uid, CarriableComponent component, CarryDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (!CanCarry(args.Args.User, uid, component))
            return;

        Carry(args.Args.User, uid, component);
        args.Handled = true;
    }

    private void OnBeforeThrow(EntityUid uid, CarryingComponent component, BeforeThrowEvent args)
    {
        if (!TryComp<VirtualItemComponent>(args.ItemUid, out var virtItem) || !HasComp<CarriableComponent>(virtItem.BlockingEntity))
            return;

        var multiplier = MassContest(uid, virtItem.BlockingEntity);

        var throwSpeed = Math.Min(BaseThrowSpeed * multiplier, _maxThrowSpeed);

        _throwingSystem.TryThrow(virtItem.BlockingEntity, args.Direction, throwSpeed, uid);
    }

    private void OnVirtualItemDeleted(EntityUid uid, CarryingComponent component, VirtualItemDeletedEvent args)
    {
        if (!HasComp<CarriableComponent>(args.BlockingEntity))
            return;

        DropCarried(uid, args.BlockingEntity);
    }

    private void OnParentChanged(EntityUid uid, CarryingComponent component, ref EntParentChangedMessage args)
    {
        var xform = Transform(uid);

        if (xform.ParentUid == args.OldParent)
            return;

        // Do not drop the carried entity if the new parent is a grid
        if (xform.ParentUid == xform.GridUid)
            return;

        DropCarried(uid, component.Carried);
    }


    private void OnMobStateChanged(EntityUid uid, CarryingComponent component, MobStateChangedEvent args)
    {
        DropCarried(uid, component.Carried);
    }

    private void OnMoveAttempt(EntityUid uid, BeingCarriedComponent component, UpdateCanMoveEvent args)
    {
        args.Cancel();
    }


    private void OnStandAttempt(EntityUid uid, BeingCarriedComponent component, StandAttemptEvent args)
    {
        args.Cancel();
    }


    private void OnInteractedWith(EntityUid uid, BeingCarriedComponent component, GettingInteractedWithAttemptEvent args)
    {
        if (args.Uid != component.Carrier)
            args.Cancelled = true;
    }


    private void OnPullAttempt(EntityUid uid, BeingCarriedComponent component, PullAttemptEvent args)
    {
        args.Cancelled = true;
    }


    private void OnStartClimb(EntityUid uid, BeingCarriedComponent component, ref StartClimbEvent args)
    {
        DropCarried(component.Carrier, uid);
    }


    private void OnBuckleChange(EntityUid uid, BeingCarriedComponent component, ref BuckledEvent args)
    {
        DropCarried(component.Carrier, uid);
    }


    private void OnInteractionAttempt(EntityUid uid, BeingCarriedComponent component, InteractionAttemptEvent args)
    {
        if (args.Target == null)
            return;

        var targetParent = Transform(args.Target.Value).ParentUid;

        if (args.Target.Value != component.Carrier && targetParent != component.Carrier && targetParent != uid)
            args.Cancelled = true;
    }

    private void Carry(EntityUid carrier, EntityUid carried, CarriableComponent component)
    {
        if (TryComp<PullableComponent>(carried, out var pullable))
            _pullingSystem.TryStopPull(carried, pullable, carrier);

        _transform.AttachToGridOrMap(carrier);
        _transform.SetCoordinates(carried, carrier.ToCoordinates());
        _transform.SetParent(carried, carrier);

        for (var i = 0; i < component.FreeHandsRequired; i++)
        {
            _virtualItemSystem.TrySpawnVirtualItemInHand(carried, carrier);
        }
        var carryingComp = EnsureComp<CarryingComponent>(carrier);
        ApplyCarrySlowdown(carrier, carried);
        var carriedComp = EnsureComp<BeingCarriedComponent>(carried);
        EnsureComp<KnockedDownComponent>(carried);

        carryingComp.Carried = carried;
        carriedComp.Carrier = carrier;

        _actionBlockerSystem.UpdateCanMove(carried);
    }
    private void ApplyCarrySlowdown(EntityUid carrier, EntityUid carried)
    {
        var massRatio = MassContest(carrier, carried);

        if (massRatio == 0)
            massRatio = 1;

        // Формула замедления: чем меньше соотношение масс, тем больше замедление
        // При равных массах (ratio = 1) модификатор = 0.85
        var massRatioSq = Math.Pow(massRatio, 2);
        var modifier = 1 - (SlowdownCoefficient / massRatioSq);
        modifier = Math.Max(MinimumSpeedModifier, modifier);
        var slowdownComp = EnsureComp<CarryingSlowdownComponent>(carrier);
        _slowdown.SetModifier(carrier, (float)modifier, (float)modifier, slowdownComp);
    }

    private void ShowCarryPopup(string locString, Filter filter, PopupType type, EntityUid carrier, EntityUid carried)
    {
        _popupSystem.PopupPredicted(Loc.GetString(locString, ("carrier", Identity.Name(carrier, EntityManager)), ("target", Identity.Name(carried, EntityManager))), carrier, carried, filter, true, type);
    }

    public float MassContest(EntityUid roller, EntityUid target, PhysicsComponent? rollerPhysics = null, PhysicsComponent? targetPhysics = null)
    {
        if (!Resolve(roller, ref rollerPhysics, false) || !Resolve(target, ref targetPhysics, false))
            return 1f;

        if (targetPhysics.FixturesMass == 0)
            return 1f;

        return rollerPhysics.FixturesMass / targetPhysics.FixturesMass;
    }
    public bool CanCarry(EntityUid carrier, EntityUid carried, CarriableComponent? carriedComp = null)
    {
        if (!Resolve(carried, ref carriedComp, false))
            return false;

        if (!HasComp<MapGridComponent>(Transform(carrier).ParentUid))
            return false;

        if (HasComp<BeingCarriedComponent>(carrier) || HasComp<BeingCarriedComponent>(carried))
            return false;

        if (_handsSystem.CountFreeHands(carrier) < carriedComp.FreeHandsRequired)
            return false;

        return true;
    }

    public void DropCarried(EntityUid carrier, EntityUid carried)
    {
        RemComp<KnockedDownComponent>(carried);
        RemComp<CarryingComponent>(carrier); // get rid of this first so we don't recusrively fire that event
        RemComp<CarryingSlowdownComponent>(carrier);
        RemComp<BeingCarriedComponent>(carried);

        _actionBlockerSystem.UpdateCanMove(carried);
        _virtualItemSystem.DeleteInHandsMatching(carrier, carried);

        _transform.AttachToGridOrMap(carried);

        _standingState.Stand(carried);
        _movementSpeed.RefreshMovementSpeedModifiers(carrier);

        var ev = new CarryDroppedEvent();
        RaiseLocalEvent(carried, ref ev);
    }
}

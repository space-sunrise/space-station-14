using System.Numerics;
using Content.Shared.DoAfter;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Verbs;
using Robust.Shared.Player;
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

namespace Content.Shared._Sunrise.Movement.Carrying;

public abstract partial class SharedCarryingSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;

    private const float CarryThrowGravity = 8f;
    private const float CarryThrowSpeedModifier = 0.36f;
    private const float CarryThrowMassExponent = 0.25f;
    private const float MinCarryThrowSpeed = 1f;
    private const float MaxCarryThrowSpeed = 4.5f;
    private const float MinCarryThrowDistance = 0.25f;
    private const float MaxCarryThrowDistance = 2f;
    private const float CarryDistanceThreshold = 0.1f;
    private const float BaseCarryTime = 1f;
    private const float MaxCarryTime = 5f;
    private const float DefaultCarrySlowdownModifier = 0.6f;
    private const float IncapacitatedCarrySlowdownModifier = 0.8f;
    private const float CarrySlowdownMassInfluence = 0.1f;
    private const float MinCarrySlowdownMassModifier = 0.5f;
    private const float MaxCarrySlowdownMassModifier = 1.2f;
    private const float MinimumSpeedModifier = 0.1f;
    private const float CarryInteractionRange = 0.75f;

    private EntityQuery<StandingStateComponent> _standingStateQuery;

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

        _standingStateQuery = GetEntityQuery<StandingStateComponent>();
    }

    #region Update

    public override void Update(float frametime)
    {
        base.Update(frametime);

        var query = EntityQueryEnumerator<CarryingComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            if (comp.Target == null)
                continue;

            var target = comp.Target.Value;
            if (_standingStateQuery.TryComp(uid, out var standing) && !standing.Standing)
            {
                _popup.PopupClient(Loc.GetString("carry-lying-cancel"), target, uid, PopupType.MediumCaution);
                DropCarried(uid, target);
                continue;
            }

            if (!xform.Coordinates.TryDistance(EntityManager, Transform(target).Coordinates, out var distance))
                continue;

            if (distance > CarryDistanceThreshold)
                DropCarried(uid, target);
        }
    }

    #endregion

    #region Event handlers

    private void AddCarryVerb(Entity<CarriableComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (!CanCarry(args.User, ent.AsNullable()))
            return;

        var user = args.User;
        AlternativeVerb verb = new()
        {
            Act = () =>
            {
                StartCarryDoAfter(user, ent);
            },
            Text = Loc.GetString("carry-verb"),
            Priority = 2,
        };
        args.Verbs.Add(verb);
    }

    private void OnDoAfter(Entity<CarriableComponent> ent, ref CarryDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        TryStartCarry(args.Args.User, ent.AsNullable());
        args.Handled = true;
    }

    private void OnBeforeThrow(Entity<CarryingComponent> ent, ref BeforeThrowEvent args)
    {
        if (args.Direction == Vector2.Zero)
            return;

        if (!TryComp<VirtualItemComponent>(args.ItemUid, out var item) || !HasComp<CarriableComponent>(item.BlockingEntity))
            return;

        var target = item.BlockingEntity;
        var direction = args.Direction.Normalized();
        var massRatio = MassContest(ent.Owner, target);
        var throwSpeed = CalculateCarryThrowSpeed(args.ThrowSpeed, massRatio);
        var throwDistance = CalculateCarryThrowDistance(throwSpeed);

        if (!TryDropCarried(ent.AsNullable()))
            return;

        _throwing.TryThrow(
            target,
            direction * throwDistance,
            throwSpeed,
            args.PlayerUid,
            pushbackRatio: 0f,
            compensateFriction: true,
            doSpin: false);

        args.Cancelled = true;
    }

    private void OnVirtualItemDeleted(Entity<CarryingComponent> ent, ref VirtualItemDeletedEvent args)
    {
        if (!HasComp<CarriableComponent>(args.BlockingEntity))
            return;

        TryDropCarried(ent.AsNullable());
    }

    private void OnParentChanged(Entity<CarryingComponent> ent, ref EntParentChangedMessage args)
    {
        var xform = Transform(ent);
        if (xform.ParentUid == args.OldParent)
            return;

        // Do not drop the carried entity if the new parent is a grid
        if (xform.ParentUid == xform.GridUid)
            return;

        TryDropCarried(ent.AsNullable());
    }

    private void OnMobStateChanged(Entity<CarryingComponent> ent, ref MobStateChangedEvent args)
    {
        TryDropCarried(ent.AsNullable());
    }

    private void OnMoveAttempt(Entity<BeingCarriedComponent> ent, ref UpdateCanMoveEvent args)
    {
        args.Cancel();
    }

    private void OnStandAttempt(Entity<BeingCarriedComponent> ent, ref StandAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnInteractedWith(Entity<BeingCarriedComponent> ent, ref GettingInteractedWithAttemptEvent args)
    {
        if (args.Uid != ent.Comp.Carrier)
            args.Cancelled = true;
    }

    private void OnPullAttempt(Entity<BeingCarriedComponent> ent, ref PullAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnStartClimb(Entity<BeingCarriedComponent> ent, ref StartClimbEvent args)
    {
        TryDropCarriedByTarget(ent.AsNullable());
    }

    private void OnBuckleChange(Entity<BeingCarriedComponent> ent, ref BuckledEvent args)
    {
        TryDropCarriedByTarget(ent.AsNullable());
    }

    private void OnInteractionAttempt(Entity<BeingCarriedComponent> ent, ref InteractionAttemptEvent args)
    {
        if (args.Target == null)
            return;

        var targetParent = Transform(args.Target.Value).ParentUid;

        if (args.Target.Value != ent.Comp.Carrier && targetParent != ent.Comp.Carrier && targetParent != ent.Owner)
            args.Cancelled = true;
    }

    #endregion

    private void StartCarryDoAfter(EntityUid carrier, EntityUid carried)
    {
        var length = TimeSpan.FromSeconds(BaseCarryTime);

        var mod = MassContest(carrier, carried);

        if (mod != 0)
            length /= mod;

        if (!HasComp<KnockedDownComponent>(carried))
            length *= 2f;

        if (TryComp<MobStateComponent>(carried, out var mobState) && mobState.CurrentState != MobState.Alive)
            length /= 2f;

        if (length >= TimeSpan.FromSeconds(MaxCarryTime))
        {
            _popup.PopupPredicted(Loc.GetString("carry-too-heavy"), carried, carrier, PopupType.SmallCaution);
            return;
        }

        var ev = new CarryDoAfterEvent();
        var args = new DoAfterArgs(EntityManager, carrier, length, ev, carried, target: carried)
        {
            BreakOnMove = true,
            NeedHand = true,
            MovementThreshold = 0.01f,
        };

        if (!_doAfter.TryStartDoAfter(args))
            return;

        ShowCarryPopup("carry-starting", Filter.Entities(carrier), PopupType.Medium, carrier, carried);
        ShowCarryPopup("carry-started", Filter.Entities(carried), PopupType.Medium, carrier, carried);
        ShowCarryPopup("carry-observed", Filter.PvsExcept(carrier).RemoveWhereAttachedEntity(e => e == carried), PopupType.MediumCaution, carrier, carried);
    }
}

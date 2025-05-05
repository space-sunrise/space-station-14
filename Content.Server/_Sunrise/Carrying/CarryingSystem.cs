using System.Threading;
using Content.Server.Carrying;
using Content.Server.DoAfter;
using Content.Server.Popups;
using Content.Server.Resist;
using Content.Shared._Sunrise.Carrying;
using Content.Shared.IdentityManagement;
using Content.Shared.ActionBlocker;
using Content.Shared.Buckle.Components;
using Content.Shared.Carrying;
using Content.Shared.Climbing.Events;
using Content.Shared.DoAfter;
using Content.Shared.Hands;
using Content.Shared.Popups;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Verbs;
using Robust.Shared.Player;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;

namespace Content.Server._Sunrise.Carrying
{
    public sealed class CarryingSystem : EntitySystem
    {
        private readonly float _maxThrowSpeed = 15f;

        [Dependency] private readonly SharedVirtualItemSystem _virtualItemSystem = default!;
        [Dependency] private readonly CarryingSlowdownSystem _slowdown = default!;
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly SharedStandingStateSystem _standingState = default!;
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
        [Dependency] private readonly PullingSystem _pullingSystem = default!;
        [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
        [Dependency] private readonly EscapeInventorySystem _escapeInventorySystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
        [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
        [Dependency] private readonly ThrowingSystem _throwingSystem = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<CarriableComponent, GetVerbsEvent<AlternativeVerb>>(AddCarryVerb);
            SubscribeLocalEvent<CarryingComponent, VirtualItemDeletedEvent>(OnVirtualItemDeleted);
            SubscribeLocalEvent<CarryingComponent, BeforeThrowEvent>(OnThrow);
            SubscribeLocalEvent<CarryingComponent, EntParentChangedMessage>(OnParentChanged);
            SubscribeLocalEvent<CarryingComponent, MobStateChangedEvent>(OnMobStateChanged);
            SubscribeLocalEvent<BeingCarriedComponent, InteractionAttemptEvent>(OnInteractionAttempt);
            SubscribeLocalEvent<BeingCarriedComponent, MoveInputEvent>(OnMoveInput);
            SubscribeLocalEvent<BeingCarriedComponent, UpdateCanMoveEvent>(OnMoveAttempt);
            SubscribeLocalEvent<BeingCarriedComponent, StandAttemptEvent>(OnStandAttempt);
            SubscribeLocalEvent<BeingCarriedComponent, GettingInteractedWithAttemptEvent>(OnInteractedWith);
            SubscribeLocalEvent<BeingCarriedComponent, PullAttemptEvent>(OnPullAttempt);
            SubscribeLocalEvent<BeingCarriedComponent, StartClimbEvent>(OnStartClimb);
            SubscribeLocalEvent<BeingCarriedComponent, BuckledEvent>(OnBuckleChange);
            SubscribeLocalEvent<CarriableComponent, CarryDoAfterEvent>(OnDoAfter);
        }

        private float MassContest(EntityUid roller, EntityUid target, PhysicsComponent? rollerPhysics = null, PhysicsComponent? targetPhysics = null)
        {
            if (!Resolve(roller, ref rollerPhysics, false) || !Resolve(target, ref targetPhysics, false))
                return 1f;

            if (targetPhysics.FixturesMass == 0)
                return 1f;

            return rollerPhysics.FixturesMass / targetPhysics.FixturesMass;
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

            if (!_interactionSystem.InRangeUnobstructed(args.User, uid, 0.75f))
                return;

            if (!_mobStateSystem.IsAlive(args.User))
                return;

            if (args.User == args.Target)
                return;

            AlternativeVerb verb = new()
            {
                Act = () =>
                {
                    StartCarryDoAfter(args.User, uid, component);
                },
                Text = Loc.GetString("carry-verb"),
                Priority = 2
            };
            args.Verbs.Add(verb);
        }

        private void OnVirtualItemDeleted(EntityUid uid, CarryingComponent component, VirtualItemDeletedEvent args)
        {
            if (!HasComp<CarriableComponent>(args.BlockingEntity))
                return;

            DropCarried(uid, args.BlockingEntity);
        }

        private void OnThrow(EntityUid uid, CarryingComponent component, BeforeThrowEvent args)
        {
            if (!TryComp<VirtualItemComponent>(args.ItemUid, out var virtItem) || !HasComp<CarriableComponent>(virtItem.BlockingEntity))
                return;

            var multiplier = MassContest(uid, virtItem.BlockingEntity);

            var throwSpeed = 3f * multiplier > _maxThrowSpeed ? _maxThrowSpeed : 3f * multiplier;

            _throwingSystem.TryThrow(virtItem.BlockingEntity, args.Direction, throwSpeed, uid);
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

        private void OnInteractionAttempt(EntityUid uid, BeingCarriedComponent component, InteractionAttemptEvent args)
        {
            if (args.Target == null)
                return;

            var targetParent = Transform(args.Target.Value).ParentUid;

            if (args.Target.Value != component.Carrier && targetParent != component.Carrier && targetParent != uid)
                args.Cancelled = true;
        }

        /// <summary>
        /// Try to escape via the escape inventory system.
        /// </summary>
        private void OnMoveInput(EntityUid uid, BeingCarriedComponent component, ref MoveInputEvent args)
        {
            if (!TryComp<CanEscapeInventoryComponent>(uid, out var escape))
                return;

            if (args.OldMovement == MoveButtons.None || args.OldMovement == MoveButtons.Walk)
                return;

            if (_actionBlockerSystem.CanInteract(uid, component.Carrier))
            {
                _escapeInventorySystem.AttemptEscape(uid, component.Carrier, escape, MassContest(component.Carrier, uid));
            }
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

        private void OnDoAfter(EntityUid uid, CarriableComponent component, CarryDoAfterEvent args)
        {
            component.CancelToken = null;
            if (args.Handled || args.Cancelled)
                return;

            if (!CanCarry(args.Args.User, uid, component))
                return;

            Carry(args.Args.User, uid, component);
            args.Handled = true;
        }
        private void StartCarryDoAfter(EntityUid carrier, EntityUid carried, CarriableComponent component)
        {
            TimeSpan length = TimeSpan.FromSeconds(1);

            var mod = MassContest(carrier, carried);

            if (mod != 0)
                length /= mod;

            if (length >= TimeSpan.FromSeconds(5))
            {
                _popupSystem.PopupEntity(Loc.GetString("carry-too-heavy"), carried, carrier, Shared.Popups.PopupType.SmallCaution);
                return;
            }

            if (!HasComp<KnockedDownComponent>(carried))
                length *= 2f;

            component.CancelToken = new CancellationTokenSource();

            var ev = new CarryDoAfterEvent();
            var args = new DoAfterArgs(EntityManager ,carrier, length, ev, carried, target: carried)
            {
                BreakOnMove = true,
                NeedHand = true
            };

            _doAfterSystem.TryStartDoAfter(args);


            ShowCarryPopup("carry-starting", Filter.Entities(carrier), PopupType.Medium, carrier, carried);
            ShowCarryPopup("carry-started", Filter.Entities(carried), PopupType.Medium, carrier, carried);
            ShowCarryPopup("carry-observed", Filter.PvsExcept(carrier).RemoveWhereAttachedEntity(e => e == carried), PopupType.MediumCaution, carrier, carried);
        }

        private void Carry(EntityUid carrier, EntityUid carried, CarriableComponent component)
        {
            if (TryComp<PullableComponent>(carried, out var pullable))
                _pullingSystem.TryStopPull(carried, pullable, carrier);

            Transform(carrier).AttachToGridOrMap();
            Transform(carried).Coordinates = Transform(carrier).Coordinates;
            Transform(carried).AttachParent(carrier);
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

        public void DropCarried(EntityUid carrier, EntityUid carried)
        {
            RemComp<CarryingComponent>(carrier); // get rid of this first so we don't recusrively fire that event
            RemComp<CarryingSlowdownComponent>(carrier);
            RemComp<BeingCarriedComponent>(carried);
            RemComp<KnockedDownComponent>(carried);
            _actionBlockerSystem.UpdateCanMove(carried);
            _virtualItemSystem.DeleteInHandsMatching(carrier, carried);
            if (_entityManager.TryGetComponent<TransformComponent>(carried, out var transformComponent))
            {
                transformComponent.AttachToGridOrMap();
            }
            else
            {
                Console.WriteLine($"TransformComponent отсутствует у сущности {carried}.");
            }
            _standingState.Stand(carried);
            _movementSpeed.RefreshMovementSpeedModifiers(carrier);

            var ev = new CarryDroppedEvent();
            RaiseLocalEvent(carried, ref ev);
        }

        private void ApplyCarrySlowdown(EntityUid carrier, EntityUid carried)
        {
            var massRatio = MassContest(carrier, carried);

            if (massRatio == 0)
                massRatio = 1;

            var massRatioSq = Math.Pow(massRatio, 2);
            var modifier = (1 - (0.15 / massRatioSq));
            modifier = Math.Max(0.1, modifier);
            var slowdownComp = EnsureComp<CarryingSlowdownComponent>(carrier);
            _slowdown.SetModifier(carrier, (float) modifier, (float) modifier, slowdownComp);
        }

        public bool CanCarry(EntityUid carrier, EntityUid carried, CarriableComponent? carriedComp = null)
        {
            if (!Resolve(carried, ref carriedComp, false))
                return false;

            if (carriedComp.CancelToken != null)
                return false;

            if (!HasComp<MapGridComponent>(Transform(carrier).ParentUid))
                return false;

            if (HasComp<BeingCarriedComponent>(carrier) || HasComp<BeingCarriedComponent>(carried))
                return false;

            if (!TryComp<HandsComponent>(carrier, out var hands))
                return false;

            if (hands.CountFreeHands() < carriedComp.FreeHandsRequired)
                return false;

            return true;
        }

        private void ShowCarryPopup(string locString, Filter filter, PopupType type, EntityUid carrier, EntityUid carried)
        {
            _popupSystem.PopupEntity(Loc.GetString(locString, ("carrier", Identity.Name(carrier, EntityManager)), ("target", Identity.Name(carried, EntityManager))),carrier, filter, true, type);
        }
    }
}

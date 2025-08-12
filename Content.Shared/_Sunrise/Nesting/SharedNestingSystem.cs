using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Content.Shared.DoAfter;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Strip.Components;

namespace Content.Shared._Sunrise.Nesting;

public abstract partial class SharedNestingSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public const string BaseStorageId = "storagebase";

    public override void Initialize()
    {
        SubscribeLocalEvent<NestingMobComponent, GetVerbsEvent<InteractionVerb>>(AddPickupVerb);
        SubscribeLocalEvent<NestingMobComponent, NestingMobPickupDoAfterEvent>(FinishPickup);
        SubscribeLocalEvent<NestingMobComponent, DoAfterAttemptEvent<NestingMobPickupDoAfterEvent>>(DuringPickup);

        SubscribeLocalEvent<NestingMobComponent, PickupAttemptEvent>(OnPickupAttempt);
        SubscribeLocalEvent<NestingMobComponent, BeingEquippedAttemptEvent>(OnBeingEquippedAttempt);
        SubscribeLocalEvent<NestingMobComponent, ContainerIsInsertingAttemptEvent>(OnHandEquippedAttempt);
        SubscribeLocalEvent<NestingMobComponent, UseAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<NestingMobComponent, ThrowAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<NestingMobComponent, InteractionAttemptEvent>(OnInteractAttempt);
        SubscribeLocalEvent<NestingMobComponent, PullAttemptEvent>(OnPullAttempt);
        SubscribeLocalEvent<NestingMobComponent, AttackAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<NestingMobComponent, GettingPickedUpAttemptEvent>(OnGettingPickupAttempt);

        SubscribeLocalEvent<NestingContainerComponent, GetVerbsEvent<AlternativeVerb>>(AddInsertAltVerb);
        SubscribeLocalEvent<NestingContainerComponent, NestingMobInsertDoAfterEvent>(OnInsertingDoAfter);
    }

    private void AddPickupVerb(Entity<NestingMobComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (args.Target == args.User) return;
        if (!InRange(args.User, args.Target)) return;
        if (_containerSystem.TryGetContainingContainer((args.User, null, null), out _)) return;

        var verb = new InteractionVerb
        {
            Text = Loc.GetString("pick-up-verb-get-data-text"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/pickup.svg.192dpi.png")),
            Act = () => StartPickup(ent, args.User, args.Target)
        };
        args.Verbs.Add(verb);
    }

    private bool InRange(EntityUid user, EntityUid target)
    {
        return _interactionSystem.InRangeAndAccessible(user, target) &&
               _actionBlockerSystem.CanInteract(user, target);
    }

    private void StartPickup(Entity<NestingMobComponent> ent, EntityUid user, EntityUid target)
    {
        if (_containerSystem.TryGetContainingContainer((user, null, null), out _)) return;
        if (!InRange(user, target)) return;

        var doAfterTime = !_mobState.IsAlive(ent)
            ? TimeSpan.FromSeconds(1.0)
            : ent.Comp.DoAfter;

        var doAfterEvent = new DoAfterArgs(EntityManager, user, doAfterTime,
            new NestingMobPickupDoAfterEvent(), ent)
        {
            AttemptFrequency = AttemptFrequency.EveryTick,
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true
        };

        if (_doAfter.TryStartDoAfter(doAfterEvent))
        {
            _popup.PopupEntity(Loc.GetString("restrict-nesting-item-pickup-start", ("user", user)), target, target);
        }
    }

    private void DuringPickup(Entity<NestingMobComponent> ent, ref DoAfterAttemptEvent<NestingMobPickupDoAfterEvent> ev)
    {
        if (_containerSystem.TryGetContainingContainer((ev.Event.Args.User, null, null), out _) ||
            !InRange(ev.Event.Args.User, ent))
        {
            ev.Cancel();
        }
    }

    private void FinishPickup(Entity<NestingMobComponent> ent, ref NestingMobPickupDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled) return;
        if (_containerSystem.TryGetContainingContainer((args.User, null, null), out _)) return;
        if (!InRange(args.User, ent)) return;

        _handsSystem.TryPickup(args.User, ent, animateUser: false);
    }

    private void OnInteractAttempt(Entity<NestingMobComponent> ent, ref InteractionAttemptEvent args)
    {
        if (ent.Comp.InContainer && !HasComp<NestingContainerComponent>(args.Target))
            args.Cancelled = true;
    }

    private void OnAttempt(EntityUid uid, NestingMobComponent component, CancellableEntityEventArgs args)
    {
        if (component.InContainer) args.Cancel();
    }

    private void OnPullAttempt(EntityUid uid, NestingMobComponent component, PullAttemptEvent args)
    {
        if (component.InContainer) args.Cancelled = true;
    }

    private void OnHandEquippedAttempt(EntityUid uid, NestingMobComponent component, ContainerIsInsertingAttemptEvent args)
    {
        if (HasComp<NestingMobComponent>(args.EntityUid)) args.Cancel();
    }

    private void OnBeingEquippedAttempt(Entity<NestingMobComponent> ent, ref BeingEquippedAttemptEvent args)
    {
        if (HasComp<NestingMobComponent>(args.EquipTarget)) args.Cancel();
    }

    private void OnPickupAttempt(EntityUid uid, NestingMobComponent component, PickupAttemptEvent args)
    {
        if (HasComp<NestingMobComponent>(args.Item) || component.InContainer)
            args.Cancel();
    }

    private void OnGettingPickupAttempt(EntityUid uid, NestingMobComponent component, ref GettingPickedUpAttemptEvent args)
    {
        args.Cancel();
    }

    private void AddInsertAltVerb(EntityUid uid, NestingContainerComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess) return;
        if (!TryComp<NestingMobComponent>(args.User, out _)) return;

        args.Verbs.Add(new AlternativeVerb
        {
            Act = () => StartNestingInsertDoAfter(args.User, uid),
            Text = Loc.GetString("disposal-self-insert-verb-get-data-text"),
            Priority = 2
        });
    }

    private void StartNestingInsertDoAfter(EntityUid user, EntityUid container)
    {
        var args = new DoAfterArgs(EntityManager, user, 1f,
            new NestingMobInsertDoAfterEvent(), container, target: container)
        {
            BreakOnMove = true,
            NeedHand = true,
            MovementThreshold = 0.5f
        };
        _doAfter.TryStartDoAfter(args);
    }

    private void OnInsertingDoAfter(Entity<NestingContainerComponent> ent, ref NestingMobInsertDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Target == null) return;
        if (!TryComp<NestingMobComponent>(args.Args.User, out var nestingComp)) return;
        if (!_containerSystem.TryGetContainer(ent, BaseStorageId, out var storageContainer)) return;

        if (_containerSystem.Insert(args.Args.User, storageContainer))
        {
            nestingComp.InContainer = true;
        }
        else
        {
            _popup.PopupClient(Loc.GetString("unsuccessfully-insert"), args.Args.User, args.Args.User);
        }
        args.Handled = true;
    }
}

[Serializable, NetSerializable]
public sealed partial class NestingMobPickupDoAfterEvent : SimpleDoAfterEvent {}

[Serializable, NetSerializable]
public sealed partial class NestingMobInsertDoAfterEvent : SimpleDoAfterEvent {}

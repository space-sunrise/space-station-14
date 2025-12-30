using Content.Shared._Sunrise.Nesting;
using Content.Shared.DoAfter;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Content.Shared.Verbs;
using Content.Shared._Sunrise.Carrying;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Sunrise.Nesting;

public abstract class SharedNestingSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    public const string BaseStorageId = "storagebase";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NestingMobComponent, GetVerbsEvent<AlternativeVerb>>(AddNestingPickupAltVerb);
        SubscribeLocalEvent<NestingMobComponent, PickupAttemptEvent>(OnPickupAttempt);
        SubscribeLocalEvent<NestingMobComponent, GettingPickedUpAttemptEvent>(OnGettingPickedUpAttempt);
        SubscribeLocalEvent<NestingMobComponent, BeingEquippedAttemptEvent>(OnBeingEquippedAttempt);
        SubscribeLocalEvent<NestingMobComponent, ContainerIsInsertingAttemptEvent>(OnHandEquippedAttempt);
        SubscribeLocalEvent<NestingMobComponent, UseAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<NestingMobComponent, ThrowAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<NestingMobComponent, InteractionAttemptEvent>(OnInteractAttempt);
        SubscribeLocalEvent<NestingMobComponent, PullAttemptEvent>(OnPullAttempt);
        SubscribeLocalEvent<NestingMobComponent, AttackAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<NestingMobComponent, NestingPickupDoAfterEvent>(OnPickupDoAfter);
        SubscribeLocalEvent<NestingContainerComponent, GetVerbsEvent<AlternativeVerb>>(AddInsertAltVerb);
        SubscribeLocalEvent<NestingContainerComponent, NestingInsertDoAfter>(OnInsertingDoAfter);
        SubscribeLocalEvent<CarriableComponent, CanCarryEvent>(OnCanCarry);
    }

    private void OnInteractAttempt(Entity<NestingMobComponent> ent, ref InteractionAttemptEvent args)
    {
        if (ent.Comp.InContainer && !HasComp<NestingContainerComponent>(args.Target))
            args.Cancelled = true;
    }

    private void OnAttempt(EntityUid uid, NestingMobComponent component, CancellableEntityEventArgs args)
    {
        if (component.InContainer)
            args.Cancel();
    }

    private void OnPullAttempt(EntityUid uid, NestingMobComponent component, PullAttemptEvent args)
    {
        if (component.InContainer)
            args.Cancelled = true;
    }

    private void OnHandEquippedAttempt(EntityUid uid, NestingMobComponent component, ContainerIsInsertingAttemptEvent args)
    {
        if (!HasComp<NestingMobComponent>(args.EntityUid))
            return;

        args.Cancel();
    }

    private void OnBeingEquippedAttempt(Entity<NestingMobComponent> ent, ref BeingEquippedAttemptEvent args)
    {
        if (!HasComp<NestingMobComponent>(args.EquipTarget))
            return;

        args.Cancel();
    }

    private void OnPickupAttempt(EntityUid uid, NestingMobComponent component, PickupAttemptEvent args)
    {
        if (HasComp<NestingMobComponent>(args.Item) || component.InContainer)
            args.Cancel();
    }

    private void OnGettingPickedUpAttempt(EntityUid uid, NestingMobComponent component, GettingPickedUpAttemptEvent args)
    {
        args.Cancel();
    }

    private void AddInsertAltVerb(EntityUid uid, NestingContainerComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (!TryComp<NestingMobComponent>(args.User, out var nestingComponent))
            return;

        AlternativeVerb verb = new()
        {
            Act = () => StartNestingInsertingDoAfter(args.User, uid),
            Text = Loc.GetString("disposal-self-insert-verb-get-data-text"),
            Priority = 2
        };
        args.Verbs.Add(verb);
    }

    private void StartNestingInsertingDoAfter(EntityUid nestingEntity, EntityUid container)
    {
        var ev = new NestingInsertDoAfter();
        var args = new DoAfterArgs(EntityManager, nestingEntity, 1f, ev, container, target: container)
        {
            BreakOnMove = true,
            NeedHand = true,
            MovementThreshold = 0.5f
        };

        _doAfterSystem.TryStartDoAfter(args);
    }

    private void OnInsertingDoAfter(Entity<NestingContainerComponent> ent, ref NestingInsertDoAfter args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (args.Args.Target == null)
            return;

        if (!TryComp<NestingMobComponent>(args.Args.User, out var nestingComponent))
            return;

        if (!_container.TryGetContainer(ent, BaseStorageId, out var storageContainer))
            return;

        if (_container.Insert(args.User, storageContainer))
            nestingComponent.InContainer = true;
        else
            _popup.PopupClient(Loc.GetString("unsuccessfully-insert"), args.User, args.User);

        args.Handled = true;
    }

    private void AddNestingPickupAltVerb(EntityUid uid, NestingMobComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (component.InContainer)
            return;

        if (args.User == args.Target)
            return;

        AlternativeVerb verb = new()
        {
            Act = () =>
            {
                StartNestingPickupDoAfter(args.User, uid);
            },
            Text = Loc.GetString("pick-up-verb-get-data-text"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/pickup.svg.192dpi.png")),
            Priority = 2
        };
        args.Verbs.Add(verb);
    }

    private void StartNestingPickupDoAfter(EntityUid user, EntityUid item)
    {
        if (!TryComp<NestingMobComponent>(item, out var nestingComponent))
            return;

        var length = nestingComponent.DefaultDoAfterLength;

        if (!_mobState.IsAlive(item))
            length = nestingComponent.DeadDoAfterLength;

        var ev = new NestingPickupDoAfterEvent();
        var args = new DoAfterArgs(EntityManager, user, length, ev, item, target: item)
        {
            BreakOnMove = true,
            NeedHand = true,
            MovementThreshold = 0.5f
        };

        _doAfterSystem.TryStartDoAfter(args);
    }

    private void OnPickupDoAfter(Entity<NestingMobComponent> ent, ref NestingPickupDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (args.Args.Target == null)
            return;

        if (!TryComp<HandsComponent>(args.Args.User, out var hands))
            return;

        if (!_hands.TryGetEmptyHand(args.Args.User, out var emptyHand))
            return;

        if (TryComp<MultiHandedItemComponent>(args.Args.Target.Value, out var multiHanded)
            && _hands.CountFreeHands(args.Args.User) < multiHanded!.HandsNeeded)
        {
            _popup.PopupPredictedCursor(Loc.GetString("multi-handed-item-pick-up-fail",
                ("number", multiHanded.HandsNeeded - 1), ("item", ent.Owner)), args.Args.User);
            return;
        }

        _hands.TryPickup(args.Args.User, args.Args.Target.Value, emptyHand, checkActionBlocker: false, handsComp: hands);
        args.Handled = true;
    }

    private void OnCanCarry(EntityUid uid, CarriableComponent component, CanCarryEvent args)
    {
        if (!HasComp<NestingMobComponent>(args.Carrier))
            args.Cancel();
    }
}

[Serializable, NetSerializable]
public sealed partial class NestingPickupDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class NestingInsertDoAfter : SimpleDoAfterEvent
{
}

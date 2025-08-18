using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.UserInterface;
using Robust.Shared.Utility;

namespace Content.Shared._Sunrise.InnateItem;

public sealed class InnateItemSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InnateItemComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<InnateItemComponent, InnateEntityTargetActionEvent>(WorldTargetActionActivate);
        SubscribeLocalEvent<InnateItemComponent, InnateInstantActionEvent>(InstantActionActivate);
        SubscribeLocalEvent<InnateItemComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnShutdown(EntityUid uid, InnateItemComponent component, ComponentShutdown args)
    {
        foreach (var action in component.Actions)
        {
            _actionsSystem.RemoveAction(action);
        }
    }

    private void OnMapInit(EntityUid uid, InnateItemComponent component, MapInitEvent args)
    {
        AddItems(uid, component);
    }

    private void AddItems(EntityUid uid, InnateItemComponent component)
    {
        foreach (var itemProto in component.WorldTargetActions)
        {
            var item = Spawn(itemProto);
            var action = CreateWorldTargetAction(item);
            _actionContainer.AddAction(uid, action);
            _actionsSystem.AddAction(uid, action, uid);
            component.Actions.Add(action);
        }

        foreach (var itemProto in component.InstantActions)
        {
            var item = Spawn(itemProto);
            var action = CreateInstantAction(item);
            _actionContainer.AddAction(uid, action);
            _actionsSystem.AddAction(uid, action, uid);
            component.Actions.Add(action);
        }
    }

    private EntityUid CreateWorldTargetAction(EntityUid uid)
    {
        if (!TryComp<ActionComponent>(uid, out var action))
            return EntityUid.Invalid;

        EnsureComp<WorldTargetActionComponent>(uid);
        EnsureComp<EntityTargetActionComponent>(uid);
        var targetAction = EnsureComp<TargetActionComponent>(uid);
        _actionsSystem.SetIcon(uid, new SpriteSpecifier.EntityPrototype(MetaData(uid).EntityPrototype!.ID));
        _actionsSystem.SetEvent(uid, new InnateEntityTargetActionEvent(uid));
        _actionsSystem.SetPriority((uid, action), 0);
        _actionsSystem.SetItemIconStyle((uid, action), ItemActionIconStyle.NoItem);
        _actionsSystem.SetCheckCanInteract((uid, action), false);
        _actionsSystem.SetIgnoreContainer((uid, targetAction), true);
        if (TryComp<ActivatableUIComponent>(uid, out var activatableUIComponent))
        {
            activatableUIComponent.RequiresComplex = false;
            activatableUIComponent.InHandsOnly = false;
            activatableUIComponent.RequireActiveHand = false;
            Dirty(uid, activatableUIComponent);
        }
        return uid;
    }

    private EntityUid CreateInstantAction(EntityUid uid)
    {
        if (!TryComp<ActionComponent>(uid, out var action))
            return EntityUid.Invalid;

        EnsureComp<InstantActionComponent>(uid);
        _actionsSystem.SetEvent(uid, new InnateInstantActionEvent(uid));
        _actionsSystem.SetPriority((uid, action), 0);
        _actionsSystem.SetIcon(uid, new SpriteSpecifier.EntityPrototype(MetaData(uid).EntityPrototype!.ID));
        _actionsSystem.SetCheckCanInteract((uid, action), false);
        if (TryComp<ActivatableUIComponent>(uid, out var activatableUIComponent))
        {
            activatableUIComponent.RequiresComplex = false;
            activatableUIComponent.InHandsOnly = false;
            activatableUIComponent.RequireActiveHand = false;
            Dirty(uid, activatableUIComponent);
        }
        return uid;
    }

    private void WorldTargetActionActivate(EntityUid uid, InnateItemComponent component, InnateEntityTargetActionEvent args)
    {
        if (args.Entity == null)
            return;

        _interactionSystem.InteractUsing(
            args.Performer,
            args.Item,
            args.Entity.Value,
            args.Target,
            false,
            false,
            false);
    }

    private void InstantActionActivate(EntityUid uid, InnateItemComponent component, InnateInstantActionEvent args)
    {
        var ev = new UseInHandEvent(args.Performer);
        RaiseLocalEvent(args.Item, ev);
    }
}

public sealed partial class InnateEntityTargetActionEvent : WorldTargetActionEvent
{
    public EntityUid Item;

    public InnateEntityTargetActionEvent(EntityUid item)
    {
        Item = item;
    }
}

public sealed partial class InnateInstantActionEvent : InstantActionEvent
{
    public EntityUid Item;

    public InnateInstantActionEvent(EntityUid item)
    {
        Item = item;
    }
}

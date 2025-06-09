using Content.Server.Actions;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Actions.Events;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.UserInterface;
using Robust.Shared.Utility;

namespace Content.Server._Sunrise.InnateItem
{
    public sealed class InnateItemSystem : EntitySystem
    {
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
        [Dependency] private readonly ActionsSystem _actions = default!;
        [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
        [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<InnateItemComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<InnateItemComponent, InnateWorldTargetActionEvent>(WorldTargetActionActivate);
            SubscribeLocalEvent<InnateItemComponent, InnateInstantActionEvent>(InstantActionActivate);
            SubscribeLocalEvent<InnateItemComponent, ComponentShutdown>(OnShutdown);
        }

        private void OnShutdown(EntityUid uid, InnateItemComponent component, ComponentShutdown args)
        {
            foreach (var action in component.Actions)
            {
                _actionContainer.RemoveAction(action);
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
            var targetAction = EnsureComp<TargetActionComponent>(uid);
            var action = EnsureComp<ActionComponent>(uid);
            _actions.SetIcon(uid, new SpriteSpecifier.EntityPrototype(MetaData(uid).EntityPrototype!.ID));
            _actions.SetEvent(uid, new InnateWorldTargetActionEvent(uid));
            _actions.SetCheckCanInteract((uid, action), false);
            _actions.SetCheckCanAccess((uid, targetAction), false);
            _actions.SetIgnoreContainer((uid, targetAction), false);
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
            var action = EnsureComp<ActionComponent>(uid);
            _actions.SetEvent(uid, new InnateInstantActionEvent(uid));
            _actions.SetIcon(uid, new SpriteSpecifier.EntityPrototype(MetaData(uid).EntityPrototype!.ID));
            _actions.SetCheckCanInteract((uid, action), false);
            if (TryComp<ActivatableUIComponent>(uid, out var activatableUIComponent))
            {
                activatableUIComponent.RequiresComplex = false;
                activatableUIComponent.InHandsOnly = false;
                activatableUIComponent.RequireActiveHand = false;
                Dirty(uid, activatableUIComponent);
            }
            return uid;
        }

        private void WorldTargetActionActivate(EntityUid uid, InnateItemComponent component, InnateWorldTargetActionEvent args)
        {
            _interactionSystem.InteractUsing(
                args.Performer,
                args.Item,
                args.Target,
                Transform(args.Target).Coordinates,
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

    public sealed partial class InnateWorldTargetActionEvent : EntityTargetActionEvent
    {
        public EntityUid Item;

        public InnateWorldTargetActionEvent(EntityUid item)
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
}

using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Shared._Sunrise.Actions;

public sealed class WorldTargetActionKeybindSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.UseWorldTargetAction, new PointerInputCmdHandler(HandleUseWorldTargetAction))
            .Register<WorldTargetActionKeybindSystem>();
    }

    public override void Shutdown()
    {
        CommandBinds.Unregister<WorldTargetActionKeybindSystem>();
        base.Shutdown();
    }

    private bool HandleUseWorldTargetAction(ICommonSession? session, EntityCoordinates coordinates, EntityUid target)
    {
        if (_net.IsClient)
            return false;

        if (session?.AttachedEntity is not { Valid: true } user || !Exists(user))
            return false;

        if (!TryComp<WorldTargetActionKeybindComponent>(user, out var component))
            return false;

        TryUseWorldTargetAction((user, component), coordinates, target);
        return false;
    }

    public bool TryUseWorldTargetAction(
        Entity<WorldTargetActionKeybindComponent?> ent,
        EntityCoordinates coordinates,
        EntityUid target)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (!TryGetAction(ent, coordinates, out var actionEnt, out var action, out var actionEvent))
            return false;

        actionEvent.Target = coordinates;
        actionEvent.Entity = HasComp<EntityTargetActionComponent>(actionEnt) && target.IsValid() && Exists(target)
            ? target
            : null;

        _actions.PerformAction(ent.Owner, (actionEnt, action), actionEvent, predicted: false);
        return true;
    }

    private bool TryGetAction(
        Entity<WorldTargetActionKeybindComponent?> ent,
        EntityCoordinates coordinates,
        out EntityUid actionEnt,
        out ActionComponent action,
        out WorldTargetActionEvent actionEvent)
    {
        actionEnt = default;
        action = default!;
        actionEvent = default!;

        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (!TryComp<ActionsComponent>(ent.Owner, out var actions))
            return false;

        foreach (var candidate in actions.Actions)
        {
            var prototype = Prototype(candidate);
            if (prototype == null || prototype.ID != ent.Comp.Action.Id)
                continue;

            if (!TryComp<ActionComponent>(candidate, out var actionComp))
                return false;

            if (!TryComp<WorldTargetActionComponent>(candidate, out var worldTarget))
                return false;

            if (!_actions.ValidAction((candidate, actionComp)))
                return false;

            if (!_actions.ValidateWorldTarget(ent.Owner, coordinates, (candidate, worldTarget)))
                return false;

            if (_actions.GetEvent(candidate) is not WorldTargetActionEvent worldEvent)
                return false;

            actionEnt = candidate;
            action = actionComp;
            actionEvent = worldEvent;
            return true;
        }

        return false;
    }
}

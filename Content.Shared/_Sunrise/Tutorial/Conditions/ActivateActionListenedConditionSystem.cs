using Content.Shared._Sunrise.Tutorial.Components;
using Content.Shared._Sunrise.Tutorial.Components.Trackers;
using Content.Shared.Actions;
using Content.Shared.Actions.Events;
using Content.Shared.Hands;
using Robust.Shared.Timing;

namespace Content.Shared._Sunrise.Tutorial.Conditions;

/// <summary>
/// Records activated actions
/// </summary>
public sealed partial class ActivateActionListenedConditionSystem : EventListenedConditionSystemBase<ActivateActionListenedCondition>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TutorialObservableComponent, ActionAttemptEvent>(OnAttemptAction);
    }

    private void OnAttemptAction(Entity<TutorialObservableComponent> ent, ref ActionAttemptEvent args)
    {
        if (!ent.Comp.Observers.Contains(args.User))
            return;

        RecordEvent(args.User, DefaultKey, ent);
    }
}

/// <summary>
/// Checks if the player has activated target action entity.
/// </summary>
public sealed partial class ActivateActionListenedCondition : EventListenedConditionBase<ActivateActionListenedCondition>
{
}

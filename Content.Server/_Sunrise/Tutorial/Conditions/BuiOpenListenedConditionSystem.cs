using Content.Shared._Sunrise.Tutorial.Components;
using Content.Shared._Sunrise.Tutorial.Conditions;

namespace Content.Server._Sunrise.Tutorial.Conditions;

/// <summary>
/// Records bound UI openings on observed tutorial entities.
/// </summary>
public sealed partial class BuiOpenListenedConditionSystem
    : EventListenedConditionSystemBase<BuiOpenListenedCondition>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TutorialObservableComponent, BoundUIOpenedEvent>(OnBoundUIOpened);
    }

    private void OnBoundUIOpened(Entity<TutorialObservableComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (!ent.Comp.Observers.Contains(args.Actor))
            return;

        RecordEvent(args.Actor, DefaultKey, ent);
    }
}

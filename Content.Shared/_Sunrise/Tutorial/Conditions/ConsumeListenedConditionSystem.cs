using Content.Shared._Sunrise.Tutorial.Components;
using Content.Shared.Nutrition;

namespace Content.Shared._Sunrise.Tutorial.Conditions;

/// <summary>
/// Records food and drink consumption events for tutorial conditions.
/// </summary>
public sealed partial class ConsumeListenedConditionSystem : EventListenedConditionSystemBase<ConsumeListenedCondition>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TutorialObservableComponent, IngestedEvent>(OnIngested);
    }

    private void OnIngested(Entity<TutorialObservableComponent> ent, ref IngestedEvent args)
    {
        // Target is the entity doing the eating; User may differ in force-feed scenarios.
        if (!ent.Comp.Observers.Contains(args.Target))
            return;

        RecordEvent(args.Target, DefaultKey, ent);
    }
}

/// <summary>
/// Checks if the player has consumed a food or drink item (any item, or a specific prototype).
/// </summary>
public sealed partial class ConsumeListenedCondition : EventListenedConditionBase<ConsumeListenedCondition>
{
    public override bool ObserveAnyWithoutTarget => true;
}

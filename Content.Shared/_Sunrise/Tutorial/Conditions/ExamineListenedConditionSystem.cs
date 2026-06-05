using System.Collections.Generic;
using Content.Shared._Sunrise.Tutorial.Components;
using Content.Shared.Examine;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Tutorial.Conditions;

/// <summary>
/// Records examine events performed by tutorial players on observed entities.
/// </summary>
public sealed partial class ExamineListenedConditionSystem : EventListenedConditionSystemBase<ExamineListenedCondition>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TutorialObservableComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<TutorialObservableComponent> ent, ref ExaminedEvent args)
    {
        if (!ent.Comp.Observers.Contains(args.Examiner))
            return;

        RecordEvent(args.Examiner, DefaultKey, args.Examined);
    }
}

/// <summary>
/// Checks if the player has examined a target entity.
/// </summary>
public sealed partial class ExamineListenedCondition : EventListenedConditionBase<ExamineListenedCondition>
{
    public override bool ObserveAnyWithoutTarget => true;
}


using Content.Shared._Sunrise.Tutorial.Components;
using Content.Shared.Doors;

namespace Content.Shared._Sunrise.Tutorial.Conditions;

/// <summary>
/// Records successful door opening attempts made by observed tutorial players.
/// </summary>
public sealed partial class DoorOpenListenedConditionSystem : EventListenedConditionSystemBase<DoorOpenListenedCondition>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TutorialObservableComponent, BeforeDoorOpenedEvent>(OnBeforeDoorOpened);
    }

    private void OnBeforeDoorOpened(Entity<TutorialObservableComponent> ent, ref BeforeDoorOpenedEvent args)
    {
        if (args.User is null || !ent.Comp.Observers.Contains(args.User.Value))
            return;

        RecordEvent(args.User.Value, DefaultKey, ent);
    }
}

/// <summary>
/// Checks if the player has opened a door/airlock (any door, or a specific prototype).
/// </summary>
public sealed partial class DoorOpenListenedCondition : EventListenedConditionBase<DoorOpenListenedCondition>
{
    public override bool ObserveAnyWithoutTarget => true;
}

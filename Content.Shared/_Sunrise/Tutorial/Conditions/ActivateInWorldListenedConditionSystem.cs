using Content.Shared._Sunrise.Tutorial.Components;
using Content.Shared.Interaction;

namespace Content.Shared._Sunrise.Tutorial.Conditions;

/// <summary>
/// Records activation events performed by a tutorial player on observed world entities.
/// </summary>
public sealed partial class ActivateInWorldListenedConditionSystem : EventListenedConditionSystemBase<ActivateInWorldListenedCondition>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TutorialObservableComponent, ActivateInWorldEvent>(OnActivateInWorld);
    }

    private void OnActivateInWorld(Entity<TutorialObservableComponent> ent, ref ActivateInWorldEvent args)
    {
        if (!ent.Comp.Observers.Contains(args.User))
            return;

        RecordEvent(args.User, DefaultKey, ent);
    }
}

/// <summary>
/// Checks if the player has activated a target entity in the world.
/// </summary>
public sealed partial class ActivateInWorldListenedCondition : EventListenedConditionBase<ActivateInWorldListenedCondition>
{
}

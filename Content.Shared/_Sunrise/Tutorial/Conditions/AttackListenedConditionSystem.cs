using Content.Shared._Sunrise.Tutorial.Components;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared._Sunrise.Tutorial.Conditions;

/// <summary>
/// Records melee attacks performed by a tutorial player against observed entities.
/// </summary>
public sealed partial class AttackListenedConditionSystem : EventListenedConditionSystemBase<AttackListenedCondition>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TutorialObservableComponent, AttackedEvent>(OnMeleeHit);
    }

    private void OnMeleeHit(Entity<TutorialObservableComponent> ent, ref AttackedEvent args)
    {
        if (!ent.Comp.Observers.Contains(args.User))
            return;

        RecordEvent(args.User, DefaultKey, ent, args.Used);
    }
}

/// <summary>
/// Checks if the player has attacked a target entity.
/// </summary>
public sealed partial class AttackListenedCondition : EventListenedConditionBase<AttackListenedCondition>
{
    public override bool ObserveAnyWithoutTarget => true;
}

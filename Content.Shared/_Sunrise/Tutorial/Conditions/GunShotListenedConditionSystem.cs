using Content.Shared._Sunrise.Tutorial.Components;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Shared._Sunrise.Tutorial.Conditions;

/// <summary>
/// Records gun firing events from observed weapons for tutorial conditions.
/// </summary>
public sealed partial class GunShotListenedConditionSystem : EventListenedConditionSystemBase<GunShotListenedCondition>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TutorialObservableComponent, AmmoShotEvent>(OnAmmoShot);
    }

    private void OnAmmoShot(Entity<TutorialObservableComponent> ent, ref AmmoShotEvent args)
    {
        if (args.Shooter is null || !ent.Comp.Observers.Contains(args.Shooter.Value))
            return;

        RecordEvent(args.Shooter.Value, DefaultKey, ent);
    }
}

/// <summary>
/// Checks if the player has fired a gun (any gun, or a specific prototype).
/// </summary>
public sealed partial class GunShotListenedCondition : EventListenedConditionBase<GunShotListenedCondition>
{
    public override bool ObserveAnyWithoutTarget => true;
}

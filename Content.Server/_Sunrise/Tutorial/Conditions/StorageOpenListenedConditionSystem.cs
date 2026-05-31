using Content.Shared._Sunrise.Tutorial.Components;
using Content.Shared._Sunrise.Tutorial.Conditions;
using Content.Shared.Storage.Components;

namespace Content.Server._Sunrise.Tutorial.Conditions;

/// <summary>
/// Records successful physical storage openings on observed tutorial entities.
/// </summary>
public sealed partial class StorageOpenListenedConditionSystem
    : EventListenedConditionSystemBase<StorageOpenListenedCondition>
{
    public override void Initialize()
    {
        base.Initialize();
        // Lockers, crates, etc. — open physically (EntityStorageComponent)
        SubscribeLocalEvent<TutorialObservableComponent, StorageOpenAttemptEvent>(OnEntityStorageOpen);
    }

    private void OnEntityStorageOpen(Entity<TutorialObservableComponent> ent, ref StorageOpenAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!ent.Comp.Observers.Contains(args.User))
            return;

        RecordEvent(args.User, DefaultKey, ent);
    }
}

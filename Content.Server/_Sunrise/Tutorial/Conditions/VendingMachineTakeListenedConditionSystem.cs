using Content.Shared._Sunrise.Tutorial.Components;
using Content.Shared._Sunrise.Tutorial.Components.Trackers;
using Content.Shared._Sunrise.Tutorial.Conditions;
using Content.Shared.VendingMachines;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.Tutorial.Conditions;

/// <summary>
/// Records vending machine eject actions and supports matching the dispensed item prototype.
/// </summary>
public sealed partial class VendingMachineTakeListenedConditionSystem
    : EventListenedConditionSystemBase<VendingMachineTakeListenedCondition>
{
    public override void Initialize()
    {
        base.Initialize();
        Subs.BuiEvents<TutorialObservableComponent>(VendingMachineUiKey.Key, subs =>
        {
            subs.Event<VendingMachineEjectMessage>(OnVendingEject);
        });
    }

    protected override void Condition(Entity<TutorialPlayerComponent> entity, ref TutorialConditionEvent<VendingMachineTakeListenedCondition> args)
    {
        if (args.Condition.Count <= 0)
        {
            args.Result = true;
            return;
        }

        if (!TryComp<TutorialTrackerComponent>(entity, out var tracker))
            return;

        // ItemTarget takes priority: check the specific dispensed item prototype.
        // If unset, fall back to Target (vending machine proto) or AnyTarget.
        var target = (args.Condition.ItemTarget ?? args.Condition.Target) ?? AnyTarget;
        args.Result = HasCount(tracker.Counters, args.Condition.CounterKey, target, args.Condition.Count);
    }

    private void OnVendingEject(Entity<TutorialObservableComponent> ent, ref VendingMachineEjectMessage args)
    {
        if (args.Actor is not { Valid: true } actor)
            return;

        if (!ent.Comp.Observers.Contains(actor))
            return;

        // Record against vending machine proto (for Target matching) + AnyTarget
        RecordEvent(actor, DefaultKey, ent.Owner);

        // Also record against the dispensed item proto (for ItemTarget matching).
        // RecordEvent above already ensured TutorialTrackerComponent exists.
        if (!TryComp<TutorialTrackerComponent>(actor, out var tracker))
            return;

        var key = (DefaultKey, new EntProtoId(args.ID));
        tracker.Counters.TryGetValue(key, out var count);
        tracker.Counters[key] = count + 1;
        Dirty<TutorialTrackerComponent>((actor, tracker));
    }
}

using Content.Shared.ResearchRig.Components;
using Content.Shared.Anomaly.Components;
using Content.Shared.ItemSlots;
using Robust.Shared.Timing;

namespace Content.Shared.ResearchRig.Systems;

/// <summary>
/// Handles anomaly core battery functionality for research RIG suits
/// </summary>
public abstract class SharedAnomalyCoreBatterySystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] protected readonly ItemSlotsSystem ItemSlots = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<AnomalyCoreBatteryComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<AnomalyCoreBatteryComponent, EntInsertedIntoContainerMessage>(OnCoreInserted);
        SubscribeLocalEvent<AnomalyCoreBatteryComponent, EntRemovedFromContainerMessage>(OnCoreRemoved);
    }

    private void OnStartup(EntityUid uid, AnomalyCoreBatteryComponent component, ComponentStartup args)
    {
        // Initialize next drain time
        component.NextDrainTime = Timing.CurTime + TimeSpan.FromSeconds(component.DrainInterval);
    }

    private void OnCoreInserted(EntityUid uid, AnomalyCoreBatteryComponent component, EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != component.CoreSlotId)
            return;

        if (!HasComponent<AnomalyCoreComponent>(args.Entity))
            return;

        component.IsActive = true;
        component.NextDrainTime = Timing.CurTime + TimeSpan.FromSeconds(component.DrainInterval);
        Dirty(uid, component);
    }

    private void OnCoreRemoved(EntityUid uid, AnomalyCoreBatteryComponent component, EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != component.CoreSlotId)
            return;

        component.IsActive = false;
        Dirty(uid, component);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<AnomalyCoreBatteryComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (!component.IsActive)
                continue;

            if (Timing.CurTime < component.NextDrainTime)
                continue;

            DrainCoreBattery(uid, component);
        }
    }

    protected virtual void DrainCoreBattery(EntityUid uid, AnomalyCoreBatteryComponent component)
    {
        // Get the anomaly core from the slot
        if (!ItemSlots.TryGetSlot(uid, component.CoreSlotId, out var slot))
            return;

        if (slot.Item == null || !TryComp<AnomalyCoreComponent>(slot.Item, out var coreComp))
            return;

        // Drain charge
        coreComp.Charge = Math.Max(0, coreComp.Charge - component.ChargeTodrain);
        Dirty(slot.Item.Value, coreComp);

        // If core is fully drained, it becomes inactive
        if (coreComp.Charge <= 0)
        {
            component.IsActive = false;
            // Optionally eject the core or give a warning
        }
        else
        {
            // Schedule next drain
            component.NextDrainTime = Timing.CurTime + TimeSpan.FromSeconds(component.DrainInterval);
        }

        Dirty(uid, component);
    }
}

using Content.Shared.Lock;

namespace Content.Shared.Containers.ItemSlots;

public sealed partial class ItemSlotsSystem
{
    private void InitializeLock()
    {
        SubscribeLocalEvent<ItemSlotsLockComponent, MapInitEvent>(OnLockMapInit);
        SubscribeLocalEvent<ItemSlotsLockComponent, LockToggledEvent>(OnLockToggled);
    }

    private void OnLockMapInit(Entity<ItemSlotsLockComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp(ent, out LockComponent? lockComp))
            return;

        UpdateLocks(ent, lockComp.Locked);
    }

    private void OnLockToggled(Entity<ItemSlotsLockComponent> ent, ref LockToggledEvent args)
    {
        UpdateLocks(ent, args.Locked);
    }

    private void UpdateLocks(Entity<ItemSlotsLockComponent> ent, bool value)
    {
        foreach (var slot in ent.Comp.Slots)
        {
            if (!TryGetSlot(ent, slot, out var itemSlot))
                continue;

            SetLock(ent, itemSlot, value);
        }
    }
}

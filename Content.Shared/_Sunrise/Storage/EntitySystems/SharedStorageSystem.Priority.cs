using Content.Shared._Sunrise.Inventory.Components;
using Content.Shared._Sunrise.Inventory.Events;
using Robust.Shared.GameObjects;

namespace Content.Shared.Storage.EntitySystems;

public abstract partial class SharedStorageSystem
{
    partial void InitializePriority()
    {
        SubscribeAllEvent<StorageToggleItemPriorityEvent>(OnToggleItemPriority);
        SubscribeLocalEvent<EntityTerminatingEvent>(OnEntityDeleted);
    }

    private void OnToggleItemPriority(StorageToggleItemPriorityEvent msg, EntitySessionEventArgs args)
    {
        if (!ValidateInput(args, msg.Storage, msg.Item, out var player, out var storage, out var item))
        {
            return;
        }

        if (!storage.Comp.Container.Contains(item.Owner))
        {
            return;
        }

        if (!storage.Comp.StoredItems.ContainsKey(item.Owner))
        {
            return;
        }

        var priorityComp = EnsureComp<PersonalStoragePriorityComponent>(player.Owner);

        if (priorityComp.Priorities.TryGetValue(storage.Owner, out var current) && current == item.Owner)
        {
            priorityComp.Priorities.Remove(storage.Owner);
        }
        else
        {
            priorityComp.Priorities[storage.Owner] = item.Owner;
        }

        Dirty(player.Owner, priorityComp);
        UpdateUI(storage!);
    }

    private void OnEntityDeleted(ref EntityTerminatingEvent ev)
    {
        var deletedUid = ev.Entity;
        var priorityQuery = AllEntityQuery<PersonalStoragePriorityComponent>();

        while (priorityQuery.MoveNext(out var playerUid, out var priorityComp))
        {
            var modified = false;

            // Delete if the deleted entity is used as a key (storage)
            if (priorityComp.Priorities.Remove(deletedUid))
            {
                modified = true;
            }

            // We delete all records where the deleted entity is used as a value (subject)
            var keysToRemove = new List<EntityUid>();

            foreach (var (storageUid, itemUid) in priorityComp.Priorities)
            {
                if (itemUid.Equals(deletedUid))
                {
                    keysToRemove.Add(storageUid);
                }
            }

            foreach (var key in keysToRemove)
            {
                priorityComp.Priorities.Remove(key);
                modified = true;
            }

            if (modified)
            {
                Dirty(playerUid, priorityComp);
            }
        }
    }
}

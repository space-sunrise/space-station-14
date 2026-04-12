#pragma warning disable IDE0130
using Content.Shared._Sunrise.Inventory.Components;
using Content.Shared._Sunrise.Inventory.Events;
using Content.Shared.Hands.Components;
using Content.Shared.Item;
using Content.Shared.Storage.Components;
using Robust.Shared.GameObjects;

namespace Content.Shared.Storage.EntitySystems;

public abstract partial class SharedStorageSystem
{
    /// <summary>
    /// Priority-related logic for shared storage handling
    /// </summary>
    private readonly List<EntityUid> _keysToRemove = new();

    /// <summary>
    /// Priority item selection logic for storage containers
    /// </summary>
    partial void InitializePriority()
    {
        SubscribeAllEvent<StorageToggleItemPriorityEvent>(OnToggleItemPriority);
        SubscribeLocalEvent<EntityTerminatingEvent>(OnEntityDeleted);
    }

    private void OnToggleItemPriority(StorageToggleItemPriorityEvent msg, EntitySessionEventArgs args)
    {
        TryToggleItemPriority(msg, args);
    }

    private bool TryToggleItemPriority(StorageToggleItemPriorityEvent msg, EntitySessionEventArgs args)
    {
        if (!CanToggleItemPriority(msg, args, out var player, out var storage, out var item))
            return false;

        DoToggleItemPriority(player.Owner, storage.Owner, item.Owner);
        UpdateUI(storage.AsNullable());
        return true;
    }

    private bool CanToggleItemPriority(
        StorageToggleItemPriorityEvent msg,
        EntitySessionEventArgs args,
        out Entity<HandsComponent> player,
        out Entity<StorageComponent> storage,
        out Entity<ItemComponent> item)
    {
        if (!ValidateInput(args, msg.Storage, msg.Item, out player, out storage, out item))
            return false;

        return storage.Comp.Container.Contains(item.Owner) &&
               storage.Comp.StoredItems.ContainsKey(item.Owner);
    }

    private void DoToggleItemPriority(EntityUid playerUid, EntityUid storageUid, EntityUid itemUid)
    {
        var priorityComp = EnsureComp<PersonalStoragePriorityComponent>(playerUid);
        if (priorityComp.Priorities.TryGetValue(storageUid, out var current) && current == itemUid)
            priorityComp.Priorities.Remove(storageUid);
        else
            priorityComp.Priorities[storageUid] = itemUid;

        Dirty(playerUid, priorityComp);
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
            _keysToRemove.Clear();

            foreach (var (storageUid, itemUid) in priorityComp.Priorities)
            {
                if (itemUid.Equals(deletedUid))
                {
                    _keysToRemove.Add(storageUid);
                }
            }

            foreach (var key in _keysToRemove)
            {
                priorityComp.Priorities.Remove(key);
                modified = true;
            }

            _keysToRemove.Clear();

            if (modified)
            {
                Dirty(playerUid, priorityComp);
            }
        }
    }
}

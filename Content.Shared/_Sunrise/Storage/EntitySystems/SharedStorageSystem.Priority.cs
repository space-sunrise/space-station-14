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
    public void InitializePriority()
    {
        SubscribeAllEvent<StorageToggleItemPriorityEvent>(OnToggleItemPriority);
        SubscribeLocalEvent<StorageComponent, EntityTerminatingEvent>(OnStorageDeleted);
        SubscribeLocalEvent<ItemComponent, EntityTerminatingEvent>(OnItemDeleted);
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

    private void OnStorageDeleted(Entity<StorageComponent> storage, ref EntityTerminatingEvent ev)
    {
        var query = AllEntityQuery<PersonalStoragePriorityComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Priorities.Remove(storage.Owner))
            {
                Dirty(uid, comp);
            }
        }
    }

    private void OnItemDeleted(Entity<ItemComponent> item, ref EntityTerminatingEvent ev)
    {
        var query = AllEntityQuery<PersonalStoragePriorityComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            var keysToRemove = new List<EntityUid>();

            foreach (var (storage, priorityItem) in comp.Priorities)
            {
                if (priorityItem == item.Owner)
                {
                    keysToRemove.Add(storage);
                }
            }

            if (keysToRemove.Count > 0)
            {
                foreach (var storage in keysToRemove)
                {
                    comp.Priorities.Remove(storage);
                }
                Dirty(uid, comp);
            }
        }
    }
}

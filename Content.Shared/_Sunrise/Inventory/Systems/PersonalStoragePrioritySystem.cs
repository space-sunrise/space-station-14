using Content.Shared._Sunrise.Inventory.Components;
using Content.Shared._Sunrise.Inventory.Events;
using Content.Shared.Storage;
using Robust.Shared.Player;

namespace Content.Shared._Sunrise.Inventory.Systems;

/// <summary>
/// Система управления логикой приоритета личного хранилища.
/// Управляет предпочтительными элементами для каждого объекта хранения при вставке.
/// </summary>
public sealed partial class PersonalStoragePrioritySystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<StorageToggleItemPriorityEvent>(OnToggleItemPriority);
    }

    private void OnToggleItemPriority(StorageToggleItemPriorityEvent msg, EntitySessionEventArgs args)
    {
        var playerSession = args.SenderSession;
        if (playerSession == null)
            return;

        if (!playerSession.AttachedEntity.HasValue)
            return;

        if (!TryGetEntity(msg.Storage, out var storageUid) || !storageUid.HasValue || !HasComp<StorageComponent>(storageUid))
            return;

        if (!TryGetEntity(msg.Item, out var itemUid) || !itemUid.HasValue)
            return;

        if (!TryComp<StorageComponent>(storageUid, out var storageComponent))
            return;

        if (!storageComponent.Container.Contains(itemUid.Value))
            return;

        if (!storageComponent.StoredItems.ContainsKey(itemUid.Value))
            return;

        var playerUid = playerSession.AttachedEntity.Value;
        var priorityComp = EnsureComp<PersonalStoragePriorityComponent>(playerUid);

        if (priorityComp.Priorities.TryGetValue(storageUid.Value, out var current) && current == itemUid.Value)
        {
            priorityComp.Priorities.Remove(storageUid.Value);
        }
        else
        {
            priorityComp.Priorities[storageUid.Value] = itemUid.Value;
        }

        Dirty(playerUid, priorityComp);
    }
}

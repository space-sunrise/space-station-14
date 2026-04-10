using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Inventory.Events;

/// <summary>
/// Вызывается, когда запрашивается переключение приоритета элемента для контейнера хранения.
/// </summary>
[Serializable, NetSerializable]
public sealed class StorageToggleItemPriorityEvent : EntityEventArgs
{
    public readonly NetEntity Item;

    public readonly NetEntity Storage;

    public StorageToggleItemPriorityEvent(NetEntity item, NetEntity storage)
    {
        Item = item;
        Storage = storage;
    }
}
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Inventory.Events;

/// <summary>
/// Raised when toggling item priority for a storage container is requested.
/// </summary>
[Serializable, NetSerializable]
public sealed class StorageToggleItemPriorityEvent(NetEntity item, NetEntity storage) : EntityEventArgs
{
    public readonly NetEntity Item = item;
    public readonly NetEntity Storage = storage;
}

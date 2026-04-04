using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.LockableEquipment;

[Serializable, NetSerializable]
public sealed class LockableEquipmentComponentState : ComponentState
{
    public bool Locked { get; }
    public string? LockId { get; }

    public LockableEquipmentComponentState(bool locked, string? lockId)
    {
        Locked = locked;
        LockId = lockId;
    }
}
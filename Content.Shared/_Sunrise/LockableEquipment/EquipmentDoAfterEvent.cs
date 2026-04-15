using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.LockableEquipment;

public enum EquipmentActionType
{
    Attach,
    Detach
}

[Serializable, NetSerializable]
public sealed partial class EquipmentDoAfterEvent : SimpleDoAfterEvent
{
    public EquipmentActionType Action { get; }

    public EquipmentDoAfterEvent(EquipmentActionType action)
    {
        Action = action;
    }
}

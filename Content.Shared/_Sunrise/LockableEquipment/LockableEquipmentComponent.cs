using Robust.Shared.GameObjects;

namespace Content.Shared._Sunrise.LockableEquipment;

[RegisterComponent]
public sealed class LockableEquipmentComponent : Component
{
    [DataField]
    public bool Locked = false;

    [DataField]
    public string? LockId;

    [DataField]
    public bool GenerateKeyOnEquip = true;

    [DataField]
    public EntProtoId? KeyPrototype;

}
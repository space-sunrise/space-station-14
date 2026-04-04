using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.LockableEquipment;

[RegisterComponent, NetworkedComponent]
public sealed partial class LockableEquipmentComponent : Component
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
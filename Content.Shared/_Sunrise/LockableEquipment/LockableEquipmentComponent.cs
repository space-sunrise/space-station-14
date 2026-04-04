using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.LocableEquipment;

[RegisterComponent, NetworkedComponent]
public sealed partial class LocatableEquipmentComponent : Component
{
    [DataField]
    public SlotFlags Slots = SlotFlags.NONE;

    [DataField]
    public bool Enabled = true;
}
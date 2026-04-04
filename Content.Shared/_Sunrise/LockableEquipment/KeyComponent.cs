using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.LockableEquipment;

[RegisterComponent, NetworkedComponent]
public sealed partial class KeyComponent : Component
{
    [DataField]
    public string? LockId;
}
using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.LockableEquipment;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class KeyComponent : Component
{
    /// <summary>
    /// Shared lock identifier paired with a lockable device.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? LockId;
}

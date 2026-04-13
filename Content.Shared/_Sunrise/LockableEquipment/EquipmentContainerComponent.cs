using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared._Sunrise.LockableEquipment;

/// <summary>
/// Stores the internal container used for installed lockable devices.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EquipmentContainerComponent : Component
{
    /// <summary>
    /// Container identifier holding the currently installed device.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public string ContainerId = "locked-equipment";

    /// <summary>
    /// Delay before attaching a device completes.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float AttachDoAfter = 1.5f;

    /// <summary>
    /// Delay before removing a device completes.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float DetachDoAfter = 1.5f;
}

using System;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.Manager.Attributes;

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
    [DataField, AutoNetworkedField]
    public string ContainerId = "locked-equipment";

    /// <summary>
    /// Delay before attaching a device completes.
    /// </summary>
    [DataField]
    public TimeSpan AttachDoAfter = TimeSpan.FromSeconds(1.5);

    /// <summary>
    /// Delay before removing a device completes.
    /// </summary>
    [DataField]
    public TimeSpan DetachDoAfter = TimeSpan.FromSeconds(1.5);
}

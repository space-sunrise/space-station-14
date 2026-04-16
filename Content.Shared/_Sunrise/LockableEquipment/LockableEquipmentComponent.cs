using System;
using Content.Shared.Stacks;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.LockableEquipment;

/// <summary>
/// Stores lock, break and visual configuration for a lockable device.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LockableEquipmentComponent : Component
{
    /// <summary>
    /// Whether the device is currently locked.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Locked { get; set; }

    /// <summary>
    /// Whether the device is currently broken and must be repaired.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Broken { get; set; }

    /// <summary>
    /// Shared identifier paired with a matching key.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? LockId;

    /// <summary>
    /// Body layer occupied by the installed device.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string Layer = "lockable_under";

    /// <summary>
    /// RSI path used for the installed device overlay.
    /// </summary>
    [DataField]
    public string? RsiPath;

    /// <summary>
    /// Forced-open behavior used when the device is broken open.
    /// </summary>
    [DataField]
    public BreakMode Mode = BreakMode.Breakable;

    /// <summary>
    /// Tag required on a tool to force the device open.
    /// </summary>
    [DataField]
    public string RequiredToolTag = "Wirecutter";

    /// <summary>
    /// Delay before a forced-open attempt completes.
    /// </summary>
    [DataField]
    public TimeSpan BreakDoAfter = TimeSpan.FromSeconds(1.5);

    /// <summary>
    /// Stack material required to repair a broken device.
    /// </summary>
    [DataField]
    public ProtoId<StackPrototype>? RepairMaterial;

    /// <summary>
    /// Amount of repair material consumed on repair.
    /// </summary>
    [DataField]
    public int RepairAmount = 1;
    
    /// <summary>
    /// Access priority used when comparing against blocking layers.
    /// </summary>
    [DataField]
    public int AccessPriority = 1;
    
    /// <summary>
    /// RSI state used for the installed device overlay.
    /// </summary>
    [DataField]
    public string SpriteState = "equipped";
    
    /// <summary>
    /// Defines what happens when the device is forced open.
    /// </summary>
    public enum BreakMode
    {
        None,
        Breakable,
        Destroyable,
        ForceOpen
    }
}

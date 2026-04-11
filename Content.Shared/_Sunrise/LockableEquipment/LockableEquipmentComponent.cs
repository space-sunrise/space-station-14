using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared.Stacks;

namespace Content.Shared._Sunrise.LockableEquipment;

/// <summary>
/// Stores lock, break and visual configuration for a lockable device.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LockableEquipmentComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool Locked { get; set; } = false;

    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public bool Broken { get; set; } = false;

    [DataField, AutoNetworkedField]
    public string? LockId;

    [DataField, AutoNetworkedField]
    public string Layer = "lockable_under";

    [DataField, AutoNetworkedField]
    public string RsiPath = "_Sunrise/Clothing/Locked/cage.rsi";

    [DataField]
    public BreakMode Mode = BreakMode.Breakable;

    [DataField]
    public string RequiredToolTag = "Wirecutter";

    [DataField]
    public float BreakDoAfter = 1.5f;

    [DataField]
    public ProtoId<StackPrototype>? RepairMaterial;

    [DataField]
    public int RepairAmount = 1;
    
    [DataField]
    public int AccessPriority = 1;
    
    [DataField, AutoNetworkedField]
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

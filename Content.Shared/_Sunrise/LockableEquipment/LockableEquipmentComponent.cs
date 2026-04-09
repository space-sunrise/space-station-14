using System.Security.Cryptography.X509Certificates;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.LockableEquipment;

[RegisterComponent, NetworkedComponent]
public sealed partial class LockableEquipmentComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public bool Locked { get; set; } = false;

    [DataField]
    public string? LockId;

    [DataField]
    public EntProtoId? KeyPrototype;

    [DataField("layer")]
    public string Layer = "lockable_under"; // Use standard layer names

    [DataField("rsiPath")]
    public string rsiPath = "_Sunrise/Clothing/Locked/cage.rsi";

    [DataField]
    public string? RequiredFreeSlot;

    [DataField]
    public BreakMode Mode = BreakMode.Breakable;

    [DataField]
    public string RequiredToolTag = "Wirecutter";
    
    [DataField("accessPriority")]
    public int AccessPriority = 1;
    
    [DataField("spriteState")]
    public string SpriteState = "equipped"; // Allow configurable sprite state
    
    public enum BreakMode
    {
        None,               // Can't be broken, prayed and etc.
        Breakable,          // Breaks and can be fixed.
        Destroyable,        // Destroyed on force.
        ForceOpen           // Opens without break.
    }
}

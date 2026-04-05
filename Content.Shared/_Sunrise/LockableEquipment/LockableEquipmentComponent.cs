using System.Security.Cryptography.X509Certificates;
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
    public EntProtoId? KeyPrototype;

    [DataField]
    public string OverlayLayer = "belt";

    [DataField]
    public string OverlaySprite = "_Sunrise/Clothing/Locked/cage.rsi";

    [DataField]
    public string? RequiredFreeSlot;

    [DataField]
    public BreakMode Mode = BreakMode.Breakable;

    [DataField]
    public string RequiredToolTag = "Wirecutter";
    public enum BreakMode
    {
        None,               // Can't be broken, prayed and etc.
        Breakable,          // Breaks and can be fixed.
        Destroyable,        // Destroyed on force.
        ForceOpen           // Opens without break.
    }
}

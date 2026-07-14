using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Antags.Vampires.Components.Classes;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class DantalionComponent : Component
{
    /// <summary>
    ///     Base thrall limit before blood / power bonuses
    /// </summary>
    [DataField]
    public int BaseThrallLimit = 1;

    /// <summary>
    ///     Runtime tracking of enthralled entities
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public HashSet<EntityUid> Thralls = new();

    /// <summary>
    ///     Total thrall slots consumed. Does not decrease when thralls are lost.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public int ThrallSlotsUsed;

    /// <summary>
    ///     Whether Blood Bond is currently active
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    [AutoNetworkedField]
    public bool BloodBondActive;

    /// <summary>
    ///     Thralls currently linked via Blood Bond
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    [AutoNetworkedField]
    public List<EntityUid> BloodBondLinkedThralls = new();

    [AutoNetworkedField]
    public EntProtoId BloodBondBeamPrototype = string.Empty;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool BloodBondProcessingDamage;

    [DataField]
    public EntProtoId RallyOverlayEffect = "VampireRallyOverlayEffect";

    [DataField]
    public int ThrallLevel2Blood = 400;

    [DataField]
    public int ThrallLevel3Blood = 600;

    [DataField]
    public int HealBloodThreshold = 300;

    [DataField]
    public Dictionary<string, int> ThrallHealGroups = new()
    {
        { "Brute", 3 },
        { "Burn", 3 },
    };

    [DataField]
    public Dictionary<string, int> ThrallHealTypes = new()
    {
        { "Asphyxiation", 5 },
    };
}

using System.Threading;
using Content.Shared.FixedPoint;
using Content.Shared.StatusIcon;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.BloodCult.Components;

/// <summary>
/// This is used for tagging a mob as a cultist.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BloodCultistComponent : Component
{
    public static string SummonCultDaggerAction = "InstantActionSummonCultDagger";

    //public static string BloodRitesAction = "InstantActionBloodRites";

    public static string CultTwistedConstructionAction = "ActionCultTwistedConstruction";

    public static string CultTeleportAction = "ActionCultTeleport";

    public static string CultSummonCombatEquipmentAction = "ActionCultSummonCombatEquipment";

    public static string CultStunAction = "ActionCultStun";

    public static string EmpPulseAction = "InstantActionEmpPulse";

    public static string ShadowShacklesAction = "ActionShadowShackles";

    public static string BloodRitualAction = "InstantActionBloodRitual";

    public static string BloodMagicAction = "InstantActionBloodMagic";

    public static List<string> CultistActions = new()
    {
        SummonCultDaggerAction, CultTwistedConstructionAction, CultTeleportAction,
        CultSummonCombatEquipmentAction, CultStunAction, EmpPulseAction, ShadowShacklesAction,
        BloodRitualAction, // BloodRitesAction
    };

    [ViewVariables(VVAccess.ReadWrite), DataField("bloodCharges")]
    public FixedPoint2 BloodCharges = 0;

    [ViewVariables(VVAccess.ReadWrite), DataField("holyConvertChance")]
    public int HolyConvertChance = 33;

    [ViewVariables(VVAccess.ReadWrite), DataField("holyConvertTime")]
    public float HolyConvertTime = 30f;

    public EntityUid? BloodMagicEntity;

    public CancellationTokenSource? HolyConvertToken;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<FactionIconPrototype> StatusIcon { get; set; } = "BloodCultFaction";

    [DataField, AutoNetworkedField]
    public BloodCultType? CultType;
}

using System.Threading;
using Content.Shared.FixedPoint;
using Content.Shared.StatusIcon;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._Sunrise.BloodCult.Components;

/// <summary>
/// This is used for tagging a mob as a cultist.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BloodCultistComponent : Component
{
    [DataField("greetSound", customTypeSerializer: typeof(SoundSpecifierTypeSerializer))]
    public SoundSpecifier? CultistGreetSound = new SoundPathSpecifier("/Audio/CultSounds/fart.ogg");

    [ViewVariables(VVAccess.ReadWrite), DataField("holyConvertTime")]
    public float HolyConvertTime = 30f;

    [ViewVariables(VVAccess.ReadWrite), DataField("holyConvertChance")]
    public int HolyConvertChance = 33;

    public CancellationTokenSource? HolyConvertToken;

    [NonSerialized]
    public List<string> SelectedEmpowers = new();

    public static string SummonCultDaggerAction = "InstantActionSummonCultDagger";

    //public static string BloodRitesAction = "InstantActionBloodRites";

    public static string CultTwistedConstructionAction = "ActionCultTwistedConstruction";

    public static string CultTeleportAction = "ActionCultTeleport";

    public static string CultSummonCombatEquipmentAction = "ActionCultSummonCombatEquipment";

    public static string CultStunAction = "ActionCultStun";

    public static string EmpPulseAction = "InstantActionEmpPulse";

    public static string ShadowShacklesAction = "ActionShadowShackles";

    public static string BloodRitualAction = "InstantActionBloodRitual";

    public static List<string> CultistActions = new()
    {
        SummonCultDaggerAction, CultTwistedConstructionAction, CultTeleportAction,
        CultSummonCombatEquipmentAction, CultStunAction, EmpPulseAction, ShadowShacklesAction, BloodRitualAction, // BloodRitesAction
    };

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<FactionIconPrototype> StatusIcon { get; set; } = "BloodCultFaction";

    [ViewVariables(VVAccess.ReadWrite), DataField("bloodCharges")]
    public FixedPoint2 BloodCharges = 0;
}

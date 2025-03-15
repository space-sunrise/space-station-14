using Content.Shared.Roles;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server._Sunrise.BloodCult.GameRule;

[RegisterComponent, Access(typeof(BloodCultRuleSystem))]
public sealed partial class BloodCultRuleComponent : Component
{
    [DataField("cultistPrototypeId", customTypeSerializer: typeof(PrototypeIdSerializer<AntagPrototype>))]
    public static string CultistPrototypeId = "BloodCultist";

    [DataField("reaperPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public static string ReaperPrototype = "ReaperConstruct";

    [ViewVariables(VVAccess.ReadOnly), DataField("tileId")]
    public static string CultFloor = "CultFloor";

    [DataField("eyeColor")]
    public static Color EyeColor = Color.FromHex("#f80000");

    public static string HolyWaterReagent = "Holywater";

    public static string ChaplainProtoId = "Chaplain";

    [DataField("redEyeThreshold")]
    public static int ReadEyeThreshold = 5;

    [DataField("pentagramThreshold")]
    public static int PentagramThreshold = 8;

    public readonly SoundSpecifier GreatingsSound =
        new SoundPathSpecifier("/Audio/_Sunrise/BloodCult/blood_cult_greeting.ogg");

    [DataField("cultistRolePrototype", customTypeSerializer: typeof(PrototypeIdSerializer<AntagPrototype>))]
    public string CultistRolePrototype = "Cultist";

    [DataField]
    public int CultMembersForSummonGod = 10;

    public List<EntityUid> CultTargets = new();

    [DataField]
    public int MaxTargets = 3;

    [DataField]
    public int MinTargets = 1;

    public List<ICommonSession> StarCandidates = new();

    [DataField("cultistStartingItems", customTypeSerializer: typeof(PrototypeIdListSerializer<EntityPrototype>))]
    public List<string> StartingItems = new();

    [DataField]
    public int TargetsPerPlayer = 30;

    public CultWinCondition WinCondition;
}

public enum CultWinCondition : byte
{
    CultWin,
    CultFailure
}

public sealed class CultNarsieSummoned : EntityEventArgs
{
}

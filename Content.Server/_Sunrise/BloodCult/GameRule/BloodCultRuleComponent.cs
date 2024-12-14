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
    public readonly SoundSpecifier GreatingsSound = new SoundPathSpecifier("/Audio/_Sunrise/BloodCult/blood_cult_greeting.ogg");

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

    public List<ICommonSession> StarCandidates = new();

    [DataField("cultistStartingItems", customTypeSerializer: typeof(PrototypeIdListSerializer<EntityPrototype>))]
    public List<string> StartingItems = new();

    public List<EntityUid> CultTargets = new();

    public CultWinCondition WinCondition;

    [DataField]
    public int MinTargets = 1;

    [DataField]
    public int TargetsPerPlayer = 30;

    [DataField]
    public int MaxTargets = 3;

    [DataField]
    public int CultMembersForSummonGod = 10;
}

public enum CultWinCondition : byte
{
    CultWin,
    CultFailure
}

public sealed class CultNarsieSummoned : EntityEventArgs
{
}

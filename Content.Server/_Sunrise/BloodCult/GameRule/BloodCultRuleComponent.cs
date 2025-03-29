using Content.Shared._Sunrise.BloodCult;
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
    [DataField("reaperPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public static string ReaperPrototype = "ReaperConstruct";

    [ViewVariables(VVAccess.ReadOnly), DataField("tileId")]
    public static string CultFloor = "CultFloor";

    [DataField("eyeColor")]
    public static Color EyeColor = Color.FromHex("#f80000");

    [DataField]
    public int ReadEyeThresholdPercentage = 15;

    [DataField]
    public int PentagramThresholdPercentage = 30;

    public readonly SoundSpecifier GreatingsSound =
        new SoundPathSpecifier("/Audio/_Sunrise/BloodCult/blood_cult_greeting.ogg");

    public List<EntityUid> CultTargets = new();

    public List<EntityUid> SacrificedMinds = new();

    [DataField]
    public int MaxTargets = 3;

    [DataField]
    public int MinTargets = 1;

    [DataField]
    public int TargetsPerPlayer = 30;

    public CultWinCondition WinCondition;

    [DataField]
    public BloodCultType? CultType;

    [DataField]
    public int SacrificeCount = 3;
}

public enum CultWinCondition : byte
{
    CultWin,
    CultFailure
}

public sealed class CultNarsieSummoned : EntityEventArgs
{
}

public sealed class UpdateCultAppearance : EntityEventArgs
{
}

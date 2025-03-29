using Content.Shared._Sunrise.BloodCult;
using Robust.Shared.Audio;

namespace Content.Server._Sunrise.BloodCult.GameRule;

[RegisterComponent, Access(typeof(BloodCultRuleSystem))]
public sealed partial class BloodCultRuleComponent : Component
{
    [DataField]
    public Color EyeColor = Color.FromHex("#f80000");

    [DataField]
    public int ReadEyeThresholdPercentage = 10;

    [DataField]
    public int PentagramThresholdPercentage = 20;

    public readonly SoundSpecifier GreatingsSound =
        new SoundPathSpecifier("/Audio/_Sunrise/BloodCult/blood_cult_greeting.ogg");

    [ViewVariables]
    public List<EntityUid> CultTargets = new();

    [DataField]
    public int MaxTargets = 3;

    [DataField]
    public int MinTargets = 3;

    [DataField]
    public int TargetsPerPlayer = 20;

    [ViewVariables]
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

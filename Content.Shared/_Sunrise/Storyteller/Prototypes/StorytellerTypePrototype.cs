using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Storyteller.Prototypes;

/// <summary>
/// Prototype for storyteller type configuration (Calm, Classic, Insane).
/// Allows easy YAML configuration of budget modification speed, maximum budget, and pacing durations.
/// </summary>
[Prototype]
public sealed partial class StorytellerTypePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public float BudgetModifier = 1f;

    [DataField]
    public float MaxBudgetModifier = 1f;

    [DataField]
    public float DurationMultiplier = 1f;

    // Stage Durations (in minutes)
    [DataField]
    public float RelaxationMinMinutes = 20f;
    [DataField]
    public float RelaxationMaxMinutes = 40f;

    [DataField]
    public float BuildUpMinMinutes = 15f;
    [DataField]
    public float BuildUpMaxMinutes = 30f;

    [DataField]
    public float PeakMinMinutes = 12f;
    [DataField]
    public float PeakMaxMinutes = 24f;

    [DataField]
    public float RecoveryMinMinutes = 10f;
    [DataField]
    public float RecoveryMaxMinutes = 20f;

    // Budget Preservation
    [DataField]
    public float BuildUpPreservationThreshold = 40f;

    // Station Strength Coefficients
    [DataField]
    public float StrengthArmedCrewCoefficient = 10f;
    [DataField]
    public float StrengthSecurityCoefficient = 15f;

    [DataField]
    public float StrengthCargoCoefficient = 0.0005f;

    [DataField]
    public float StrengthTechnologyCoefficient = 3.0f;

    // Threat Scaling Coefficients
    [DataField]
    public float HighBudgetThreshold = 40f;
    [DataField]
    public float MajorThreatWeightMultiplier = 8f;
    [DataField]
    public float MinorThreatWeightMultiplier = 0.1f;
    [DataField]
    public float StationStrengthScalingFactor = 50f;
}

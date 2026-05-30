using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Storyteller.Prototypes;

/// <summary>
/// Prototype for storyteller type configuration (Calm, Classic, Insane).
/// Allows easy YAML configuration of budget modification speed, maximum budget, and pacing durations.
/// </summary>
[Prototype("storytellerType")]
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
}

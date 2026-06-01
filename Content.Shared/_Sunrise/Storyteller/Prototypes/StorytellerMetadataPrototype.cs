using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Storyteller.Prototypes;

/// <summary>
/// Storyteller metadata for game rules. Configures event pacing, scoring, and filters in a separate prototype class.
/// </summary>
[Prototype("storytellerMetadata")]
public sealed partial class StorytellerMetadataPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public StorytellerThreatType ThreatType = StorytellerThreatType.Neutral;

    [DataField]
    public float ThreatCost = 10f;

    [DataField]
    public float StressReduction = 0f;

    [DataField]
    public float MinStress = 0f;

    [DataField]
    public float MaxStress = 100f;

    [DataField]
    public float WeightModifier = 1f;

    // Sunrise-Edit - Custom localization keys for storyteller timeline history
    [DataField("descriptionLocKey")]
    public string? DescriptionLocKey = null;

    [DataField("endedLocKey")]
    public string? EndedLocKey = null;
}

public enum StorytellerThreatType
{
    Helpful,     // Positive events that aid the crew (cargo budget bonus, helpful arrivals, etc.)
    Neutral,     // Minor announcements, cosmetic things
    MinorCalm,   // Minor issues (breaker flips, vent pests, anomalies)
    MajorCalm,   // Moderate issues (ion storms, solar flares)
    MinorAntag,  // Light antags (thieves, minor spider infestations)
    MajorAntag   // Serious threats (traitors, wizards, zombie outbreaks, nukeops)
}

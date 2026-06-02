using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Antags.Vampires.Components.Effects;

/// <summary>
/// Tracks active vampire blood bond beam connections for Dantalion's Blood Bond ability
/// </summary>
[RegisterComponent]
public sealed partial class VampireBloodBondBeamComponent : Component
{
    [DataField]
    public EntProtoId VisualPrototype = "VampireBloodBondBeamVisual";

    /// <summary>
    /// Active beam connections where this entity is the source
    /// </summary>
    [DataField]
    public Dictionary<EntityUid, BloodBondBeamConnection> ActiveBeams = new();
}

/// <summary>
/// Data for blood bond beam connection
/// </summary>
[DataRecord]
public readonly partial record struct BloodBondBeamConnection(
    EntityUid Source,
    EntityUid Target,
    float MaxRange
);

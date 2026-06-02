using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Antags.Vampires.Components.Effects;

/// <summary>
/// Tracks active vampire drain beam connections for Blood Bringers Rite
/// </summary>
[RegisterComponent]
public sealed partial class VampireDrainBeamComponent : Component
{
    [DataField]
    public EntProtoId VisualPrototype = "VampireDrainBeamVisual";

    /// <summary>
    /// Active beam connections where this entity is the source
    /// </summary>
    public Dictionary<EntityUid, DrainBeamConnection> ActiveBeams = new();
}

/// <summary>
/// Data for drain beam connection
/// </summary>
[DataRecord]
public readonly partial record struct DrainBeamConnection(
    EntityUid Source,
    EntityUid Target,
    float MaxRange
);

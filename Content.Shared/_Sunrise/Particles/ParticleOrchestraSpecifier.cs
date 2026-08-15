using System.Numerics;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Particles;

/// <summary>
/// Selects an optional entity visual property used as the orchestra tint.
/// </summary>
[Serializable, NetSerializable]
public enum ParticleVisualColorSource : byte
{
    None,
    SourcePointLight,
    TargetSpriteDominant,
}

/// <summary>
/// Configures one orchestra invocation without duplicating its particle layers.
/// </summary>
[DataDefinition, Serializable, NetSerializable]
public sealed partial class ParticleOrchestraSpecifier
{
    /// <summary>Orchestra prototype to start.</summary>
    [DataField(required: true)]
    public ProtoId<ParticleOrchestraPrototype> Orchestra;

    /// <summary>Optional tint multiplied with a resolved dynamic tint.</summary>
    [DataField]
    public Color? ColorOverride;

    /// <summary>Optional source of an entity-dependent tint.</summary>
    [DataField]
    public ParticleVisualColorSource ColorSource;

    /// <summary>Tint used when <see cref="ColorSource"/> cannot be resolved.</summary>
    [DataField]
    public Color? FallbackColor;

    /// <summary>Global intensity multiplier for all orchestra layers.</summary>
    [DataField]
    public float Intensity = 1f;

    /// <summary>Additional world-space offset shared by all orchestra layers.</summary>
    [DataField]
    public Vector2 SpawnOffset;
}

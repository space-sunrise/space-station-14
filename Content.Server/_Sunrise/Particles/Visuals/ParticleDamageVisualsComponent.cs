using Content.Shared._Sunrise.Particles;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.Particles.Visuals;

/// <summary>
/// Configures particle orchestras for damage impacts and final destruction.
/// </summary>
[RegisterComponent, Access(typeof(ParticleDamageVisualsSystem))]
public sealed partial class ParticleDamageVisualsComponent : Component
{
    /// <summary>Explicit surface reaction used before automatic material resolution.</summary>
    [DataField]
    public ProtoId<ParticleMaterialReactionPrototype>? Material;

    /// <summary>Orchestra played when the entity receives damage from another entity.</summary>
    [DataField]
    public ProtoId<ParticleOrchestraPrototype>? ImpactOrchestra;

    /// <summary>Orchestra played immediately before the entity is destroyed.</summary>
    [DataField]
    public ProtoId<ParticleOrchestraPrototype>? DestructionOrchestra;

    /// <summary>Whether the resolved material may supply a destruction orchestra.</summary>
    [DataField]
    public bool UseMaterialDestruction;

    /// <summary>Minimum positive damage delta required to display an impact.</summary>
    [DataField]
    public float MinimumImpactDamage = 1f;

    /// <summary>Minimum interval between impact visuals on the same entity.</summary>
    [DataField]
    public TimeSpan ImpactCooldown = TimeSpan.FromSeconds(0.08);

    /// <summary>Intensity multiplier for the impact orchestra.</summary>
    [DataField]
    public float ImpactIntensity = 1f;

    /// <summary>Intensity multiplier for the destruction orchestra.</summary>
    [DataField]
    public float DestructionIntensity = 1f;

    /// <summary>Optional tint passed to both orchestras.</summary>
    [DataField]
    public Color? ColorOverride;

    /// <summary>Next server time at which an impact may produce a visual.</summary>
    public TimeSpan NextImpactTime;
}

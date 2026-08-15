using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Particles;

/// <summary>
/// Selects semantic ambient and pulse particle profiles for an anomaly.
/// </summary>
[RegisterComponent]
public sealed partial class AnomalyParticleVisualsComponent : Component
{
    /// <summary>Whether particle visuals should run for this anomaly.</summary>
    [DataField]
    public bool Enabled = true;

    /// <summary>Persistent low-intensity orchestra.</summary>
    [DataField]
    public ProtoId<ParticleOrchestraPrototype> AmbientOrchestra = "AnomalyAmbient";

    /// <summary>Finite orchestra spawned by regular and supercritical pulses.</summary>
    [DataField]
    public ProtoId<ParticleOrchestraPrototype> PulseOrchestra = "AnomalyPulse";

    /// <summary>Base intensity of the persistent orchestra.</summary>
    [DataField]
    public float AmbientIntensity = 0.55f;

    /// <summary>Base intensity of pulse bursts.</summary>
    [DataField]
    public float PulseIntensity = 0.8f;

    /// <summary>Whether the entity point-light color should tint both orchestras.</summary>
    [DataField]
    public bool TintFromPointLight;
}

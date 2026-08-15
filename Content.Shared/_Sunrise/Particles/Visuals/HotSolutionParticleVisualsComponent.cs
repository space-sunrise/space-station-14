using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Particles;

/// <summary>
/// Configures temperature-driven particle visuals for a managed solution.
/// </summary>
[RegisterComponent]
public sealed partial class HotSolutionParticleVisualsComponent : Component
{
    /// <summary>
    /// Managed solution name whose temperature controls the effect.
    /// </summary>
    [DataField(required: true)]
    public string Solution = string.Empty;

    /// <summary>
    /// Orchestra shown while the solution is hot enough.
    /// </summary>
    [DataField]
    public ProtoId<ParticleOrchestraPrototype> Orchestra = "HotSolutionSteam";

    /// <summary>
    /// Temperature in kelvin at which steam first becomes visible.
    /// </summary>
    [DataField]
    public float MinimumTemperature = 315f;

    /// <summary>
    /// Temperature in kelvin at which the orchestra reaches full intensity.
    /// </summary>
    [DataField]
    public float FullIntensityTemperature = 365f;
}

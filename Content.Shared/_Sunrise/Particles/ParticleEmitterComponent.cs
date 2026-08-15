namespace Content.Shared._Sunrise.Particles;

/// <summary>
/// Starts one or more managed particle orchestras while this entity exists on the client.
/// </summary>
[RegisterComponent]
public sealed partial class ParticleEmitterComponent : Component
{
    /// <summary>Orchestras started together on component initialization.</summary>
    [DataField(required: true)]
    public List<ParticleOrchestraSpecifier> Orchestras = [];
}

namespace Content.Shared._Sunrise.Particles;

/// <summary>
/// Starts managed particle orchestras while this entity is in flight after being thrown.
/// </summary>
[RegisterComponent]
public sealed partial class ParticleOnThrownComponent : Component
{
    /// <summary>Orchestras started when the entity is thrown.</summary>
    [DataField(required: true)]
    public List<ParticleOrchestraSpecifier> Orchestras = [];
}

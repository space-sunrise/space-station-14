namespace Content.Shared._Sunrise.Particles;

/// <summary>
/// Starts particle orchestras on each projectile fired by this gun.
/// </summary>
[RegisterComponent]
public sealed partial class ParticleOnGunShotProjectileComponent : Component
{
    /// <summary>Orchestras started on every fired projectile.</summary>
    [DataField(required: true)]
    public List<ParticleOrchestraSpecifier> Orchestras = [];
}

using Content.Client._Sunrise.Particles;

#pragma warning disable IDE0130
namespace Content.Client.Emp;

public sealed partial class EmpSystem
{
    [Dependency] private readonly ParticleOrchestraSystem _particleOrchestra = default!;

    /// <summary>
    /// Spawns a local periodic EMP response over the affected entity's visible bounds.
    /// </summary>
    private void SpawnEmpParticles(EntityUid target)
    {
        _particleOrchestra.Spawn(EmpDisabledParticleOrchestra, target);
    }
}

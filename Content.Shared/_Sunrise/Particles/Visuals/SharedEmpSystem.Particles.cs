using Content.Shared._Sunrise.Particles;
using Robust.Shared.Prototypes;

#pragma warning disable IDE0130
namespace Content.Shared.Emp;

public abstract partial class SharedEmpSystem
{
    protected static readonly ProtoId<ParticleOrchestraPrototype> EmpDisabledParticleOrchestra = "EmpDisabledSparks";

    /// <summary>
    /// Requests an EMP particle response anchored to the affected entity.
    /// </summary>
    private void RaiseEmpParticleVisual(EntityUid target)
    {
        var particleEvent = new ParticleVisualRequestEvent(EmpDisabledParticleOrchestra, target);
        RaiseLocalEvent(ref particleEvent);
    }
}

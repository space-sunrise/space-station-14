using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Particles.Effects;

/// <summary>
/// Spawns a composed client-side particle orchestra on the affected entity.
/// </summary>
public sealed partial class SpawnParticleOrchestraEffect : EntityEffectBase<SpawnParticleOrchestraEffect>
{
    /// <summary>Orchestra prototype to spawn.</summary>
    [DataField(required: true)]
    public ProtoId<ParticleOrchestraPrototype> Orchestra;

    /// <summary>Optional tint applied to every orchestra layer.</summary>
    [DataField]
    public Color? ColorOverride;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;
}

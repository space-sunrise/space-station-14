using Content.Shared._Sunrise.Particles;
using Content.Shared._Sunrise.Particles.Effects;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Client._Sunrise.Particles.Effects;

/// <summary>
/// Executes composed particle entity effects on the client.
/// </summary>
public sealed partial class SpawnParticleOrchestraEffectSystem
    : EntityEffectSystem<TransformComponent, SpawnParticleOrchestraEffect>
{
    [Dependency] private readonly ParticleOrchestraSystem _orchestra = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    protected override void Effect(
        Entity<TransformComponent> entity,
        ref EntityEffectEvent<SpawnParticleOrchestraEffect> args)
    {
        if (!ParticleOrchestraValidator.TryValidateOneShot(
                _prototype,
                args.Effect.Orchestra,
                out var validationError))
        {
            Log.Error(
                $"SpawnParticleOrchestraEffect cannot start orchestra '{args.Effect.Orchestra}': {validationError}");
            return;
        }

        _orchestra.Spawn(args.Effect.Orchestra, entity, colorOverride: args.Effect.ColorOverride);
    }
}

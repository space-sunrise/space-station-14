using Content.Shared._Sunrise.Particles.Effects;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Client._Sunrise.Particles.Effects;

/// <summary>
/// Executes <see cref="SpawnParticleEffect"/> on the client, spawning a particle effect on the target entity.
/// </summary>
public sealed partial class SpawnParticleEffectSystem : EntityEffectSystem<TransformComponent, SpawnParticleEffect>
{
    [Dependency] private readonly ParticleSystem _particles = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<SpawnParticleEffect> args)
    {
        if (!_proto.TryIndex(args.Effect.Effect, out var proto))
        {
            Log.Error($"SpawnParticleEffect references unknown particle effect '{args.Effect.Effect}'");
            return;
        }

        // Наличие timed burst само по себе не останавливает непрерывную эмиссию.
        if (proto.Duration == TimeSpan.Zero && !proto.Burst && proto.EmissionRate > 0f)
        {
            Log.Error($"SpawnParticleEffect tried to spawn '{args.Effect.Effect}' which has infinite duration (Duration=0, Burst=false). " +
                      $"Set a finite Duration or use Burst mode.");
            return;
        }

        _particles.CreateParticle(args.Effect.Effect, entity.Owner, args.Effect.ColorOverride);
    }
}


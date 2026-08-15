using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Particles;

/// <summary>
/// Validates particle orchestras used by fire-and-forget gameplay APIs.
/// </summary>
public static class ParticleOrchestraValidator
{
    /// <summary>
    /// Verifies that an orchestra and all of its sub-emitters terminate without an external stop signal.
    /// </summary>
    public static bool TryValidateOneShot(
        IPrototypeManager prototype,
        ProtoId<ParticleOrchestraPrototype> orchestraId,
        out string? error)
    {
        if (!prototype.TryIndex(orchestraId, out var orchestra))
        {
            error = $"unknown orchestra '{orchestraId}'";
            return false;
        }

        var path = new HashSet<ProtoId<ParticleEffectPrototype>>();
        foreach (var layer in orchestra.Layers)
        {
            if (!TryValidateEffect(prototype, layer.Effect, path, out error))
                return false;
        }

        error = null;
        return true;
    }

    private static bool TryValidateEffect(
        IPrototypeManager prototype,
        ProtoId<ParticleEffectPrototype> effectId,
        HashSet<ProtoId<ParticleEffectPrototype>> path,
        out string? error)
    {
        if (!prototype.TryIndex(effectId, out var effect))
        {
            error = $"unknown particle effect '{effectId}'";
            return false;
        }

        if (!path.Add(effectId))
        {
            error = $"cyclic sub-emitter chain at '{effectId}'";
            return false;
        }

        if (!effect.Burst && effect.Duration <= TimeSpan.Zero && effect.EmissionRate > 0f)
        {
            error = $"persistent particle effect '{effectId}' requires an explicit stop lifecycle";
            path.Remove(effectId);
            return false;
        }

        if (effect.SubEmitterOnSpawn is { } spawnEffect &&
            !TryValidateEffect(prototype, spawnEffect, path, out error))
        {
            path.Remove(effectId);
            return false;
        }

        if (effect.SubEmitterOnDeath is { } deathEffect &&
            !TryValidateEffect(prototype, deathEffect, path, out error))
        {
            path.Remove(effectId);
            return false;
        }

        path.Remove(effectId);
        error = null;
        return true;
    }
}

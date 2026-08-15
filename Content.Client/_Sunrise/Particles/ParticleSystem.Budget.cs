using System.Numerics;
using Content.Shared._Sunrise.Particles;

namespace Content.Client._Sunrise.Particles;

public sealed partial class ParticleSystem
{
    // Ограничение нагрузки, повторное использование объектов и очистка эмиттеров.
    private static bool AllBurstsFired(ActiveEmitter emitter)
    {
        foreach (var fired in emitter.FiredBursts)
        {
            if (!fired)
                return false;
        }

        return true;
    }

    private int GetScaledMaxCount(
        ParticleEffectPrototype proto,
        int maxCount,
        float qualityMultiplier,
        float intensity)
    {
        var scaled = Math.Min(
            Math.Max(0, maxCount) * (double)qualityMultiplier * intensity,
            HardMaxParticles);
        var result = (int)Math.Ceiling(scaled);

        return GetEffectivePriority(proto) == ParticleEffectPriority.Critical &&
               _quality < QualityMultipliers.Length - 1
            ? Math.Min(result, IgnoreQualityMaxParticles)
            : result;
    }

    private float GetQualityMultiplier(ParticleEffectPrototype proto)
    {
        if (GetEffectivePriority(proto) == ParticleEffectPriority.Critical)
            return 1f;

        if (!IsQualityEnabled(proto.MinimumQuality))
            return 0f;

        return QualityMultipliers[Math.Clamp(_quality, 0, QualityMultipliers.Length - 1)];
    }

    private bool TryEmitParticle(ActiveEmitter emitter, float eyeAngle, int globalBudget)
    {
        if (_liveParticleCount >= globalBudget && !TryEvictLowerPriorityParticle(GetEffectivePriority(emitter.Proto)))
            return false;

        EmitParticle(emitter, eyeAngle);
        return true;
    }

    private bool TryEvictLowerPriorityParticle(ParticleEffectPriority requestedPriority)
    {
        if (requestedPriority == ParticleEffectPriority.Decorative)
            return false;

        var selectedEmitter = -1;
        var selectedPriority = requestedPriority;
        for (var emitterIndex = 0; emitterIndex < _emitters.Count; emitterIndex++)
        {
            var emitter = _emitters[emitterIndex];
            if (emitter.Particles.Count == 0)
                continue;

            var priority = GetEffectivePriority(emitter.Proto);
            if (priority >= selectedPriority)
                continue;

            selectedPriority = priority;
            selectedEmitter = emitterIndex;
            if (priority == ParticleEffectPriority.Decorative)
                break;
        }

        if (selectedEmitter < 0)
            return false;

        var selected = _emitters[selectedEmitter];
        ReleaseParticleAt(selected, selected.Particles.Count - 1);
        return true;
    }

    private bool TryAdmitEmitter(ParticleEffectPriority requestedPriority)
    {
        if (_emitters.Count < HardMaxEmitters)
            return true;

        for (var emitterIndex = 0; emitterIndex < _emitters.Count; emitterIndex++)
        {
            var emitter = _emitters[emitterIndex];
            if (!emitter.Exhausted || emitter.Particles.Count > 0)
                continue;

            RecycleEmitterAt(emitterIndex);
            return true;
        }

        for (var emitterIndex = 0; emitterIndex < _emitters.Count; emitterIndex++)
        {
            var emitter = _emitters[emitterIndex];
            // Внешние визуальные системы хранят прямые ссылки на активные эмиттеры, поэтому вытеснять можно только завершённые.
            if (!emitter.Exhausted || GetEffectivePriority(emitter.Proto) >= requestedPriority)
                continue;

            RecycleEmitterAt(emitterIndex);
            return true;
        }

        return false;
    }

    private static ParticleEffectPriority GetEffectivePriority(ParticleEffectPrototype proto)
        => proto.IgnoreQualitySettings ? ParticleEffectPriority.Critical : proto.Priority;

    internal static Vector2 GetEmitterSpawnOffset(ActiveEmitter emitter)
        => emitter.VisualAnchor.HasValue
            ? emitter.SpawnOffset
            : emitter.Overrides?.SpawnOffset ?? emitter.SpawnOffset;

    private void ReleaseParticleAt(ActiveEmitter emitter, int particleIndex)
    {
        var lastIndex = emitter.Particles.Count - 1;
        var particle = emitter.Particles[particleIndex];
        if (particleIndex != lastIndex)
            emitter.Particles[particleIndex] = emitter.Particles[lastIndex];
        emitter.Particles.RemoveAt(lastIndex);

        particle.Reset();
        if (_particlePool.Count < HardMaxParticles)
            _particlePool.Push(particle);

        _liveParticleCount--;
    }

    private void ReleaseEmitterParticles(ActiveEmitter emitter)
    {
        var releasedCount = emitter.Particles.Count;
        foreach (var particle in emitter.Particles)
        {
            particle.Reset();
            if (_particlePool.Count < HardMaxParticles)
                _particlePool.Push(particle);
        }

        _liveParticleCount -= releasedCount;
        emitter.Particles.Clear();
    }

    private void RecycleEmitterAt(int emitterIndex)
    {
        var emitter = _emitters[emitterIndex];
        ReleaseEmitterParticles(emitter);
        _emitters.RemoveAt(emitterIndex);
    }

    private void ReleaseAllEmitters()
    {
        for (var emitterIndex = _emitters.Count - 1; emitterIndex >= 0; emitterIndex--)
            RecycleEmitterAt(emitterIndex);
    }

    private static float NormalizeIntensity(float intensity)
        => float.IsFinite(intensity)
            ? Math.Clamp(intensity, 0f, MaxIntensity)
            : 0f;

    private static bool IsPersistentEmitter(
        ParticleEffectPrototype proto,
        ParticleRuntimeOverrides? overrides)
    {
        var duration = overrides?.Duration ?? proto.Duration;
        var emissionRate = overrides?.EmissionRate ?? proto.EmissionRate;
        return !proto.Burst && duration <= TimeSpan.Zero && emissionRate > 0f;
    }

    private void KillCosmeticParticles()
    {
        foreach (var emitter in _emitters)
        {
            if (GetEffectivePriority(emitter.Proto) == ParticleEffectPriority.Critical)
                continue;

            ReleaseEmitterParticles(emitter);
            emitter.EmitAccum = 0f;
        }
    }

}

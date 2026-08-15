using System.Numerics;
using Content.Shared._Sunrise.Particles;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;

namespace Content.Client._Sunrise.Particles;

public sealed partial class ParticleSystem
{
    // Симуляция активных эмиттеров и отдельных частиц.
    private void TickEmitter(ActiveEmitter emitter, float dt, float eyeAngle)
    {
        var proto = emitter.Proto;

        // Aim-at: recompute emit angle toward target each tick
        Vector2? targetWorldPos = null;
        if (emitter.TargetEntity is { } targetEnt)
        {
            if (!Deleted(targetEnt))
                targetWorldPos = _transform.GetMapCoordinates(targetEnt).Position;
            else
                emitter.TargetEntity = null; // entity GONE, fall to TargetPosition
        }
        if (targetWorldPos == null && emitter.TargetPosition.HasValue)
            targetWorldPos = emitter.TargetPosition.Value;

        if (targetWorldPos.HasValue)
        {
            var spawnOffset = GetEmitterSpawnOffset(emitter);
            var worldDir = targetWorldPos.Value - (emitter.MapCoords.Position + spawnOffset);
            if (worldDir.LengthSquared() > 0.0001f)
            {
                // Convert world direction to screen-space direction to angle (0 = screen-up)
                var cosE = MathF.Cos(eyeAngle);
                var sinE = MathF.Sin(eyeAngle);
                var sx = worldDir.X * cosE - worldDir.Y * sinE;
                var sy = worldDir.X * sinE + worldDir.Y * cosE;
                emitter.EffectiveEmitAngle = MathF.Atan2(sx, sy);
            }
        }
        else if (emitter.Overrides?.EmitAngle is { } overriddenAngle)
        {
            emitter.EffectiveEmitAngle = (float) overriddenAngle.Theta;
        }
        else if (TryGetVelocityEmitAngle(emitter, eyeAngle, out var velocityAngle))
        {
            emitter.EffectiveEmitAngle = velocityAngle;
        }
        else
        {
            emitter.EffectiveEmitAngle = (float) proto.EmitAngle.Theta;
        }

        // Resolve overridable scalars once per tick
        var ovr          = emitter.Overrides;
        var drag         = ovr?.Drag          ?? proto.Drag;
        var constForce   = ovr?.ConstantForce ?? proto.ConstantForce;
        var termSpeed    = ovr?.TerminalSpeed ?? proto.TerminalSpeed;
        var gravity      = ovr?.Gravity       ?? proto.Gravity;
        var noiseStr     = ovr?.NoiseStrength ?? proto.NoiseStrength;
        var noiseFreq    = ovr?.NoiseFrequency ?? proto.NoiseFrequency;
        var duration     = (float)(ovr?.Duration      ?? proto.Duration).TotalSeconds;
        var emissionRate = ovr?.EmissionRate  ?? proto.EmissionRate;
        var maxCount     = ovr?.MaxCount      ?? proto.MaxCount;
        var intensity    = NormalizeIntensity(emitter.Intensity);
        emitter.Intensity = intensity;

        // Precompute per-tick constants for SimulateParticle to avoid recomputing per particle.
        var dragMul     = drag > 0f ? MathF.Exp(-drag * dt) : 1f;
        var termSpeedSq = termSpeed > 0f ? termSpeed * termSpeed : float.MaxValue;

        // Advance age and check duration
        emitter.Age += TimeSpan.FromSeconds(dt);
        if (!emitter.Exhausted && duration > 0f && emitter.Age.TotalSeconds >= duration)
            emitter.Exhausted = true;

        // RSI animation
        if (emitter.Delays.Length > 0 && emitter.Frames.Length > 0)
        {
            emitter.AnimTimer += dt;
            while (emitter.AnimTimer >= emitter.Delays[emitter.AnimFrame])
            {
                var delay = emitter.Delays[emitter.AnimFrame];
                if (delay <= 0f)
                    break;
                emitter.AnimTimer -= delay;
                emitter.AnimFrame = (emitter.AnimFrame + 1) % emitter.Frames.Length;
            }
        }

        // Simulate live particles
        for (var particleIndex = emitter.Particles.Count - 1; particleIndex >= 0; particleIndex--)
        {
            var p = emitter.Particles[particleIndex];

            p.Age += TimeSpan.FromSeconds(dt);

            if (p.Age >= p.Lifetime)
            {
                if (proto.SubEmitterOnDeath.HasValue)
                {
                    var worldPos = ComputeParticleWorldPos(p, emitter, eyeAngle);
                    _pendingSubEmitters.Add((proto.SubEmitterOnDeath.Value,
                        new MapCoordinates(worldPos, emitter.MapCoords.MapId),
                        emitter.SubEmitterDepth + 1));
                }

                ReleaseParticleAt(emitter, particleIndex);
                continue;
            }

            SimulateParticle(
                p,
                p.AgeRatio,
                dt,
                dragMul,
                constForce,
                termSpeed,
                termSpeedSq,
                gravity,
                noiseStr,
                noiseFreq,
                emitter.Compiled);
        }

        // Timed bursts
        if (!emitter.Exhausted)
        {
            for (int b = 0; b < proto.Bursts.Count; b++)
            {
                if (emitter.FiredBursts[b])
                    continue;
                var burst = proto.Bursts[b];
                if (emitter.Age < burst.Time)
                    continue;

                // Bypass quality settings for gameplay-critical particles
                var qualityMult = GetQualityMultiplier(proto);
                var scaledMax = GetScaledMaxCount(proto, maxCount, qualityMult, intensity);
                var scaledBurst = Math.Min(Math.Max(0, burst.Count) * (double)qualityMult * intensity, scaledMax);
                var toEmit = (int)Math.Ceiling(scaledBurst);
                var globalBudget = GetGlobalBudget(proto);
                for (int j = 0; j < toEmit && emitter.Particles.Count < scaledMax; j++)
                {
                    if (!TryEmitParticle(emitter, eyeAngle, globalBudget))
                        break;
                }
                emitter.FiredBursts[b] = true;
            }
        }

        // Continuous emission
        if (!emitter.Exhausted && !proto.Burst)
        {
            var qualityMult = GetQualityMultiplier(proto);
            // IgnoreQualitySettings emitters are capped at IgnoreQualityMaxParticles unless quality is High.
            var scaledMax = GetScaledMaxCount(proto, maxCount, qualityMult, intensity);
            var canEmit = scaledMax - emitter.Particles.Count;
            if (canEmit > 0)
            {
                // EmissionOverTime rate multiplier
                float emissionMult = 1f;
                if (emitter.Compiled.EmissionOverTime is { } emissionSamples)
                {
                    var t = duration > 0f
                        ? Math.Clamp((float)(emitter.Age.TotalSeconds / duration), 0f, 1f)
                        : Math.Clamp((float)emitter.Age.TotalSeconds, 0f, 1f);
                    emissionMult = CompiledParticleEffect.Sample(emissionSamples, t);
                }

                var emittedThisFrame = emissionRate * emissionMult * dt * intensity;
                if (float.IsFinite(emittedThisFrame) && emittedThisFrame > 0f)
                    emitter.EmitAccum = Math.Min(emitter.EmitAccum + emittedThisFrame, HardMaxParticles);

                var toEmit = (int)emitter.EmitAccum;
                emitter.EmitAccum -= toEmit;
                toEmit = Math.Min(toEmit, canEmit);

                var globalBudget = GetGlobalBudget(proto);
                for (int i = 0; i < toEmit; i++)
                {
                    if (!TryEmitParticle(emitter, eyeAngle, globalBudget))
                        break;
                }
            }
        }

        // Burst-only расписание без непрерывной эмиссии завершается само.
        if (!emitter.Exhausted && duration <= 0f && emissionRate <= 0f && AllBurstsFired(emitter))
            emitter.Exhausted = true;
    }

    private void BurstEmit(ActiveEmitter emitter)
    {
        var proto = emitter.Proto;
        var eyeAngle = (float)_eye.CurrentEye.Rotation;
        // Bypass quality settings for gameplay-critical particles
        var qualityMult = GetQualityMultiplier(proto);
        var maxCount = emitter.Overrides?.MaxCount ?? proto.MaxCount;
        var intensity = NormalizeIntensity(emitter.Intensity);
        emitter.Intensity = intensity;
        var count = GetScaledMaxCount(proto, maxCount, qualityMult, intensity);
        var globalBudget = GetGlobalBudget(proto);
        for (int i = 0; i < count; i++)
        {
            if (!TryEmitParticle(emitter, eyeAngle, globalBudget))
                break;
        }
    }

    private void EmitParticle(ActiveEmitter emitter, float eyeAngle)
    {
        var proto = emitter.Proto;

        var p = _particlePool.TryPop(out var pooled) ? pooled : new ParticleData();
        p.Reset();

        // Resolve spawn time overridable fields
        _liveParticleCount++;
        var ovr = emitter.Overrides;
        var lifetime        = (float)(ovr?.Lifetime         ?? proto.Lifetime).TotalSeconds;
        var lifetimeVar     = (float)(ovr?.LifetimeVariance  ?? proto.LifetimeVariance).TotalSeconds;
        var spreadAngle     = (float)(ovr?.SpreadAngle?.Theta     ?? proto.SpreadAngle.Theta);
        var speed0          = ovr?.Speed             ?? proto.Speed;
        var speedVar        = ovr?.SpeedVariance     ?? proto.SpeedVariance;
        var sizeVar         = ovr?.SizeVariance      ?? proto.SizeVariance;
        var inheritVel      = ovr?.InheritVelocity   ?? proto.InheritVelocity;
        var startRot        = (float)(ovr?.StartRotation?.Theta         ?? proto.StartRotation.Theta);
        var startRotVar     = (float)(ovr?.StartRotationVariance?.Theta ?? proto.StartRotationVariance.Theta);
        var rotSpeed        = (float)(ovr?.RotationSpeed?.Theta         ?? proto.RotationSpeed.Theta);
        var rotSpeedVar     = (float)(ovr?.RotationSpeedVariance?.Theta ?? proto.RotationSpeedVariance.Theta);
        var emissionShape   = ovr?.EmissionShape      ?? proto.Shape.Type;
        var emissionRadius  = ovr?.EmissionRadius     ?? proto.Shape.Radius;
        var emissionExtents = ovr?.EmissionBoxExtents ?? proto.Shape.BoxExtents;
        var emissionLineStart = ovr?.EmissionLineStart ?? proto.Shape.LineStart;
        var emissionLineEnd = ovr?.EmissionLineEnd ?? proto.Shape.LineEnd;
        var emissionTriangleLength = ovr?.EmissionTriangleLength ?? proto.Shape.TriangleLength;
        var emissionTriangleHalfWidth = ovr?.EmissionTriangleHalfWidth ?? proto.Shape.TriangleHalfWidth;

        p.Lifetime = TimeSpan.FromSeconds(lifetime + _random.NextFloat(-lifetimeVar, lifetimeVar));
        if (p.Lifetime < TimeSpan.FromSeconds(0.05))
            p.Lifetime = TimeSpan.FromSeconds(0.05);

        var spreadHalf = spreadAngle * 0.5f;
        var spreadOffset = _random.NextFloat(-spreadHalf, spreadHalf);
        var angle = emitter.EffectiveEmitAngle + spreadOffset;

        var speed = speed0 + _random.NextFloat(-speedVar, speedVar);
        speed = Math.Max(speed, 0f);

        p.LocalOffset = SampleEmissionShape(
            emissionShape,
            emissionRadius,
            emissionExtents,
            emissionLineStart,
            emissionLineEnd,
            emissionTriangleLength,
            emissionTriangleHalfWidth,
            emitter.EffectiveEmitAngle);

        var velocityDirection = new Vector2(MathF.Sin(angle), MathF.Cos(angle));
        if ((proto.DirectionMode == ParticleEmissionDirectionMode.RadialOutward ||
             proto.DirectionMode == ParticleEmissionDirectionMode.RadialInward) &&
            emitter.TargetEntity == null &&
            emitter.TargetPosition == null &&
            emitter.Overrides?.EmitAngle == null &&
            p.LocalOffset.LengthSquared() > 0.0001f)
        {
            // Радиальное направление вычисляется после выбора точки на форме эмиссии.
            var radialDirection = Vector2.Normalize(p.LocalOffset);
            if (proto.DirectionMode == ParticleEmissionDirectionMode.RadialInward)
                radialDirection = -radialDirection;

            var spreadCos = MathF.Cos(spreadOffset);
            var spreadSin = MathF.Sin(spreadOffset);
            velocityDirection = new Vector2(
                radialDirection.X * spreadCos + radialDirection.Y * spreadSin,
                radialDirection.Y * spreadCos - radialDirection.X * spreadSin);
        }

        p.Velocity = velocityDirection * speed;

        // Для world-space смещение задаётся через SpawnOrigin и не применяется второй раз.
        var spawnOffset = GetEmitterSpawnOffset(emitter);

        // InheritVelocity: convert emitter world velocity to screen space then add
        if (inheritVel != 0f && emitter.EmitterVelocity != Vector2.Zero)
        {
            var cosE = MathF.Cos(eyeAngle);
            var sinE = MathF.Sin(eyeAngle);
            var wv = emitter.EmitterVelocity * inheritVel;
            var screenVel = new Vector2(wv.X * cosE - wv.Y * sinE, wv.X * sinE + wv.Y * cosE);
            p.Velocity += screenVel;
        }

        if (proto.WorldSpace)
            p.SpawnOrigin = emitter.MapCoords.Position + spawnOffset;

        p.SpawnSpeed = speed;
        p.SpawnIntensity = emitter.Intensity;

        // SizeVariance
        if (sizeVar > 0f)
            p.SizeMultiplier = 1f + _random.NextFloat(-sizeVar, sizeVar);
        else
            p.SizeMultiplier = 1f;

        p.Rotation = startRot + _random.NextFloat(-startRotVar, startRotVar);
        p.RotationSpeed = rotSpeed + _random.NextFloat(-rotSpeedVar, rotSpeedVar);

        // Unique noise offset so each particle gets different turbulence
        p.NoiseOffset = new Vector2(_random.NextFloat(-100f, 100f), _random.NextFloat(-100f, 100f));

        emitter.Particles.Add(p);

        // Sub-emitter on spawn
        if (proto.SubEmitterOnSpawn.HasValue)
        {
            var worldPos = ComputeParticleWorldPos(p, emitter, eyeAngle);
            _pendingSubEmitters.Add((proto.SubEmitterOnSpawn.Value,
                new MapCoordinates(worldPos, emitter.MapCoords.MapId),
                emitter.SubEmitterDepth + 1));
        }
    }

    /// <summary>
    /// Advances a single live particle's simulation by one step.
    /// </summary>
    private static void SimulateParticle(
        ParticleData p,
        float ageRatio,
        float dt,
        float dragMul,
        Vector2 constForce,
        float termSpeed,
        float termSpeedSq,
        float gravity,
        float noiseStr,
        float noiseFreq,
        CompiledParticleEffect compiled)
    {
        // Drag: dragMul is MathF.Exp(-drag * dt) precomputed per tick
        if (dragMul < 1f)
            p.Velocity *= dragMul;

        // ConstantForce
        if (constForce != Vector2.Zero)
            p.Velocity += constForce * dt;

        // ForceOverLifetime
        if (compiled.ForceOverLifetime is { } forceSamples)
            p.Velocity += CompiledParticleEffect.Sample(forceSamples, ageRatio) * dt;

        // SpeedOverLifetime: rescale velocity magnitude to the curve-defined speed
        if (compiled.SpeedOverLifetime is { } speedSamples)
        {
            var curveSpeed = CompiledParticleEffect.Sample(speedSamples, ageRatio) * p.SpawnSpeed;
            var currentSpeed = p.Velocity.Length();
            if (currentSpeed > 0f)
                p.Velocity = p.Velocity / currentSpeed * curveSpeed;
        }

        // Terminal speed cap: termSpeedSq is termSpeed*termSpeed precomputed per tick
        if (termSpeedSq < float.MaxValue)
        {
            var speedSq = p.Velocity.LengthSquared();
            if (speedSq > termSpeedSq)
                p.Velocity *= termSpeed / MathF.Sqrt(speedSq);
        }

        // Advance position
        p.LocalOffset += p.Velocity * dt;

        // VelocityOverLifetime: positional nudge (does not modify velocity)
        if (compiled.VelocityOverLifetime is { } velocitySamples)
            p.LocalOffset += CompiledParticleEffect.Sample(velocitySamples, ageRatio) * dt;

        // Gravity
        if (gravity != 0f)
            p.LocalOffset.Y += -gravity * dt * ageRatio;

        // Noise
        if (noiseStr > 0f)
        {
            var ageSec = (float)p.Age.TotalSeconds;
            var nx = ValueNoise(p.NoiseOffset.X + ageSec * noiseFreq, p.NoiseOffset.Y);
            var ny = ValueNoise(p.NoiseOffset.X, p.NoiseOffset.Y + ageSec * noiseFreq);
            p.LocalOffset += new Vector2(nx, ny) * noiseStr * dt;
        }

        // Вращение.
        if (p.RotationSpeed != 0f)
            p.Rotation += p.RotationSpeed * dt;
    }

    /// <summary>Converts a particle's screen-space LocalOffset to a world position.</summary>
    private static Vector2 ComputeParticleWorldPos(ParticleData p, ActiveEmitter emitter, float eyeAngle)
    {
        var cosR = MathF.Cos(-eyeAngle);
        var sinR = MathF.Sin(-eyeAngle);
        var worldOffset = new Vector2(p.LocalOffset.X * cosR - p.LocalOffset.Y * sinR,
                                      p.LocalOffset.X * sinR + p.LocalOffset.Y * cosR);
        var spawnOffset = GetEmitterSpawnOffset(emitter);
        var origin = emitter.Proto.WorldSpace
            ? p.SpawnOrigin
            : emitter.MapCoords.Position + spawnOffset;
        return origin + worldOffset;
    }

    private static bool TryGetVelocityEmitAngle(ActiveEmitter emitter, float eyeAngle, out float emitAngle)
    {
        emitAngle = 0f;
        if (emitter.Proto.DirectionMode == ParticleEmissionDirectionMode.Prototype)
            return false;

        var worldDirection = emitter.Proto.DirectionMode switch
        {
            ParticleEmissionDirectionMode.EmitterVelocity => emitter.EmitterVelocity,
            ParticleEmissionDirectionMode.OppositeEmitterVelocity => -emitter.EmitterVelocity,
            _ => Vector2.Zero,
        };
        if (worldDirection.LengthSquared() <= 0.0001f)
            return false;

        var cos = MathF.Cos(eyeAngle);
        var sin = MathF.Sin(eyeAngle);
        var screenX = worldDirection.X * cos - worldDirection.Y * sin;
        var screenY = worldDirection.X * sin + worldDirection.Y * cos;
        emitAngle = MathF.Atan2(screenX, screenY);
        return true;
    }

    // Привязанные эмиттеры не должны терять сущность при выходе за границы экрана.
    private void UpdateEmitterTransform(ActiveEmitter emitter, float dt)
    {
        var newPosition = emitter.MapCoords.Position;
        var newMapId = emitter.MapCoords.MapId;
        EntityUid? attachedEntity = null;
        if (emitter.AttachedEntity is { } attached)
        {
            if (Deleted(attached))
            {
                emitter.Exhausted = true;
                emitter.AttachedEntity = null;
            }
            else
            {
                var attachedCoordinates = _transform.GetMapCoordinates(attached);
                newPosition = attachedCoordinates.Position;
                newMapId = attachedCoordinates.MapId;
                emitter.MapCoords = attachedCoordinates;
                attachedEntity = attached;

                if (emitter.VisualAnchor is { } visualAnchor)
                {
                    emitter.SpawnOffset = _anchors.GetOffset(
                            attached,
                            visualAnchor,
                            emitter.VisualAnchorLateralOffset) +
                        emitter.VisualAnchorOffset;
                }
            }
        }

        // Физическая скорость не дрожит между кадрами визуализации и поэтому приоритетнее разницы позиций.
        if (attachedEntity is { } physicsEntity &&
            TryComp<PhysicsComponent>(physicsEntity, out var physics))
        {
            emitter.EmitterVelocity = _physics.GetMapLinearVelocity(physicsEntity, physics);
            emitter.PreviousPosition = newPosition;
            emitter.PreviousMapId = newMapId;
            emitter.VelocityInitialized = true;
            return;
        }

        if (!emitter.VelocityInitialized)
        {
            emitter.PreviousPosition = newPosition;
            emitter.PreviousMapId = newMapId;
            emitter.VelocityInitialized = true;
            return;
        }

        if (emitter.PreviousMapId != newMapId)
        {
            emitter.EmitterVelocity = Vector2.Zero;
        }
        else if (dt > 0f)
        {
            emitter.EmitterVelocity = (newPosition - emitter.PreviousPosition) / dt;
        }

        emitter.PreviousPosition = newPosition;
        emitter.PreviousMapId = newMapId;
    }

    /// <summary>
    /// Ages particles on off-screen emitters without running full simulation.
    /// Kills expired particles and decrements the live count.
    /// </summary>
    private void AgeOffScreenParticles(ActiveEmitter emitter, float dt)
    {
        emitter.Age += TimeSpan.FromSeconds(dt);

        // Завершённые вне экрана эффекты не остаются в списке навсегда.
        var proto = emitter.Proto;
        var duration = emitter.Overrides?.Duration ?? proto.Duration;
        if (!emitter.Exhausted && duration > TimeSpan.Zero && emitter.Age >= duration)
            emitter.Exhausted = true;

        for (var i = 0; i < proto.Bursts.Count; i++)
        {
            if (!emitter.FiredBursts[i] && emitter.Age >= proto.Bursts[i].Time)
                emitter.FiredBursts[i] = true;
        }

        var emissionRate = emitter.Overrides?.EmissionRate ?? proto.EmissionRate;
        if (!emitter.Exhausted && duration <= TimeSpan.Zero && emissionRate <= 0f && AllBurstsFired(emitter))
            emitter.Exhausted = true;

        for (var particleIndex = emitter.Particles.Count - 1; particleIndex >= 0; particleIndex--)
        {
            var p = emitter.Particles[particleIndex];
            p.Age += TimeSpan.FromSeconds(dt);
            if (p.Age >= p.Lifetime)
                ReleaseParticleAt(emitter, particleIndex);
        }
    }
}

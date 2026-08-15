using System.Numerics;
using Content.Shared._Sunrise.Particles;
using Robust.Shared.Map;

namespace Content.Client._Sunrise.Particles;

/// <summary>
/// A managed group of emitters created by one particle orchestra invocation.
/// </summary>
public sealed class ActiveParticleOrchestra
{
    /// <summary>Whether this orchestra was explicitly stopped.</summary>
    public bool IsStopped { get; internal set; }

    internal readonly List<ActiveParticleOrchestraEmitter> Emitters = [];
    internal readonly ActiveParticleOrchestraContext Context;

    internal ActiveParticleOrchestra(ActiveParticleOrchestraContext context)
    {
        Context = context;
    }
}

internal sealed class ActiveParticleOrchestraContext(
    MapCoordinates coordinates,
    EntityUid? source,
    EntityUid? target,
    Vector2 movement,
    Color? colorOverride,
    float intensity,
    ParticleRuntimeOverrides? runtimeOverrides,
    Vector2 spawnOffset)
{
    public readonly MapCoordinates Coordinates = coordinates;
    public readonly EntityUid? Source = source;
    public readonly EntityUid? Target = target;
    public readonly Vector2 Movement = movement;
    public Color? ColorOverride = colorOverride;
    public float Intensity = intensity;
    public ParticleRuntimeOverrides? RuntimeOverrides = runtimeOverrides;
    public Vector2 SpawnOffset = spawnOffset;
    public Vector2? TargetPosition;
}

internal sealed class ActiveParticleOrchestraEmitter(
    ActiveEmitter emitter,
    ParticleOrchestraLayerData layer)
{
    public readonly ActiveEmitter Emitter = emitter;
    public readonly ParticleOrchestraLayerData Layer = layer;
}

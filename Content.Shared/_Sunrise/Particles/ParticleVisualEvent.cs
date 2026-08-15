using System.Numerics;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Particles;

/// <summary>
/// Requests a particle visual from shared predicted gameplay without exposing individual effect layers.
/// </summary>
[ByRefEvent]
public readonly record struct ParticleVisualRequestEvent(
    ProtoId<ParticleOrchestraPrototype> Orchestra,
    EntityUid Source,
    EntityUid? Target = null,
    EntityUid? PredictedBy = null,
    Vector2 Movement = default,
    Color? ColorOverride = null,
    float Intensity = 1f,
    MapCoordinates? Coordinates = null,
    ParticleVisualColorSource ColorSource = ParticleVisualColorSource.None,
    Color? FallbackColor = null,
    Vector2 SpawnOffset = default);

/// <summary>
/// Sends a semantic particle visual to clients without networking individual particles.
/// </summary>
[Serializable, NetSerializable]
public sealed class ParticleVisualEvent(
    ProtoId<ParticleOrchestraPrototype> orchestra,
    MapCoordinates coordinates,
    NetEntity? source = null,
    NetEntity? target = null,
    Vector2 movement = default,
    Color? colorOverride = null,
    float intensity = 1f,
    ParticleVisualColorSource colorSource = ParticleVisualColorSource.None,
    Color? fallbackColor = null,
    Vector2 spawnOffset = default) : EntityEventArgs
{
    /// <summary>Orchestra prototype to expand on the client.</summary>
    public readonly ProtoId<ParticleOrchestraPrototype> Orchestra = orchestra;

    /// <summary>Exact fallback position and position context for detached layers.</summary>
    public readonly MapCoordinates Coordinates = coordinates;

    /// <summary>Optional source entity used by anchors and directional layers.</summary>
    public readonly NetEntity? Source = source;

    /// <summary>Optional target entity used by directional layers.</summary>
    public readonly NetEntity? Target = target;

    /// <summary>Optional world-space movement or impact direction.</summary>
    public readonly Vector2 Movement = movement;

    /// <summary>Optional tint multiplied with per-layer tints.</summary>
    public readonly Color? ColorOverride = colorOverride;

    /// <summary>Global orchestra intensity multiplier.</summary>
    public readonly float Intensity = intensity;

    /// <summary>Optional source of an entity-dependent tint.</summary>
    public readonly ParticleVisualColorSource ColorSource = colorSource;

    /// <summary>Tint used when <see cref="ColorSource"/> cannot be resolved.</summary>
    public readonly Color? FallbackColor = fallbackColor;

    /// <summary>Additional world-space offset shared by all orchestra layers.</summary>
    public readonly Vector2 SpawnOffset = spawnOffset;
}

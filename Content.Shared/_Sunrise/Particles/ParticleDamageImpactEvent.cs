using System.Numerics;
using Content.Shared.Damage;

namespace Content.Shared._Sunrise.Particles;

/// <summary>
/// Supplies an exact world-space contact point for a material-aware damage visual.
/// </summary>
[ByRefEvent]
public readonly record struct ParticleDamageImpactEvent(
    DamageSpecifier Damage,
    EntityUid Origin,
    Vector2 WorldPosition,
    Vector2 Movement);

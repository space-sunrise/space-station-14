using System.Numerics;
using Content.Shared._Sunrise.Particles;
using Robust.Client.Graphics;
using Robust.Shared.Map;

namespace Content.Client._Sunrise.Particles;

/// <summary>
/// A running particle emitter and its dense collection of live particles.
/// Created in <see cref="ParticleSystem"/>.
/// </summary>
public sealed class ActiveEmitter
{
    public ParticleEffectPrototype Proto = default!;

    /// <summary>Precompiled curve lookup tables shared by every particle in this emitter.</summary>
    public CompiledParticleEffect Compiled = default!;

    /// <summary>
    /// How many sub-emitter links deep this emitter is. Root emitters are 0.
    /// Sub-emitters will not spawn if depth would exceed <see cref="ParticleSystem.MaxSubEmitterDepth"/>.
    /// </summary>
    public int SubEmitterDepth;

    /// <summary>Current world-space origin of the emitter.</summary>
    public MapCoordinates MapCoords;

    /// <summary>
    /// Additional world-space offset from <see cref="MapCoords"/> applied to the spawn origin.
    /// Useful for nudging effects away from entity anchor points.
    /// </summary>
    public Vector2 SpawnOffset;

    /// <summary>Entity this emitter follows (if any).</summary>
    public EntityUid? AttachedEntity;

    /// <summary>Semantic visual anchor recomputed while following <see cref="AttachedEntity"/>.</summary>
    public ParticleVisualAnchor? VisualAnchor;

    /// <summary>Additional world-space offset applied after resolving <see cref="VisualAnchor"/>.</summary>
    public Vector2 VisualAnchorOffset;

    /// <summary>Horizontal visual-local displacement supplied to the semantic anchor resolver.</summary>
    public float VisualAnchorLateralOffset;

    /// <summary>Time elapsed since this emitter was created.</summary>
    public TimeSpan Age;

    /// <summary>Emission accumulator for sub-tick emission rates.</summary>
    public float EmitAccum;

    /// <summary>True once the emitter stops producing new particles. Existing particles live out their lifetimes.</summary>
    public bool Exhausted;

    /// <summary>
    /// Unique client-side handle for addressing this emitter by ID.
    /// Prefer holding the <see cref="ActiveEmitter"/> reference directly when possible.
    /// </summary>
    public uint Handle;

    /// <summary>Color tint multiplied on top of every particle's computed color.</summary>
    public Color? ColorOverride;

    /// <summary>
    /// Intensity multiplier for emission rate, particle size, and live-particle capacity.
    /// Runtime clamps it to the supported range.
    /// </summary>
    public float Intensity = 1f;

    /// <summary>
    /// Live overrides shadowing individual prototype fields.
    /// Non-null values take priority, null falls back to the prototype.
    /// </summary>
    public ParticleRuntimeOverrides? Overrides;

    // =^..^= Velocity tracking =^..^=

    /// <summary>World position sampled on the previous frame.</summary>
    public Vector2 PreviousPosition;

    /// <summary>Map containing <see cref="PreviousPosition"/>.</summary>
    public MapId PreviousMapId;

    /// <summary>Measured or physics-seeded emitter velocity in map space.</summary>
    public Vector2 EmitterVelocity;

    /// <summary>Whether <see cref="PreviousPosition"/> contains a valid sample.</summary>
    public bool VelocityInitialized;

    // =^..^= Aim-at targeting =^..^=

    /// <summary>
    /// When set, each tick the emit angle is recomputed to point toward this entity.
    /// Falls back to <see cref="TargetPosition"/> if the entity is deleted.
    /// </summary>
    public EntityUid? TargetEntity;

    /// <summary>
    /// When set, the emit angle points toward this world position.
    /// Used as a fallback when <see cref="TargetEntity"/> is unset or gone.
    /// </summary>
    public Vector2? TargetPosition;

    /// <summary>Resolved emit angle in radians, recomputed each tick from the target if one is set.</summary>
    public float EffectiveEmitAngle;

    // =^..^= Timed bursts =^..^=

    /// <summary>Tracks which <see cref="ParticleEffectPrototype.Bursts"/> entries have already fired.</summary>
    public readonly List<bool> FiredBursts = new();

    // =^..^= Animation =^..^=

    /// <summary>Resolved RSI frames. Populated on creation.
    /// Single-frame sprites have one entry and empty Delays.</summary>
    public Texture[] Frames = Array.Empty<Texture>();

    /// <summary>frame delays when an RSI defines animation.</summary>
    public float[] Delays = Array.Empty<float>();

    public int AnimFrame;
    public float AnimTimer;

    // =^..^= Particles =^..^=

    /// <summary>
    /// Dense collection of live particles. Dead entries are removed with swap-removal and returned
    /// to the global pool owned by <see cref="ParticleSystem"/>.
    /// </summary>
    public readonly List<ParticleData> Particles = new();
}

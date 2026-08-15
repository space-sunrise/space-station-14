using System.Numerics;

namespace Content.Shared._Sunrise.Particles;

/// <summary>
/// Per-emitter runtime overrides for <see cref="ParticleEffectPrototype"/> fields.
/// Every field is nullable — null means "use the prototype value". Only set what you need to change.
/// </summary>
public sealed class ParticleRuntimeOverrides
{
    /// <summary>
    /// Creates an empty runtime override set.
    /// </summary>
    public ParticleRuntimeOverrides()
    {
    }

    /// <summary>
    /// Creates an independent copy of another runtime override set.
    /// </summary>
    public ParticleRuntimeOverrides(ParticleRuntimeOverrides source)
    {
        Merge(source);
    }

    /// <summary>
    /// Applies every non-null field from <paramref name="source"/>.
    /// </summary>
    public void Merge(ParticleRuntimeOverrides source)
    {
        StartColor = source.StartColor ?? StartColor;
        EndColor = source.EndColor ?? EndColor;
        ColorOverride = source.ColorOverride ?? ColorOverride;
        Shader = source.Shader ?? Shader;
        RenderLayer = source.RenderLayer ?? RenderLayer;
        ParticleSize = source.ParticleSize ?? ParticleSize;
        ParticleScale = source.ParticleScale ?? ParticleScale;
        SizeVariance = source.SizeVariance ?? SizeVariance;
        StretchFactor = source.StretchFactor ?? StretchFactor;
        Lifetime = source.Lifetime ?? Lifetime;
        LifetimeVariance = source.LifetimeVariance ?? LifetimeVariance;
        Speed = source.Speed ?? Speed;
        SpeedVariance = source.SpeedVariance ?? SpeedVariance;
        ConstantForce = source.ConstantForce ?? ConstantForce;
        Gravity = source.Gravity ?? Gravity;
        Drag = source.Drag ?? Drag;
        TerminalSpeed = source.TerminalSpeed ?? TerminalSpeed;
        NoiseStrength = source.NoiseStrength ?? NoiseStrength;
        NoiseFrequency = source.NoiseFrequency ?? NoiseFrequency;
        InheritVelocity = source.InheritVelocity ?? InheritVelocity;
        StartRotation = source.StartRotation ?? StartRotation;
        StartRotationVariance = source.StartRotationVariance ?? StartRotationVariance;
        RotationSpeed = source.RotationSpeed ?? RotationSpeed;
        RotationSpeedVariance = source.RotationSpeedVariance ?? RotationSpeedVariance;
        EmissionRate = source.EmissionRate ?? EmissionRate;
        MaxCount = source.MaxCount ?? MaxCount;
        Duration = source.Duration ?? Duration;
        SpreadAngle = source.SpreadAngle ?? SpreadAngle;
        EmitAngle = source.EmitAngle ?? EmitAngle;
        EmissionShape = source.EmissionShape ?? EmissionShape;
        EmissionRadius = source.EmissionRadius ?? EmissionRadius;
        EmissionBoxExtents = source.EmissionBoxExtents ?? EmissionBoxExtents;
        EmissionLineStart = source.EmissionLineStart ?? EmissionLineStart;
        EmissionLineEnd = source.EmissionLineEnd ?? EmissionLineEnd;
        EmissionTriangleLength = source.EmissionTriangleLength ?? EmissionTriangleLength;
        EmissionTriangleHalfWidth = source.EmissionTriangleHalfWidth ?? EmissionTriangleHalfWidth;
        SpawnOffset = source.SpawnOffset ?? SpawnOffset;
    }

    #region =^..^= Visuals =^..^=

    /// <summary>Particle color at the start of its lifetime.</summary>
    public Color? StartColor;

    /// <summary>Particle color at the end of its lifetime.</summary>
    public Color? EndColor;

    /// <summary>Global tint multiplied on top of every particle's color.</summary>
    public Color? ColorOverride;

    /// <summary>Shader used to draw this emitter's particles.</summary>
    public string? Shader;

    /// <summary>Relative render layer used to sort particle emitters.</summary>
    public int? RenderLayer;
    #endregion
    #region =^..^= Size =^..^=

    /// <summary>Base size of each particle.</summary>
    public float? ParticleSize;

    /// <summary>Независимый масштаб ширины и высоты частицы.</summary>
    public Vector2? ParticleScale;

    /// <summary>Symmetric random variance applied to particle size at spawn.</summary>
    public float? SizeVariance;

    /// <summary>Amount by which velocity stretches a particle into a streak.</summary>
    public float? StretchFactor;

    #endregion
    #region  =^..^= Lifetime =^..^=

    /// <summary>Lifetime of each particle.</summary>
    public TimeSpan? Lifetime;

    /// <summary>Symmetric random variance applied to particle lifetime at spawn.</summary>
    public TimeSpan? LifetimeVariance;

    #endregion
    #region =^..^= Movement =^..^=

    /// <summary>Initial speed of each particle.</summary>
    public float? Speed;

    /// <summary>Symmetric random variance applied to initial speed.</summary>
    public float? SpeedVariance;

    /// <summary>Constant screen-space acceleration, where X is right and Y is up.</summary>
    public Vector2? ConstantForce;

    /// <summary>Downward screen-space drift; negative values drift upward.</summary>
    public float? Gravity;

    /// <summary>Exponential drag coefficient. Zero disables drag.</summary>
    public float? Drag;

    /// <summary>Maximum particle speed. Zero disables the cap.</summary>
    public float? TerminalSpeed;

    /// <summary>Strength of value-noise turbulence applied to particle position.</summary>
    public float? NoiseStrength;

    /// <summary>Animation speed of the value-noise field.</summary>
    public float? NoiseFrequency;

    /// <summary>Fraction of the emitter velocity inherited by new particles.</summary>
    public float? InheritVelocity;

    #endregion
    #region =^..^= Rotation =^..^=

    /// <summary>Initial particle rotation.</summary>
    public Angle? StartRotation;

    /// <summary>Symmetric random variance applied to initial rotation.</summary>
    public Angle? StartRotationVariance;

    /// <summary>Particle spin rate.</summary>
    public Angle? RotationSpeed;

    /// <summary>Symmetric random variance applied to particle spin rate.</summary>
    public Angle? RotationSpeedVariance;

    #endregion
    #region =^..^= Emission =^..^=

    /// <summary>Number of particles emitted per second while the emitter is active.</summary>
    public float? EmissionRate;

    /// <summary>Maximum number of live particles owned by this emitter.</summary>
    public int? MaxCount;

    /// <summary>How long the emitter produces particles before stopping.</summary>
    public TimeSpan? Duration;

    /// <summary>Total angle of the emission cone.</summary>
    public Angle? SpreadAngle;

    /// <summary>Base emission direction, where zero points screen-up.</summary>
    public Angle? EmitAngle;

    #endregion
    #region =^..^= Emission shape =^..^=

    /// <summary>Shape used to distribute new particles around the emitter origin.</summary>
    public EmissionShapeType? EmissionShape;

    /// <summary>Runtime radius for circle emission shapes.</summary>
    public float? EmissionRadius;

    /// <summary>Runtime half-extents for a box emission shape.</summary>
    public Vector2? EmissionBoxExtents;

    /// <summary>Runtime line start in emission-local coordinates.</summary>
    public Vector2? EmissionLineStart;

    /// <summary>Runtime line end in emission-local coordinates.</summary>
    public Vector2? EmissionLineEnd;

    /// <summary>Runtime distance from a triangle apex to its base.</summary>
    public float? EmissionTriangleLength;

    /// <summary>Runtime half-width of a triangle base.</summary>
    public float? EmissionTriangleHalfWidth;

    #endregion
    #region =^..^= Spawn position =^..^=

    /// <summary>World-space offset from the emitter origin where particles are spawned.</summary>
    public Vector2? SpawnOffset;

    #endregion
}

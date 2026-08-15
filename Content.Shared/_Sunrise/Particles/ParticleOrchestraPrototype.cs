using System.Numerics;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared._Sunrise.Particles;

/// <summary>
/// Selects the world-space direction supplied to a particle layer by an orchestra context.
/// </summary>
public enum ParticleOrchestraDirection : byte
{
    Prototype,
    Movement,
    OppositeMovement,
    SourceToPosition,
    PositionToSource,
    SourceToTarget,
    TargetToSource,
    SourceFacing,
}

/// <summary>
/// One independently scalable layer of a composed particle effect.
/// </summary>
[DataDefinition]
public sealed partial class ParticleOrchestraLayerData
{
    /// <summary>Particle effect spawned by this layer.</summary>
    [DataField(required: true)]
    public ProtoId<ParticleEffectPrototype> Effect;

    /// <summary>Delay after the orchestra starts.</summary>
    [DataField]
    public TimeSpan Delay;

    /// <summary>Additional LOD threshold applied on top of the particle prototype threshold.</summary>
    [DataField]
    public ParticleQualityLevel MinimumQuality;

    /// <summary>Optional semantic anchor on the source entity.</summary>
    [DataField]
    public ParticleVisualAnchor? Anchor;

    /// <summary>Whether the emitter follows the source entity after it is created.</summary>
    [DataField]
    public bool Attach = true;

    /// <summary>Whether the emission box should be derived from the source sprite bounds.</summary>
    [DataField]
    public bool FillSourceSprite;

    /// <summary>Fraction of the source sprite bounds covered by a sprite-filling emission box.</summary>
    [DataField]
    public float SourceSpriteCoverage = 0.85f;

    /// <summary>Additional world-space offset from the context position or resolved anchor.</summary>
    [DataField]
    public Vector2 Offset;

    /// <summary>Horizontal visual-local displacement passed to the semantic anchor.</summary>
    [DataField]
    public float LateralOffset;

    /// <summary>Context direction used to override the effect's emission angle.</summary>
    [DataField]
    public ParticleOrchestraDirection Direction;

    /// <summary>Optional emission-cone override for this layer.</summary>
    [DataField]
    public Angle? SpreadAngle;

    /// <summary>Layer intensity multiplied by the orchestra invocation intensity.</summary>
    [DataField]
    public float Intensity = 1f;

    /// <summary>Optional tint applied before the orchestra invocation tint.</summary>
    [DataField]
    public Color? ColorOverride;

    /// <summary>Optional source of an entity-dependent tint for this layer.</summary>
    [DataField]
    public ParticleVisualColorSource ColorSource;

    /// <summary>Tint used when <see cref="ColorSource"/> cannot be resolved.</summary>
    [DataField]
    public Color? FallbackColor;

    /// <summary>Optional source-facing directions in which this layer is visible.</summary>
    [DataField]
    public HashSet<Direction>? AllowedFacings;
}

/// <summary>
/// Describes a semantic visual event composed from independently scalable particle layers.
/// </summary>
[Prototype]
public sealed partial class ParticleOrchestraPrototype : IPrototype, IInheritingPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<ParticleOrchestraPrototype>))]
    public string[]? Parents { get; private set; }

    [NeverPushInheritance, AbstractDataField]
    public bool Abstract { get; private set; }

    /// <summary>Layers spawned by this visual event.</summary>
    [DataField(required: true)]
    public List<ParticleOrchestraLayerData> Layers { get; private set; } = [];
}

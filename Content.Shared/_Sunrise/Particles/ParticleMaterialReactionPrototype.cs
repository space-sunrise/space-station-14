using Content.Shared.Damage.Prototypes;
using Content.Shared.Materials;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Particles;

/// <summary>
/// Defines how a surface material is visualized when it receives different kinds of damage.
/// </summary>
[Prototype]
public sealed partial class ParticleMaterialReactionPrototype : IPrototype
{
    /// <inheritdoc />
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>Physical materials resolved to this reaction when no explicit material is configured.</summary>
    [DataField]
    public HashSet<ProtoId<MaterialPrototype>> Materials { get; private set; } = [];

    /// <summary>Damage modifier sets resolved to this reaction when physical composition is unavailable.</summary>
    [DataField]
    public HashSet<ProtoId<DamageModifierSetPrototype>> DamageModifiers { get; private set; } = [];

    /// <summary>Orchestra used for blunt, piercing, slashing, and other mechanical impacts.</summary>
    [DataField]
    public ProtoId<ParticleOrchestraPrototype>? KineticImpactOrchestra { get; private set; }

    /// <summary>Orchestra used for laser, plasma, and other heat-dominant impacts.</summary>
    [DataField]
    public ProtoId<ParticleOrchestraPrototype>? EnergyImpactOrchestra { get; private set; }

    /// <summary>Orchestra used for electrical damage.</summary>
    [DataField]
    public ProtoId<ParticleOrchestraPrototype>? ElectricalImpactOrchestra { get; private set; }

    /// <summary>Orchestra used when an entity made from this material is destroyed.</summary>
    [DataField]
    public ProtoId<ParticleOrchestraPrototype>? DestructionOrchestra { get; private set; }
}

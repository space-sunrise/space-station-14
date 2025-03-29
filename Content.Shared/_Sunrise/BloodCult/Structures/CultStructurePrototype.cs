using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Utility;

namespace Content.Shared._Sunrise.BloodCult.Structures;

[Prototype("cultStructure")]
public sealed class CultStructurePrototype : IPrototype
{
    [DataField("structureId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>), required: true)]
    public string StructureId = string.Empty;

    [DataField("name", required: true)]
    public string StructureName = string.Empty;

    [DataField("icon", required: true)]
    public SpriteSpecifier Icon { get; } = default!;

    [IdDataField]
    public string ID { get; } = default!;

    [DataField( required: true)]
    public BloodCultType? CultType;
}

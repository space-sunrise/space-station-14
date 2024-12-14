using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._Sunrise.BloodCult.Structures;

[Prototype("cultStructure")]
public sealed class CultStructurePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [DataField("name", required:true)]
    public string StructureName = string.Empty;

    [DataField("structureId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>), required: true)]
    public string StructureId = string.Empty;

    [DataField("icon", required: true)]
    public string Icon { get; } = default!;
}

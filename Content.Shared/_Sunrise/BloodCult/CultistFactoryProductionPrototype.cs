using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Sunrise.BloodCult;

[Prototype("cultistFactoryProduction")]
public sealed class CultistFactoryProductionPrototype : IPrototype
{
    [DataField("icon", required: true)]
    public SpriteSpecifier? Icon;

    [DataField("item", required: true)]
    public List<string> Item = default!;

    [DataField("name", required: true)]
    public string Name = default!;

    [IdDataField]
    public string ID { get; } = default!;
}

[Serializable, NetSerializable]
public enum CultCraftStructureVisuals : byte
{
    Activated
}

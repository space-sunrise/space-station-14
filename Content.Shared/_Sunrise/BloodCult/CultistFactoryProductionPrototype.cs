using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Sunrise.BloodCult;

[Prototype("cultistFactoryProduction")]
public sealed class CultistFactoryProductionPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [DataField("item", required: true)]
    public List<string> Item = default!;

    [DataField("icon", required: true)]
    public SpriteSpecifier? Icon;

    [DataField("name", required: true)]
    public string Name = default!;
}

[Serializable, NetSerializable]
public enum CultCraftStructureVisuals : byte
{
    Activated
}

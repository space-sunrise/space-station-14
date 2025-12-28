using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.BloodCult;

[Prototype]
public sealed partial class BloodCultPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = null!;

    [DataField(required: true)]
    public BloodCultType? CultType;

    [DataField(required: true)]
    public EntProtoId DaggerProto;

    [DataField(required: true)]
    public EntProtoId BladeProto;

    [DataField(required: true)]
    public EntProtoId HalberdProto;

    [DataField(required: true)]
    public EntProtoId GodProto;

    [DataField(required: true)]
    public List<EntProtoId> StartingItems = [];
}

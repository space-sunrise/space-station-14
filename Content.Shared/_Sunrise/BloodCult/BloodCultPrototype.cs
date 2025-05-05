using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.BloodCult;

[Prototype("bloodCult")]
public sealed class BloodCultPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = null!;

    [DataField( required: true)]
    public BloodCultType? CultType;

    [DataField(required: true)]
    public EntProtoId RitualDaggerProto;

    [DataField(required: true)]
    public EntProtoId CultBladeProto;

    [DataField(required: true)]
    public EntProtoId GodProto;

    [DataField(required: true)]
    public List<EntProtoId> StartingItems = [];
}

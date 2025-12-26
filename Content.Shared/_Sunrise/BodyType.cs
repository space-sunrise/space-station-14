using Content.Shared.Humanoid;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise;

[Prototype("bodyType")]
public sealed class BodyTypePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [DataField(required: true)]
    public string Name { get; } = default!;

    [DataField(required: true)]
    public Dictionary<HumanoidVisualLayers, string> Sprites = new();

    [DataField]
    public List<string> SexRestrictions = new();
}

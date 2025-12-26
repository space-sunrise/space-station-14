using Content.Shared.Humanoid;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise;

[Prototype]
public sealed partial class BodyTypePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public string Name = default!;

    [DataField(required: true)]
    public Dictionary<HumanoidVisualLayers, string> Sprites = new();

    [DataField]
    public List<string> SexRestrictions = new();
}

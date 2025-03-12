using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Sunrise.Pets.Prototypes;

[Prototype]
public sealed partial class PetControlPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public string Name = "Unknown";

    [DataField(required: true)]
    public PetBaseEvent Event = default!;

    [DataField(required: true)]
    public SpriteSpecifier Sprite = default!;
}

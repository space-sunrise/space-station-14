using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Sunrise.Pets.Prototypes;

[Prototype]
public sealed class PetControlCategoryPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public string TooltipBase = "pet-control-category-";

    [DataField(required: true)]
    public SpriteSpecifier Sprite = default!;
}

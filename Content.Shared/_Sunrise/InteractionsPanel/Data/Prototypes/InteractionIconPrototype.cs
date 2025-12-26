using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Sunrise.InteractionsPanel.Data.Prototypes;

[Prototype]
public sealed partial class InteractionIconPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public string Name { get; private set; } = string.Empty;

    [DataField]
    public SpriteSpecifier Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/examine-star.png"));
}

using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Sunrise.CopyMachine;

[Prototype]
public sealed partial class DocTemplatePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public LocId Name;

    [DataField(required: true)]
    public ResPath Content;

    [DataField]
    public SpriteSpecifier? Header;

    [DataField(required: true)]
    public string Component { get; private set; } = default!;
    [DataField]
    public bool IsPublic { get; private set; } = true;
}

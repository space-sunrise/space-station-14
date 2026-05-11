using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Sunrise.CopyMachine;

[Prototype]
public sealed partial class DocTemplateCategoryPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name { get; private set; } = default!;

    [DataField]
    public SpriteSpecifier? Icon { get; private set; }

    [DataField]
    public int Order { get; private set; }
}

[Prototype]
public sealed partial class DocTemplateCategoryGroupPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public List<ProtoId<DocTemplateCategoryPrototype>> Categories = new();
}

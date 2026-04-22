using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared._Sunrise.CopyMachine;

[Prototype]
public sealed partial class DocTemplatePrototype : IPrototype, IInheritingPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<DocTemplatePrototype>))]
    public string[]? Parents { get; private set; }

    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; private set; }


    [DataField(required: true)]
    public ResPath Content;


    [DataField(required: true)]
    public ProtoId<DocTemplateCategoryPrototype> Category { get; private set; } = default!;

    [DataField]
    public SpriteSpecifier? Header;

    [DataField]
    public LocId Name;

    [DataField]
    public bool IsPublic { get; private set; } = true;
}

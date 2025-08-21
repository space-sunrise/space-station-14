using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared._Sunrise.CopyMachine;

[Prototype]
public sealed partial class DocTemplatePrototype : IPrototype, IInheritingPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<DocTemplatePrototype>))]
    public string[]? Parents { get; private set; }

    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; private set; }

    [DataField] public LocId Name;

    [DataField(required: true)] public ResPath Content;

    [DataField] public SpriteSpecifier? Header;

    [DataField(required: true)] public string Component { get; private set; } = default!;

    [DataField] public bool IsPublic { get; private set; } = true;
}

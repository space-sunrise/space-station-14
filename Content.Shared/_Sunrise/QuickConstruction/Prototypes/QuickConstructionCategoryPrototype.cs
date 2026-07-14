using Content.Shared.Construction.Prototypes;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using Robust.Shared.Utility;

namespace Content.Shared._Sunrise.QuickConstruction.Prototypes;

[Prototype]
public sealed partial class QuickConstructionCategoryPrototype : IPrototype, IInheritingPrototype
{
    /// <inheritdoc />
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<QuickConstructionCategoryPrototype>))]
    public string[]? Parents { get; private set; }

    /// <inheritdoc />
    [NeverPushInheritance, AbstractDataField]
    public bool Abstract { get; private set; }

    /// <inheritdoc />
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public LocId Name = string.Empty;

    [DataField]
    public SpriteSpecifier? Icon;

    [DataField]
    public List<ProtoId<ConstructionPrototype>> ConstructionEntries = [];

    [DataField]
    public List<ProtoId<QuickConstructionCategoryPrototype>> CategoryEntries = [];
}

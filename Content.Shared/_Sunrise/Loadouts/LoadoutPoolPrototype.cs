using Content.Shared.Preferences.Loadouts;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared._Sunrise.Loadouts;

[Prototype]
public sealed partial class LoadoutPoolPrototype : IPrototype, IInheritingPrototype
{
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<LoadoutPoolPrototype>))]
    public string[]? Parents { get; private set; }

    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; private set; }

    [IdDataField]
    public string ID { get; private set; } = default!;

    [AlwaysPushInheritance]
    [DataField(required: true)]
    public Dictionary<ProtoId<RoleLoadoutPrototype>, ProtoId<RoleLoadoutPrototype>> RoleLoadouts { get; private set; } = [];
}

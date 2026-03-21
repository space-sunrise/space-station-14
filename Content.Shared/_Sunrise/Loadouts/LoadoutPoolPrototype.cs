using Content.Shared.Preferences.Loadouts;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Loadouts;

[Prototype]
public sealed partial class LoadoutPoolPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public Dictionary<ProtoId<RoleLoadoutPrototype>, ProtoId<RoleLoadoutPrototype>> RoleLoadouts = new();
}

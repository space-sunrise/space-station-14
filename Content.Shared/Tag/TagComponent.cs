using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Tag;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(TagSystem))]
public sealed partial class TagComponent : Component
{
    [DataField, AutoNetworkedField]
    [Access(typeof(TagSystem), Other = AccessPermissions.ReadExecute)]
    public HashSet<ProtoId<TagPrototype>> Tags = new();
}

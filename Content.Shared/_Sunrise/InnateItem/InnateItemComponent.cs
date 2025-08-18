using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.InnateItem;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]

public sealed partial class InnateItemComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<EntProtoId?> InstantActions = new();

    [DataField, AutoNetworkedField]
    public List<EntProtoId?> WorldTargetActions = new();

    [DataField, AutoNetworkedField]
    public List<EntityUid> Actions = new();
}

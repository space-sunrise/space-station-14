using Content.Shared.Actions.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.AddWantedStatus;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AddWantedStatusComponent : Component
{
    [DataField]
    public EntProtoId<TargetActionComponent> Action = "ActionAddWanted";

    [AutoNetworkedField, ViewVariables]
    public EntityUid? ActionEntity;
}


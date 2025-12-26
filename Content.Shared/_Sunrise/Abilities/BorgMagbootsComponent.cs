using Content.Shared.Actions;
using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Abilities;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedBorgMagbootsSystem))]
public sealed partial class BorgMagbootsComponent : Component
{
    [DataField]
    public EntProtoId ToggleAction = "ActionToggleBorgMagboots";

    [DataField, AutoNetworkedField]
    public EntityUid? ToggleActionEntity;

    [DataField, AutoNetworkedField]
    public bool On;

    [DataField(required: true)] [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float WalkModifier = 1.0f;

    [DataField(required: true)] [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float SprintModifier = 1.0f;

    [DataField]
    public ProtoId<AlertPrototype> MagbootsAlert = "Magboots";

    [DataField]
    public bool RequiresGrid = true;
}


public sealed partial class ToggleBorgMagbootsActionEvent : InstantActionEvent
{

}

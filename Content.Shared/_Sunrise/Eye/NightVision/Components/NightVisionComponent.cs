using Content.Shared.Actions;
using Content.Shared._Sunrise.Eye.NightVision.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Eye.NightVision.Components;

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(NightVisionSystem), typeof(NightVisionDeviceSystem))]
public sealed partial class NightVisionComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("isOn"), AutoNetworkedField]
    public bool IsNightVision;

    [DataField("color", required: true)]
    public Color Color;

    [DataField]
    public bool IsToggle;

    [DataField] public EntityUid? ActionContainer;

    [Access(Other = AccessPermissions.ReadWriteExecute)]
    public bool DrawShadows = false;

    [Access(Other = AccessPermissions.ReadWriteExecute)]
    public bool GraceFrame = false;
}

public sealed partial class NightVisionToggleEvent : InstantActionEvent
{

}

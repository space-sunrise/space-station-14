using Content.Shared.Actions;
using Content.Shared._Sunrise.Eye.NightVision.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Eye.NightVision.Components;

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(NightVisionSystem))]
public sealed partial class NightVisionComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("isOn"), AutoNetworkedField]
    public bool IsNightVision;

    [DataField("color")]
    public Color NightVisionColor = Color.Green;

    [DataField]
    public bool IsToggle = false;

    [DataField] public EntityUid? ActionContainer;

    [Access(Other = AccessPermissions.ReadWriteExecute)]
    public bool DrawShadows = false;

    [Access(Other = AccessPermissions.ReadWriteExecute)]
    public bool GraceFrame = false;

    [DataField("playSounds")]
    public bool PlaySounds = true;
    public SoundSpecifier SoundOn = new SoundPathSpecifier("/Audio/_Sunrise/Items/night_vision_on.ogg");
    public SoundSpecifier SoundOff = new SoundPathSpecifier("/Audio/_Sunrise/Items/night_vision_off.ogg");
}

public sealed partial class NVInstantActionEvent : InstantActionEvent { }
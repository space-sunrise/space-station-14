using Content.Shared.Actions;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._Sunrise.Eye.NightVision.Components;


[RegisterComponent, NetworkedComponent]
public sealed partial class NVGComponent : Component
{
    [DataField] public EntProtoId<InstantActionComponent> ActionProto = "NVToggleAction";
    [DataField] public EntityUid? ActionContainer;
    
    [DataField("playSounds")]
    public bool PlaySounds = true;
    public SoundSpecifier SoundOn = new SoundPathSpecifier("/Audio/_Sunrise/Items/night_vision_on.ogg");
    public SoundSpecifier SoundOff = new SoundPathSpecifier("/Audio/_Sunrise/Items/night_vision_off.ogg");
}

[Serializable, NetSerializable]
public enum NVGVisuals : byte
{
    Light
}
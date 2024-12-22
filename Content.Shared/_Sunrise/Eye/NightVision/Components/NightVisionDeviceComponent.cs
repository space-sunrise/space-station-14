using Content.Shared.Inventory;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._Sunrise.Eye.NightVision.Components;


[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class NightVisionDeviceComponent : Component
{
    [DataField("toggleAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ToggleAction = "NVDToggleAction";

    [DataField("toggleActionEntity")]
    public EntityUid? ToggleActionEntity;

    [DataField("requiredSlot"), AutoNetworkedField]
    public SlotFlags RequiredFlags = SlotFlags.EYES;

    [DataField("isPowered"), AutoNetworkedField]
    public bool IsPowered;

    [DataField]
    [AutoNetworkedField]
    public bool Activated;

    [DataField(required: true)]
    [AutoNetworkedField]
    public Color? DisplayColor;

    [DataField(required: true)]
    [AutoNetworkedField]
    public string DisplayShader;

    public SoundSpecifier TurnOnSound = new SoundPathSpecifier("/Audio/_Sunrise/Items/night_vision_on.ogg");

    public SoundSpecifier TurnOffSound = new SoundPathSpecifier("/Audio/_Sunrise/Items/night_vision_off.ogg");
}

[Serializable, NetSerializable]
public enum NVDVisuals : byte
{
    Light
}

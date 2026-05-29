using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Shared.Radio.Components;

public sealed partial class HeadsetComponent
{
    [DataField, AutoNetworkedField]
    public Dictionary<string, bool> EnabledChannels = new();

    [DataField, AutoNetworkedField]
    public Dictionary<string, float> ChannelVolumes = new();

    [DataField]
    public float SendChargeCost = 10f;

    [DataField]
    public float ReceiveChargeCost = 2f;

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ToggleAction = "ActionToggleHeadset";

    [DataField, AutoNetworkedField]
    public EntityUid? ToggleActionEntity;
}

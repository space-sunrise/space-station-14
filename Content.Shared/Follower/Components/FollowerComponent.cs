using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Follower.Components;

[RegisterComponent]
[Access(typeof(FollowerSystem))]
[NetworkedComponent, AutoGenerateComponentState(RaiseAfterAutoHandleState = true)]
public sealed partial class FollowerComponent : Component
{
    [AutoNetworkedField, DataField("following")]
    public EntityUid Following;

    [DataField("stopFollowAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string StopFollowAction = "StopFollowAction";

    [DataField, AutoNetworkedField]
    public EntityUid? StopFollowActionEntity;
}

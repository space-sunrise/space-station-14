using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._Sunrise.Eye.NightVision.Components;


[RegisterComponent, NetworkedComponent]
public sealed partial class NVGComponent : Component
{
    [DataField] public EntProtoId<InstantActionComponent> ActionProto = "NVToggleAction";
    [DataField] public EntityUid? ActionContainer;
}
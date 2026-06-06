using Content.Shared.DoAfter;
using Robust.Shared.Serialization;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Mech;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MechUpgradeKitComponent : Component
{
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(3);

    [DataField(required: true)]
    [AutoNetworkedField]
    public List<EntProtoId> TargetPrototypes = new();

    [DataField(required: true)]
    [AutoNetworkedField]
    public EntProtoId ResultPrototype = default!;
}

[Serializable, NetSerializable]
public sealed partial class MechUpgradeKitDoAfterEvent : SimpleDoAfterEvent
{
}

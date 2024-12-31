using Robust.Shared.Containers;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Interrogator;

[RegisterComponent]
public sealed partial class InterrogatorComponent : Component
{
    [ViewVariables]
    public ContainerSlot BodyContainer = default!;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ExtractionTime = 30f;

    [ViewVariables(VVAccess.ReadWrite)]
    public InterrogatorStatus Status;
}

[Serializable, NetSerializable]
public enum InterrogatorVisuals : byte
{
    Status
}

[Serializable, NetSerializable]
public enum InterrogatorStatus : byte
{
    Open,
    Off,
    On,
}

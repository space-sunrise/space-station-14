using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Standing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StandingStateComponent : Component
{
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier DownSound { get; set; } = new SoundCollectionSpecifier("BodyFall");

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadOnly)]
    public StandingState CurrentState = StandingState.Standing;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadOnly)]
    public List<string> ChangedFixtures = new();

    [DataField, AutoNetworkedField]
    public float CycleTime { get; set; } = 1f;

    [DataField, AutoNetworkedField]
    public float BaseSpeedModify { get; set; } = 0.4f;
}

[Serializable, NetSerializable]
public enum StandingState : byte
{
    Standing = 0,
    Laying = 1
}

using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Sunrise.Tracker;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(TrackerSystem))]
public sealed partial class TrackerComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan UpdateAt;

    [DataField, AutoNetworkedField]
    public TimeSpan UpdateEvery = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public HashSet<string> TrackedComponents = new();

    [DataField, AutoNetworkedField]
    public string? DefaultMode;

    [DataField, AutoNetworkedField]
    public string? Mode;

    [DataField, AutoNetworkedField]
    public EntityUid? Target;

    [DataField, AutoNetworkedField]
    public ProtoId<AlertPrototype> Alert = "TrackerAlert";
}

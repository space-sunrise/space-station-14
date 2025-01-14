using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.CritHeartbeat;

[RegisterComponent, NetworkedComponent]
public sealed partial class CritHeartbeatComponent : Component
{
    [DataField]
    public SoundSpecifier HeartbeatSound = new SoundPathSpecifier("/Audio/_Sunrise/Effects/heartbeat.ogg");

    public EntityUid? AudioStream;
}

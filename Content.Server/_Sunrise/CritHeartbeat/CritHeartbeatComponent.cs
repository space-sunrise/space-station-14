using Robust.Shared.Audio;

namespace Content.Server._Sunrise.CritHeartbeat;

[RegisterComponent]
public sealed partial class CritHeartbeatComponent : Component
{
    [DataField]
    public SoundSpecifier HeartbeatSound = new SoundPathSpecifier("/Audio/_Sunrise/Effects/heartbeat.ogg");

    public EntityUid? AudioStream;
}

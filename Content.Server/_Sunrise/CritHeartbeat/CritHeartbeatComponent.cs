using Robust.Shared.Audio;

namespace Content.Server._Sunrise.CritHeartbeat;

[RegisterComponent]
public sealed partial class CritHeartbeatComponent : Component
{
    [DataField]
    public SoundSpecifier HeartbeatSound = new SoundPathSpecifier("/Audio/_Sunrise/Effects/heartbeat.ogg");

    /// <summary>
    /// Чтобы выключать это для наследников в прототипах
    /// </summary>
    [DataField]
    public bool Enabled = true;

    public EntityUid? AudioStream;
}

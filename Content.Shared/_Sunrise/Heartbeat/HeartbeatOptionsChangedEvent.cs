using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Heartbeat;

[Serializable, NetSerializable]
public sealed class HeartbeatOptionsChangedEvent(bool enabled) : EntityEventArgs
{
    public bool Enabled { get; } = enabled;
}

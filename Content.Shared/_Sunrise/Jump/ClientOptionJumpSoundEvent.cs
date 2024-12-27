using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Jump;

[Serializable, NetSerializable]
public sealed class ClientOptionJumpSoundEvent : EntityEventArgs
{
    public bool Enabled { get; }
    public ClientOptionJumpSoundEvent(bool enabled)
    {
        Enabled = enabled;
    }
}

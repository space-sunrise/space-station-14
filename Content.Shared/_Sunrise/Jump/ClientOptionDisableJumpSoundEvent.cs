using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Jump;

[Serializable, NetSerializable]
public sealed class ClientOptionDisableJumpSoundEvent : EntityEventArgs
{
    public bool Disable { get; }
    public ClientOptionDisableJumpSoundEvent(bool enabled)
    {
        Disable = enabled;
    }
}

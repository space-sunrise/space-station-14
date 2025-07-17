using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Jump;

[Serializable, NetSerializable]
public sealed class ClientOptionDisableJumpSoundEvent(bool enabled) : EntityEventArgs
{
    public bool Disable { get; } = enabled; // крутой нейминг придумали
}

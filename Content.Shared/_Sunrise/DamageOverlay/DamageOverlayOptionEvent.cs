using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.DamageOverlay;

[Serializable, NetSerializable]
public sealed class DamageOverlayOptionEvent : EntityEventArgs
{
    public bool Enabled { get; }
    public DamageOverlayOptionEvent(bool enabled)
    {
        Enabled = enabled;
    }
}

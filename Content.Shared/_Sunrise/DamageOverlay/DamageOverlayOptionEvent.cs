using Robust.Shared.Prototypes;
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

[Serializable, NetSerializable]
public sealed class DamageOverlayPresetChangedEvent : EntityEventArgs
{
    public ProtoId<DamageOverlayPrototype> Preset { get; }
    public DamageOverlayPresetChangedEvent(ProtoId<DamageOverlayPrototype> preset)
    {
        Preset = preset;
    }
}

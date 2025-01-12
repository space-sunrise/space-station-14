using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.DamageOverlay;

[Serializable, NetSerializable]
public sealed class DamageOverlayOptionEvent(bool enabled, bool selfEnabled, bool structuresEnabled) : EntityEventArgs
{
    public bool Enabled { get; } = enabled;
    public bool SelfEnabled { get; } = selfEnabled;
    public bool StructuresEnabled { get; } = structuresEnabled;
}

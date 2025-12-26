using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Switchable;

// Appearance Data key
[Serializable, NetSerializable]
public enum SwitchableLightVisuals : byte
{
    Enabled,
    Color
}

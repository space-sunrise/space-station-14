using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.HairDye;

[Serializable, NetSerializable]
public sealed partial class HairDyeDoAfterEvent : SimpleDoAfterEvent
{
    public Color TargetColor;
}

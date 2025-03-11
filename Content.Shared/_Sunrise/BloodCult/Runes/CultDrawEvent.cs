using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.BloodCult.Runes;

[Serializable, NetSerializable]
public sealed partial class CultDrawEvent : SimpleDoAfterEvent
{
    public string? Rune;
}

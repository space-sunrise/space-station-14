using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.BloodCult.Actions;

[Serializable, NetSerializable]
public sealed partial class CultMagicBloodCallEvent : SimpleDoAfterEvent
{
    public string? ActionId;
    public float BloodTake;
}

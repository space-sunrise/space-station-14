using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.SiliconStanding;

[Serializable, NetSerializable]
public sealed class ToggleStandingEvent : EntityEventArgs
{
}

[ByRefEvent]
public record struct SiliconRestToggleAttemptEvent(bool Resting, bool Cancelled = false);

[ByRefEvent]
public record struct GetSiliconRestDelayEvent(bool Resting, float Delay);

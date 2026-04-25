namespace Content.Shared._Sunrise.Standing;

[ByRefEvent]
public record struct FallAttemptEvent(bool Cancelled = false);

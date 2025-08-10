namespace Content.Shared.Standing;

[ByRefEvent]
public record struct FallAttemptEvent(bool Cancelled = false);

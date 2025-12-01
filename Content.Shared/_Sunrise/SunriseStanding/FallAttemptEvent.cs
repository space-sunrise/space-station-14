namespace Content.Shared._Sunrise.SunriseStanding;

[ByRefEvent]
public record struct FallAttemptEvent(bool Cancelled = false);

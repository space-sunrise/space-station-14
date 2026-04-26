using System.Numerics;

namespace Content.Shared._Sunrise.Standing;

[ByRefEvent]
public record struct FallAttemptEvent(bool Cancelled = false);

[ByRefEvent]
public record struct ProneCrawlPullStartedEvent(Vector2 Direction, TimeSpan Duration);

using System.Numerics;

namespace Content.Shared._Sunrise.Movement.Standing;

[ByRefEvent]
public struct FallAttemptEvent(bool cancelled = false)
{
    public bool Cancelled = cancelled;
}

[ByRefEvent]
public record struct ProneCrawlPullStartedEvent(Vector2 Direction, TimeSpan Duration);

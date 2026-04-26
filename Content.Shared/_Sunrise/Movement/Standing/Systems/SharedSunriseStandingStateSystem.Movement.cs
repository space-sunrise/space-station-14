using System.Numerics;
using Content.Shared._Sunrise.Movement.Standing.Components;
using Content.Shared.Standing;
using Content.Shared.Stunnable;

namespace Content.Shared._Sunrise.Movement.Standing.Systems;

public abstract partial class SharedSunriseStandingStateSystem
{
    private EntityQuery<CrawlerComponent> _crawlerQuery;

    private void InitializeProneCrawlMovement()
    {
        _crawlerQuery = GetEntityQuery<CrawlerComponent>();

        SubscribeLocalEvent<StandingStateComponent, DownedEvent>(OnProneCrawlMovementDowned);
        SubscribeLocalEvent<StandingStateComponent, StoodEvent>(OnProneCrawlMovementStood);
    }

    private void OnProneCrawlMovementDowned(Entity<StandingStateComponent> ent, ref DownedEvent args)
    {
        if (!_crawlerQuery.HasComp(ent))
            return;

        var movement = EnsureComp<ActiveProneCrawlMovementComponent>(ent.Owner);
        ResetProneCrawlMovementState((ent.Owner, movement));
    }

    private void OnProneCrawlMovementStood(Entity<StandingStateComponent> ent, ref StoodEvent args)
    {
        if (!_crawlerQuery.HasComp(ent))
            return;

        if (TryComp<ActiveProneCrawlMovementComponent>(ent, out var movement))
            ResetProneCrawlMovementState((ent.Owner, movement));

        RemCompDeferred<ActiveProneCrawlMovementComponent>(ent);
    }

    /// <summary>
    /// Fully clears prone-crawl pull state for <see cref="ProneCrawlMovementController.StopProneCrawl"/>
    /// and <see cref="ProneCrawlMovementController.SuspendProneCrawl"/>, including the next pull cooldown.
    /// Resets <see cref="ActiveProneCrawlMovementComponent.PullStartTime"/>,
    /// <see cref="ActiveProneCrawlMovementComponent.PullEndTime"/>,
    /// <see cref="ActiveProneCrawlMovementComponent.NextPullTime"/>,
    /// <see cref="ActiveProneCrawlMovementComponent.PullDirection"/>,
    /// <see cref="ActiveProneCrawlMovementComponent.PullVelocity"/> and
    /// <see cref="ActiveProneCrawlMovementComponent.IsPulling"/>.
    /// </summary>
    public void ResetProneCrawlMovementState(Entity<ActiveProneCrawlMovementComponent> ent)
    {
        Reset(ent, ref ent.Comp.PullStartTime, TimeSpan.Zero, nameof(ActiveProneCrawlMovementComponent.PullStartTime));
        Reset(ent, ref ent.Comp.PullEndTime,   TimeSpan.Zero, nameof(ActiveProneCrawlMovementComponent.PullEndTime));
        Reset(ent, ref ent.Comp.NextPullTime,  TimeSpan.Zero, nameof(ActiveProneCrawlMovementComponent.NextPullTime));
        Reset(ent, ref ent.Comp.PullDirection, Vector2.Zero,  nameof(ActiveProneCrawlMovementComponent.PullDirection));
        Reset(ent, ref ent.Comp.PullVelocity,  Vector2.Zero,  nameof(ActiveProneCrawlMovementComponent.PullVelocity));
        Reset(ent, ref ent.Comp.IsPulling,     false,         nameof(ActiveProneCrawlMovementComponent.IsPulling));
    }

    /// <summary>
    /// Cancels the active prone-crawl pull for <see cref="ProneCrawlMovementController.StopProneCrawl"/>
    /// while preserving <see cref="ActiveProneCrawlMovementComponent.NextPullTime"/> as the current cooldown.
    /// Resets <see cref="ActiveProneCrawlMovementComponent.PullStartTime"/>,
    /// <see cref="ActiveProneCrawlMovementComponent.PullEndTime"/>,
    /// <see cref="ActiveProneCrawlMovementComponent.PullDirection"/>,
    /// <see cref="ActiveProneCrawlMovementComponent.PullVelocity"/> and
    /// <see cref="ActiveProneCrawlMovementComponent.IsPulling"/>.
    /// </summary>
    public void CancelProneCrawlActivePull(Entity<ActiveProneCrawlMovementComponent> ent)
    {
        Reset(ent, ref ent.Comp.PullStartTime, TimeSpan.Zero, nameof(ActiveProneCrawlMovementComponent.PullStartTime));
        Reset(ent, ref ent.Comp.PullEndTime,   TimeSpan.Zero, nameof(ActiveProneCrawlMovementComponent.PullEndTime));
        Reset(ent, ref ent.Comp.PullDirection, Vector2.Zero,  nameof(ActiveProneCrawlMovementComponent.PullDirection));
        Reset(ent, ref ent.Comp.PullVelocity,  Vector2.Zero,  nameof(ActiveProneCrawlMovementComponent.PullVelocity));
        Reset(ent, ref ent.Comp.IsPulling,     false,         nameof(ActiveProneCrawlMovementComponent.IsPulling));
    }

    private void Reset<T>(Entity<ActiveProneCrawlMovementComponent> ent, ref T field, T value, string name)
        where T : IEquatable<T>
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;

        field = value;
        DirtyField(ent.Owner, ent.Comp, name);
    }
}

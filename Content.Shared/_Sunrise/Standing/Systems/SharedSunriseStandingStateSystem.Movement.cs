using System.Numerics;
using Content.Shared._Sunrise.Standing.Components;
using Content.Shared.Standing;
using Content.Shared.Stunnable;

namespace Content.Shared._Sunrise.Standing.Systems;

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
        if (_crawlerQuery.HasComp(ent))
            RemCompDeferred<ActiveProneCrawlMovementComponent>(ent);
    }

    public void ResetProneCrawlMovementState(Entity<ActiveProneCrawlMovementComponent> ent)
    {
        var changed = false;

        if (ent.Comp.PullStartTime != TimeSpan.Zero)
        {
            ent.Comp.PullStartTime = TimeSpan.Zero;
            changed = true;
        }

        if (ent.Comp.PullEndTime != TimeSpan.Zero)
        {
            ent.Comp.PullEndTime = TimeSpan.Zero;
            changed = true;
        }

        if (ent.Comp.NextPullTime != TimeSpan.Zero)
        {
            ent.Comp.NextPullTime = TimeSpan.Zero;
            changed = true;
        }

        if (ent.Comp.PullDirection != Vector2.Zero)
        {
            ent.Comp.PullDirection = Vector2.Zero;
            changed = true;
        }

        if (ent.Comp.PullVelocity != Vector2.Zero)
        {
            ent.Comp.PullVelocity = Vector2.Zero;
            changed = true;
        }

        if (ent.Comp.IsPulling)
        {
            ent.Comp.IsPulling = false;
            changed = true;
        }

        if (changed)
            Dirty(ent);
    }

    public void CancelProneCrawlActivePull(Entity<ActiveProneCrawlMovementComponent> ent)
    {
        var changed = false;

        if (ent.Comp.PullStartTime != TimeSpan.Zero)
        {
            ent.Comp.PullStartTime = TimeSpan.Zero;
            changed = true;
        }

        if (ent.Comp.PullEndTime != TimeSpan.Zero)
        {
            ent.Comp.PullEndTime = TimeSpan.Zero;
            changed = true;
        }

        if (ent.Comp.PullDirection != Vector2.Zero)
        {
            ent.Comp.PullDirection = Vector2.Zero;
            changed = true;
        }

        if (ent.Comp.PullVelocity != Vector2.Zero)
        {
            ent.Comp.PullVelocity = Vector2.Zero;
            changed = true;
        }

        if (ent.Comp.IsPulling)
        {
            ent.Comp.IsPulling = false;
            changed = true;
        }

        if (changed)
            Dirty(ent);
    }
}

using System.Numerics;
using Content.Shared._Sunrise.Standing.Components;
using Content.Shared.Gravity;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Timing;

namespace Content.Shared._Sunrise.Standing.Systems;

public sealed class SharedProneCrawlMovementController : VirtualController
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedGravitySystem _gravity = default!;
    [Dependency] private readonly SharedSunriseStandingStateSystem _sunriseStanding = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private EntityQuery<PhysicsComponent> _physicsQuery;
    private EntityQuery<PullableComponent> _pullableQuery;
    private EntityQuery<StandingStateComponent> _standingQuery;

    public override void Initialize()
    {
        UpdatesAfter.Add(typeof(SharedMoverController));
        base.Initialize();

        _physicsQuery = GetEntityQuery<PhysicsComponent>();
        _pullableQuery = GetEntityQuery<PullableComponent>();
        _standingQuery = GetEntityQuery<StandingStateComponent>();
    }

    public override void UpdateBeforeSolve(bool prediction, float frameTime)
    {
        base.UpdateBeforeSolve(prediction, frameTime);

        var query = EntityQueryEnumerator<ActiveProneCrawlMovementComponent, CrawlerComponent, InputMoverComponent>();
        while (query.MoveNext(out var uid, out var proneMovement, out var crawler, out var mover))
        {
            if (!_physicsQuery.TryComp(uid, out var physics))
                continue;

            if (prediction && !physics.Predict)
                continue;

            UpdateProneCrawl((uid, proneMovement, crawler, mover, physics));
        }
    }

    private void UpdateProneCrawl(
        Entity<ActiveProneCrawlMovementComponent, CrawlerComponent, InputMoverComponent, PhysicsComponent> ent)
    {
        if (ent.Comp4.BodyStatus == BodyStatus.InAir)
        {
            SuspendProneCrawl(ent);
            return;
        }

        if (!CanProneCrawl(ent, out var wishDir))
        {
            StopProneCrawl(ent, resetCooldown: true);
            return;
        }

        if (wishDir == Vector2.Zero)
        {
            StopProneCrawl(ent, resetCooldown: true);
            return;
        }

        var currentTime = _timing.CurTime;

        if (ent.Comp1.IsPulling && currentTime < ent.Comp1.PullEndTime)
        {
            ApplyPullVelocity(ent);
            return;
        }

        if (ent.Comp1.IsPulling)
            FinishPull(ent);

        if (currentTime < ent.Comp1.NextPullTime)
        {
            ApplyPause(ent);
            return;
        }

        if (!TryStartPull(ent, wishDir, currentTime))
        {
            StopProneCrawl(ent, resetCooldown: true);
            return;
        }

        ApplyPullVelocity(ent);
    }

    private bool CanProneCrawl(
        Entity<ActiveProneCrawlMovementComponent, CrawlerComponent, InputMoverComponent, PhysicsComponent> ent,
        out Vector2 wishDir)
    {
        wishDir = Vector2.Zero;

        if (ent.Comp4.BodyType != BodyType.KinematicController ||
            ent.Comp4.BodyStatus != BodyStatus.OnGround)
        {
            return false;
        }

        if (!_standingQuery.TryComp(ent, out var standing) || standing.Standing)
            return false;

        if (!ent.Comp3.CanMove || _gravity.IsWeightless(ent.Owner))
            return false;

        if (_pullableQuery.TryComp(ent, out var pullable) && pullable.BeingPulled)
            return false;

        wishDir = ent.Comp3.WishDir;
        return true;
    }

    private bool TryStartPull(
        Entity<ActiveProneCrawlMovementComponent, CrawlerComponent, InputMoverComponent, PhysicsComponent> ent,
        Vector2 wishDir,
        TimeSpan currentTime)
    {
        var pullDuration = ent.Comp2.CrawlPullDuration;
        if (pullDuration <= TimeSpan.Zero)
            return false;

        var desiredSpeed = wishDir.Length();
        if (desiredSpeed <= 0.001f)
            return false;

        var pullDistance = MathF.Min(ent.Comp2.CrawlPullDistance, desiredSpeed * (float) pullDuration.TotalSeconds);
        if (pullDistance <= 0.001f)
            return false;

        var direction = wishDir.Normalized();
        var velocity = direction * (pullDistance / (float) pullDuration.TotalSeconds);

        ent.Comp1.PullStartTime = currentTime;
        ent.Comp1.PullEndTime = currentTime + pullDuration;
        ent.Comp1.NextPullTime = ent.Comp1.PullEndTime + ent.Comp2.CrawlPullPause;
        ent.Comp1.PullDirection = direction;
        ent.Comp1.PullVelocity = velocity;
        ent.Comp1.IsPulling = true;
        Dirty(ent.Owner, ent.Comp1);

        var pullStarted = new ProneCrawlPullStartedEvent(direction, pullDuration);
        RaiseLocalEvent(ent.Owner, ref pullStarted);
        PlayPullSound((ent.Owner, ent.Comp2));
        return true;
    }

    private void PlayPullSound(Entity<CrawlerComponent> ent)
    {
        var sound = ent.Comp.CrawlPullSound ?? ent.Comp.CrawlingSound;
        if (sound == null)
            return;

        _audio.PlayPredicted(sound, ent.Owner, ent.Owner);
    }

    private void ApplyPullVelocity(Entity<ActiveProneCrawlMovementComponent, CrawlerComponent, InputMoverComponent, PhysicsComponent> ent)
    {
        PhysicsSystem.SetLinearVelocity(ent.Owner, ent.Comp1.PullVelocity, body: ent.Comp4);
        PhysicsSystem.SetAngularVelocity(ent.Owner, 0f, body: ent.Comp4);
    }

    private void ApplyPause(Entity<ActiveProneCrawlMovementComponent, CrawlerComponent, InputMoverComponent, PhysicsComponent> ent)
    {
        PhysicsSystem.SetLinearVelocity(ent.Owner, Vector2.Zero, body: ent.Comp4);
        PhysicsSystem.SetAngularVelocity(ent.Owner, 0f, body: ent.Comp4);
    }

    private void FinishPull(Entity<ActiveProneCrawlMovementComponent, CrawlerComponent, InputMoverComponent, PhysicsComponent> ent)
    {
        ent.Comp1.IsPulling = false;
        ent.Comp1.PullVelocity = Vector2.Zero;
        Dirty(ent.Owner, ent.Comp1);
    }

    private void StopProneCrawl(
        Entity<ActiveProneCrawlMovementComponent, CrawlerComponent, InputMoverComponent, PhysicsComponent> ent,
        bool resetCooldown)
    {
        PhysicsSystem.SetLinearVelocity(ent.Owner, Vector2.Zero, body: ent.Comp4);
        PhysicsSystem.SetAngularVelocity(ent.Owner, 0f, body: ent.Comp4);

        if (resetCooldown)
            _sunriseStanding.ResetProneCrawlMovementState((ent.Owner, ent.Comp1));
    }

    private void SuspendProneCrawl(
        Entity<ActiveProneCrawlMovementComponent, CrawlerComponent, InputMoverComponent, PhysicsComponent> ent)
    {
        _sunriseStanding.ResetProneCrawlMovementState((ent.Owner, ent.Comp1));
    }
}

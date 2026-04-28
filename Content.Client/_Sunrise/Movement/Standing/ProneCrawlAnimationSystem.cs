using System.Numerics;
using Content.Shared._Sunrise.Movement.Standing;
using Content.Shared._Sunrise.Movement.Standing.Components;
using Content.Shared.Stunnable;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.Timing;

namespace Content.Client._Sunrise.Movement.Standing;

public sealed class ProneCrawlAnimationSystem : EntitySystem
{
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private const float PullBackPeak = 0.35f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActiveProneCrawlMovementComponent, ProneCrawlPullStartedEvent>(OnPullStarted);
        SubscribeLocalEvent<ActiveProneCrawlMovementComponent, ComponentShutdown>(OnMovementShutdown);
        SubscribeLocalEvent<ProneCrawlAnimationComponent, AnimationCompletedEvent>(OnAnimationCompleted);
    }

    private void OnPullStarted(Entity<ActiveProneCrawlMovementComponent> ent, ref ProneCrawlPullStartedEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (!TryComp<SpriteComponent>(ent, out var sprite) || !TryComp<CrawlerComponent>(ent, out var crawl))
            return;

        var animationPlayer = EnsureComp<AnimationPlayerComponent>(ent.Owner);

        if (_animation.HasRunningAnimation(ent.Owner, animationPlayer, crawl.AnimationKey))
            _animation.Stop((ent.Owner, animationPlayer), crawl.AnimationKey);

        var animationState = EnsureComp<ProneCrawlAnimationComponent>(ent);
        CaptureRestState(animationState, sprite.Offset, sprite.Scale);
        RestoreAnimationState((ent.Owner, animationState), sprite);

        var duration = MathF.Max(0.05f, (float) args.Duration.TotalSeconds);
        var backOffset = animationState.BaseOffset - args.Direction * crawl.AnimationPullBackDistance;
        var stretchedScale = new Vector2(
            animationState.BaseScale.X * crawl.AnimationPullScaleMultiplier.X,
            animationState.BaseScale.Y * crawl.AnimationPullScaleMultiplier.Y);

        var animation = new Animation
        {
            Length = TimeSpan.FromSeconds(duration),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Offset),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(animationState.BaseOffset, 0f),
                        new AnimationTrackProperty.KeyFrame(backOffset, duration * PullBackPeak, Easings.OutQuad),
                        new AnimationTrackProperty.KeyFrame(animationState.BaseOffset, duration, Easings.InQuad),
                    }
                },
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Scale),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(animationState.BaseScale, 0f),
                        new AnimationTrackProperty.KeyFrame(stretchedScale, duration * PullBackPeak, Easings.OutQuad),
                        new AnimationTrackProperty.KeyFrame(animationState.BaseScale, duration, Easings.InQuad),
                    }
                }
            }
        };

        _animation.Play((ent.Owner, animationPlayer), animation, crawl.AnimationKey);
    }

    private void OnAnimationCompleted(Entity<ProneCrawlAnimationComponent> ent, ref AnimationCompletedEvent args)
    {
        if (!TryComp<CrawlerComponent>(ent, out var crawl))
            return;

        if (args.Key != crawl.AnimationKey || !TryComp<SpriteComponent>(ent, out var sprite))
            return;

        RestoreAnimationState((ent.Owner, ent.Comp), sprite);
    }

    private void OnMovementShutdown(Entity<ActiveProneCrawlMovementComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<CrawlerComponent>(ent, out var crawl) || !TryComp<ProneCrawlAnimationComponent>(ent, out var animationState))
            return;

        if (TryComp<AnimationPlayerComponent>(ent, out var animationPlayer) &&
            _animation.HasRunningAnimation(ent.Owner, animationPlayer, crawl.AnimationKey))
        {
            _animation.Stop((ent.Owner, animationPlayer), crawl.AnimationKey);
        }

        if (TryComp<SpriteComponent>(ent, out var sprite))
            RestoreAnimationState((ent.Owner, animationState), sprite);

        RemComp<ProneCrawlAnimationComponent>(ent.Owner);
    }

    private void RestoreAnimationState(Entity<ProneCrawlAnimationComponent> ent, SpriteComponent sprite)
    {
        _sprite.SetOffset((ent.Owner, sprite), ent.Comp.BaseOffset);
        _sprite.SetScale((ent.Owner, sprite), ent.Comp.BaseScale);
    }

    private void CaptureRestState(ProneCrawlAnimationComponent component, Vector2 offset, Vector2 scale)
    {
        if (component.BaseStateCaptured)
            return;

        component.BaseOffset = offset;
        component.BaseScale = scale;
        component.BaseStateCaptured = true;
    }
}

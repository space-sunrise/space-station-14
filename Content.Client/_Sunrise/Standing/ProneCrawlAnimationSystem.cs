using System.Numerics;
using Content.Shared._Sunrise.Standing;
using Content.Shared._Sunrise.Standing.Components;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.Timing;

namespace Content.Client._Sunrise.Standing;

public sealed class ProneCrawlAnimationSystem : EntitySystem
{
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private const string AnimationKey = "prone-crawl-pull";
    private const float PullBackDistance = 0.08f;
    private static readonly Vector2 PullScaleMultiplier = new(1.05f, 0.95f);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActiveProneCrawlMovementComponent, ProneCrawlPullStartedEvent>(OnPullStarted);
        SubscribeLocalEvent<ActiveProneCrawlMovementComponent, ComponentShutdown>(OnMovementShutdown);
        SubscribeLocalEvent<ProneCrawlAnimationComponent, AnimationCompletedEvent>(OnAnimationCompleted);
    }

    private void OnPullStarted(Entity<ActiveProneCrawlMovementComponent> ent, ref ProneCrawlPullStartedEvent args)
    {
        if (!_timing.IsFirstTimePredicted || !TryComp<SpriteComponent>(ent, out var sprite))
            return;

        var animationState = EnsureComp<ProneCrawlAnimationComponent>(ent);
        CaptureRestState(animationState, sprite.Offset, sprite.Scale);
        var animationPlayer = EnsureComp<AnimationPlayerComponent>(ent.Owner);

        if (_animation.HasRunningAnimation(ent.Owner, animationPlayer, AnimationKey))
        {
            _animation.Stop((ent.Owner, animationPlayer), AnimationKey);
            RestoreAnimationState((ent.Owner, animationState), sprite);
        }
        else
            RestoreAnimationState((ent.Owner, animationState), sprite);

        var duration = MathF.Max(0.05f, (float) args.Duration.TotalSeconds);
        var backOffset = animationState.BaseOffset - args.Direction * PullBackDistance;
        var stretchedScale = new Vector2(
            animationState.BaseScale.X * PullScaleMultiplier.X,
            animationState.BaseScale.Y * PullScaleMultiplier.Y);

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
                        new AnimationTrackProperty.KeyFrame(backOffset, duration * 0.35f, Easings.OutQuad),
                        new AnimationTrackProperty.KeyFrame(animationState.BaseOffset, duration, Easings.InQuad)
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
                        new AnimationTrackProperty.KeyFrame(stretchedScale, duration * 0.35f, Easings.OutQuad),
                        new AnimationTrackProperty.KeyFrame(animationState.BaseScale, duration, Easings.InQuad)
                    }
                }
            }
        };

        _animation.Play((ent.Owner, animationPlayer), animation, AnimationKey);
    }

    private void OnAnimationCompleted(Entity<ProneCrawlAnimationComponent> ent, ref AnimationCompletedEvent args)
    {
        if (args.Key != AnimationKey || !TryComp<SpriteComponent>(ent, out var sprite))
            return;

        RestoreAnimationState((ent.Owner, ent.Comp), sprite);
    }

    private void OnMovementShutdown(Entity<ActiveProneCrawlMovementComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<ProneCrawlAnimationComponent>(ent, out var animationState) ||
            !TryComp<SpriteComponent>(ent, out var sprite))
        {
            return;
        }

        if (TryComp<AnimationPlayerComponent>(ent, out var animationPlayer) &&
            _animation.HasRunningAnimation(ent.Owner, animationPlayer, AnimationKey))
        {
            _animation.Stop((ent.Owner, animationPlayer), AnimationKey);
        }

        RestoreAnimationState((ent.Owner, animationState), sprite);
        RemComp<ProneCrawlAnimationComponent>(ent.Owner);
    }

    private void RestoreAnimationState(Entity<ProneCrawlAnimationComponent> ent, SpriteComponent sprite)
    {
        _sprite.SetOffset((ent.Owner, sprite), ent.Comp.BaseOffset);
        _sprite.SetScale((ent.Owner, sprite), ent.Comp.BaseScale);
    }

    internal static void CaptureRestState(ProneCrawlAnimationComponent component, Vector2 offset, Vector2 scale)
    {
        if (component.BaseStateCaptured)
            return;

        component.BaseOffset = offset;
        component.BaseScale = scale;
        component.BaseStateCaptured = true;
    }
}

using System.Numerics;
using Content.Shared._Sunrise.Animations;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.Containers;
using Robust.Shared.Random;

namespace Content.Client._Sunrise.Animations;

public sealed class ContainerInteractionAnimationSystem : EntitySystem
{
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private EntityQuery<SpriteComponent> _spriteQuery;

    private const float MinimumScale = 1.3f;
    private const string InsertTrack = "insert-animation";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ContainerInteractionAnimationComponent, EntInsertedIntoContainerMessage>(OnInsert);

        _spriteQuery = GetEntityQuery<SpriteComponent>();
    }

    private void OnInsert(Entity<ContainerInteractionAnimationComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (!_spriteQuery.TryComp(ent, out var spriteComp))
            return;

        if (_animation.HasRunningAnimation(ent, InsertTrack))
            return;

        var targetScaleX = MinimumScale + _random.NextFloat(-ent.Comp.Variation, ent.Comp.Variation);
        var targetScaleY = MinimumScale + _random.NextFloat(-ent.Comp.Variation, ent.Comp.Variation);

        var animation = new Animation
        {
            Length = TimeSpan.FromSeconds(ent.Comp.Duration),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    Property = nameof(SpriteComponent.Scale),
                    ComponentType = typeof(SpriteComponent),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(new Vector2(targetScaleX, targetScaleY), 0f),
                        new AnimationTrackProperty.KeyFrame(spriteComp.Scale, ent.Comp.Duration),
                    },
                },
            },
        };

        _animation.Play(ent, animation, InsertTrack);
    }
}

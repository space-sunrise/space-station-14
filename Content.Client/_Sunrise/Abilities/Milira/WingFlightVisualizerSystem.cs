using System;
using System.Collections.Generic;
using System.Numerics;
using Content.Shared._Sunrise.Abilities.Milira;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.Maths;

namespace Content.Client._Sunrise.Abilities.Milira;

/// <summary>
/// Клиентская визуализация полёта милиры, плавное изменение масштаба наподобие как у броска предмета
/// </summary>
public sealed class WingFlightVisualizerSystem : EntitySystem
{
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;
    [Dependency] private readonly SpriteSystem _spriteSystem = default!;

    private const string AnimationKey = "wing-flight-scale";

    private readonly Dictionary<EntityUid, Vector2> _originalScales = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WingFlightComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<WingFlightComponent, AfterAutoHandleStateEvent>(OnState);
        SubscribeLocalEvent<WingFlightComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(EntityUid uid, WingFlightComponent component, ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        _originalScales[uid] = sprite.Scale;
        ApplyScale((uid, component, sprite), immediate: true);
    }

    private void OnState(EntityUid uid, WingFlightComponent component, ref AfterAutoHandleStateEvent args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (!_originalScales.ContainsKey(uid))
            _originalScales[uid] = sprite.Scale;

        ApplyScale((uid, component, sprite), immediate: false);
    }

    private void OnShutdown(EntityUid uid, WingFlightComponent component, ComponentShutdown args)
    {
        if (!_originalScales.TryGetValue(uid, out var scale))
            return;

        _originalScales.Remove(uid);

        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        _animation.Stop(uid, AnimationKey);
        _spriteSystem.SetScale((uid, sprite), scale);
    }

    private void ApplyScale(Entity<WingFlightComponent, SpriteComponent> ent, bool immediate)
    {
        if (!_originalScales.TryGetValue(ent.Owner, out var baseScale))
            baseScale = ent.Comp2.Scale;

        var targetScale = baseScale * ent.Comp1.CurrentScaleMultiplier;

        if (immediate)
        {
            _spriteSystem.SetScale((ent.Owner, ent.Comp2), targetScale);
            return;
        }

        var animationPlayer = EnsureComp<AnimationPlayerComponent>(ent.Owner);

        if (_animation.HasRunningAnimation(ent.Owner, animationPlayer, AnimationKey))
            _animation.Stop(ent.Owner, AnimationKey);

        var currentScale = ent.Comp2.Scale;
        if (MathHelper.CloseTo(targetScale.Length(), currentScale.Length(), 0.001f))
            return;

        var anim = new Animation
        {
            Length = TimeSpan.FromSeconds(0.25f),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Scale),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(currentScale, 0f),
                        new AnimationTrackProperty.KeyFrame(targetScale, 0.25f),
                    },
                    InterpolationMode = AnimationInterpolationMode.Linear
                }
            }
        };

        _animation.Play((ent.Owner, animationPlayer), anim, AnimationKey);
    }
}


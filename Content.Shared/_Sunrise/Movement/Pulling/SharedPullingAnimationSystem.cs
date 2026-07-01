using System.Numerics;
using Content.Shared.Coordinates;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._Sunrise.Movement.Pulling;

public sealed class SharedPullingAnimationSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedMeleeWeaponSystem _melee = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private const string PullEffect = "SunriseEffectGrab";
    private static readonly TimeSpan PullEffectInterval = TimeSpan.FromSeconds(0.24);

    private readonly SoundSpecifier _pullSound = new SoundPathSpecifier("/Audio/Effects/thudswoosh.ogg")
    {
        Params = AudioParams.Default.WithVariation(0.05f),
    };

    private readonly Dictionary<EntityUid, TimeSpan> _nextPullEffect = new();
    private readonly Dictionary<EntityUid, List<EntityUid>> _activePullEffects = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PullableComponent, PullStartedMessage>(OnPullStarted);
        SubscribeLocalEvent<PullableComponent, PullStoppedMessage>(OnPullStopped);
    }

    private void OnPullStarted(Entity<PullableComponent> ent, ref PullStartedMessage args)
    {
        if (args.PulledUid != ent.Owner)
            return;

        PlayPullLunge(args.PullerUid, args.PulledUid, true);
        SpawnPullVisual(args.PulledUid);
        _nextPullEffect[ent] = _timing.CurTime + PullEffectInterval;
    }

    private void OnPullStopped(Entity<PullableComponent> ent, ref PullStoppedMessage args)
    {
        if (args.PulledUid != ent.Owner)
            return;

        _nextPullEffect.Remove(ent);
        DeletePullVisuals(ent);
        PlayPullLunge(args.PullerUid, args.PulledUid, false);
    }

    private void PlayPullLunge(EntityUid puller, EntityUid pulled, bool playSound)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (!Exists(puller) || !Exists(pulled))
            return;

        var localPos = GetPullLocalPosition(puller, pulled);
        _melee.DoLunge(puller, puller, Angle.Zero, localPos, null);

        if (playSound)
            _audio.PlayPredicted(_pullSound, pulled, puller);
    }

    private void SpawnPullVisual(EntityUid pulled)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (!Exists(pulled))
            return;

        var effect = PredictedSpawnAttachedTo(PullEffect, pulled.ToCoordinates());

        if (!_activePullEffects.TryGetValue(pulled, out var effects))
        {
            effects = new List<EntityUid>();
            _activePullEffects[pulled] = effects;
        }

        for (var i = effects.Count - 1; i >= 0; i--)
        {
            if (Deleted(effects[i]))
                effects.RemoveAt(i);
        }

        effects.Add(effect);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<PullableComponent>();
        while (query.MoveNext(out var uid, out var pullable))
        {
            if (pullable.Puller is not { } puller)
            {
                _nextPullEffect.Remove(uid);
                DeletePullVisuals(uid);
                continue;
            }

            if (!_nextPullEffect.TryGetValue(uid, out var nextEffect))
                nextEffect = TimeSpan.Zero;

            if (_timing.CurTime < nextEffect)
                continue;

            SpawnPullVisual(uid);
            _nextPullEffect[uid] = _timing.CurTime + PullEffectInterval;
        }
    }

    private Vector2 GetPullLocalPosition(EntityUid puller, EntityUid pulled)
    {
        var pullerXform = Transform(puller);
        var targetPos = _transform.GetWorldPosition(pulled);
        var localPos = Vector2.Transform(targetPos, _transform.GetInvWorldMatrix(pullerXform));
        return pullerXform.LocalRotation.RotateVec(localPos);
    }

    private void DeletePullVisuals(EntityUid pulled)
    {
        if (!_activePullEffects.Remove(pulled, out var effects))
            return;

        foreach (var effect in effects)
        {
            if (!Deleted(effect))
                QueueDel(effect);
        }
    }
}

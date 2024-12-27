using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Gravity;
using Content.Shared.Mobs.Systems;
using Content.Shared.Physics;
using Content.Shared.Standing;
using Content.Shared.StatusEffect;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;

namespace Content.Shared._Sunrise.Flip;

public abstract class SharedFlipSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly SharedGravitySystem _gravity = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedStandingStateSystem _standingStateSystem = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private EntityQuery<FixturesComponent> _fixturesQuery;

    [ValidatePrototypeId<StatusEffectPrototype>]
    private const string FlipStatusEffectKey = "Flip";
    [ValidatePrototypeId<EmotePrototype>]
    private const string EmoteFallOnNeckProto = "FallOnNeck";
    private const string FlipSound = "";
    private static float _deadChance = 0.001f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FlipComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<FlipComponent, ComponentShutdown>(OnShutdown);

        _cfg.OnValueChanged(SunriseCCVars.SunriseCCVars.FlipDeadChanse, OnFlipDeadChanseChanged, true);

        _fixturesQuery = GetEntityQuery<FixturesComponent>();
    }

    private void OnFlipDeadChanseChanged(float deadChanse)
    {
        _deadChance = deadChanse;
    }

    public void TryFlip(EntityUid uid)
    {
        if (_gravity.IsWeightless(uid) ||
            _standingStateSystem.IsDown(uid) ||
            !_mobState.IsAlive(uid))
            return;

        Flip(uid);
    }

    public void Flip(EntityUid uid)
    {
        _statusEffects.TryAddStatusEffect<FlipComponent>(uid,
            FlipStatusEffectKey,
            TimeSpan.FromMilliseconds(500),
            false);
    }

    private void OnStartup(Entity<FlipComponent> ent, ref ComponentStartup args)
    {
        if (!_fixturesQuery.TryGetComponent(ent.Owner, out var fixtures))
            return;

        // SUNRISE-TODO: Звук сальто
        //if (_net.IsServer)
        //    _audioSystem.PlayEntity(FlipSound, Filter.Pvs(ent.Owner), ent.Owner, true, AudioParams.Default.WithVolume(-5f));

        foreach (var (id, fixture) in fixtures.Fixtures)
        {
            ent.Comp.OriginalCollisionLayers[id] = fixture.CollisionLayer;

            _physics.RemoveCollisionLayer(ent.Owner, id, fixture, (int) CollisionGroup.BulletImpassable, manager: fixtures);
            _physics.RemoveCollisionLayer(ent.Owner, id, fixture, (int) CollisionGroup.Opaque, manager: fixtures);
        }
    }

    private void OnShutdown(Entity<FlipComponent> ent, ref ComponentShutdown args)
    {
        if (!_fixturesQuery.TryGetComponent(ent.Owner, out var fixtures))
            return;

        foreach (var (id, fixture) in fixtures.Fixtures)
        {
            if (ent.Comp.OriginalCollisionLayers.TryGetValue(id, out var originalLayer))
            {
                _physics.SetCollisionLayer(ent.Owner, id, fixture, originalLayer, manager: fixtures);
            }
        }

        if (_random.Prob(_deadChance) && _net.IsServer)
        {
            RaiseLocalEvent(ent, new PlayEmoteMessage(EmoteFallOnNeckProto));
        }
    }
}

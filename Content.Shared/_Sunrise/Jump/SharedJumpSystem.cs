using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Climbing.Systems;
using Content.Shared.Gravity;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Physics;
using Content.Shared.Standing;
using Content.Shared.StatusEffect;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Content.Shared.Movement.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._Sunrise.Jump;

public abstract class SharedJumpSystem : EntitySystem
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
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ClimbSystem _climbSystem = default!;
    [Dependency] private readonly PullingSystem _pullingSystem = default!;

    private EntityQuery<PhysicsComponent> _physicsQuery;
    private EntityQuery<FixturesComponent> _fixturesQuery;

    [ValidatePrototypeId<StatusEffectPrototype>]
    private const string JumpStatusEffectKey = "Jump";
    [ValidatePrototypeId<EmotePrototype>]
    private const string EmoteFallOnNeckProto = "FallOnNeck";
    private readonly SoundSpecifier _jumpSound = new SoundPathSpecifier("/Audio/_Sunrise/jump_mario.ogg");

    public bool Enable;
    private static float _deadChance;

    public bool BunnyHopEnable;
    private static TimeSpan _bunnyHopSpeedBoostWindow;
    private static float _bunnyHopSpeedUpPerJump;
    private static float _bunnyHopSpeedLimit;
    private static float _bunnyHopMinSpeedThreshold;

    private readonly List<ICommonSession> _ignoredRecipients = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<JumpComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<JumpComponent, ComponentShutdown>(OnShutdown);
        SubscribeNetworkEvent<ClientOptionDisableJumpSoundEvent>(OnClientOptionJumpSound);
        SubscribeLocalEvent<BunnyHopComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMoveSpeed);

        _physicsQuery = GetEntityQuery<PhysicsComponent>();
        _fixturesQuery = GetEntityQuery<FixturesComponent>();

        _cfg.OnValueChanged(SunriseCCVars.SunriseCCVars.JumpEnable, OnJumpEnableChanged, true);
        _cfg.OnValueChanged(SunriseCCVars.SunriseCCVars.JumpDeadChance, OnJumpDeadChanceChanged, true);
        _cfg.OnValueChanged(SunriseCCVars.SunriseCCVars.BunnyHopEnable, OnBunnyHopEnableChanged, true);
        _cfg.OnValueChanged(SunriseCCVars.SunriseCCVars.BunnyHopMinSpeedThreshold, OnBunnyHopMinSpeedThresholdChanged, true);
        _cfg.OnValueChanged(SunriseCCVars.SunriseCCVars.BunnyHopSpeedBoostWindow, OnBunnyHopSpeedBoostWindowChanged, true);
        _cfg.OnValueChanged(SunriseCCVars.SunriseCCVars.BunnyHopSpeedUpPerJump, OnBunnyHopSpeedUpPerJumpChanged, true);
        _cfg.OnValueChanged(SunriseCCVars.SunriseCCVars.BunnyHopSpeedLimit, OnBunnyHopSpeedLimitChanged, true);
    }

    private void OnRefreshMoveSpeed(EntityUid uid, BunnyHopComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        if (component.CanBunnyHop)
            args.ModifySpeed(component.SpeedMultiplier, component.SpeedMultiplier);
    }

    private async void OnClientOptionJumpSound(ClientOptionDisableJumpSoundEvent ev, EntitySessionEventArgs args)
    {
        if (ev.Disable)
            _ignoredRecipients.Add(args.SenderSession);
        else
            _ignoredRecipients.Remove(args.SenderSession);
    }

    private void OnJumpEnableChanged(bool enanle)
    {
        Enable = enanle;
    }

    private void OnJumpDeadChanceChanged(float value)
    {
        _deadChance = value;
    }

    private void OnBunnyHopEnableChanged(bool enanle)
    {
        BunnyHopEnable = enanle;
    }

    private void OnBunnyHopMinSpeedThresholdChanged(float value)
    {
        _bunnyHopMinSpeedThreshold = value;
    }

    private void OnBunnyHopSpeedBoostWindowChanged(float value)
    {
        _bunnyHopSpeedBoostWindow = TimeSpan.FromSeconds(value);
    }

    private void OnBunnyHopSpeedUpPerJumpChanged(float value)
    {
        _bunnyHopSpeedUpPerJump = value;
    }

    private void OnBunnyHopSpeedLimitChanged(float value)
    {
        _bunnyHopSpeedLimit = value;
    }

    public bool CanJump(EntityUid uid)
    {
        return !_gravity.IsWeightless(uid) &&
               !_standingStateSystem.IsDown(uid) &&
               _mobState.IsAlive(uid) &&
               !_climbSystem.IsClimbing(uid) &&
               Enable;
    }

    public void TryJump(EntityUid uid)
    {
        if (_gravity.IsWeightless(uid) ||
            _standingStateSystem.IsDown(uid) ||
            !_mobState.IsAlive(uid) ||
            _climbSystem.IsClimbing(uid) ||
            !Enable)
            return;

        Jump(uid);
    }

    public void Jump(EntityUid uid)
    {
        _statusEffects.TryAddStatusEffect<JumpComponent>(uid,
            JumpStatusEffectKey,
            TimeSpan.FromMilliseconds(500),
            false);
    }

    private void OnStartup(Entity<JumpComponent> ent, ref ComponentStartup args)
    {
        if (!_physicsQuery.TryGetComponent(ent.Owner, out var body) ||
            !_fixturesQuery.TryGetComponent(ent.Owner, out var fixtures))
            return;

        // SUNRISE-TODO: Прыжки тратят стамину
        //_staminaSystem.TakeStaminaDamage(uid, 10);

        if (_net.IsServer)
            _audioSystem.PlayEntity(_audioSystem.ResolveSound(_jumpSound), Filter.Pvs(ent.Owner).RemovePlayers(_ignoredRecipients), ent.Owner, true, AudioParams.Default.WithVolume(-5f));

        EnsureComp<CanMoveInAirComponent>(ent.Owner);
        _physics.SetBodyStatus(ent.Owner, body, BodyStatus.InAir);
        foreach (var (id, fixture) in fixtures.Fixtures)
        {
            ent.Comp.OriginalCollisionMasks[id] = fixture.CollisionMask;
            ent.Comp.OriginalCollisionLayers[id] = fixture.CollisionLayer;

            _physics.RemoveCollisionMask(ent.Owner, id, fixture, (int) CollisionGroup.LowImpassable, manager: fixtures);
        }

        if (!BunnyHopEnable)
            return;

        if (TryComp<PullerComponent>(ent, out var pull) && _pullingSystem.IsPulling(ent, pull))
            return;

        var currentSpeed = body.LinearVelocity.Length();

        if (currentSpeed < _bunnyHopMinSpeedThreshold)
            return;

        var bunnyHopComp = EnsureComp<BunnyHopComponent>(ent.Owner);
        bunnyHopComp.LastLandingTime = _timing.CurTime;
        var timeSinceLastLand = _timing.CurTime - bunnyHopComp.LastLandingTime;
        if (timeSinceLastLand <= _bunnyHopSpeedBoostWindow)
        {
            var speedMultiplier = bunnyHopComp.SpeedMultiplier += _bunnyHopSpeedUpPerJump;
            bunnyHopComp.SpeedMultiplier = Math.Min(speedMultiplier, _bunnyHopSpeedLimit);
            _movementSpeedModifier.RefreshMovementSpeedModifiers(ent.Owner);
        }
    }

    private void OnShutdown(Entity<JumpComponent> ent, ref ComponentShutdown args)
    {
        if (!_physicsQuery.TryGetComponent(ent.Owner, out var body) ||
            !_fixturesQuery.TryGetComponent(ent.Owner, out var fixtures))
            return;

        RemCompDeferred<CanMoveInAirComponent>(ent.Owner);
        _physics.SetBodyStatus(ent.Owner, body, BodyStatus.OnGround);
        foreach (var (id, fixture) in fixtures.Fixtures)
        {
            if (ent.Comp.OriginalCollisionMasks.TryGetValue(id, out var originalMask))
            {
                _physics.SetCollisionMask(ent.Owner, id, fixture, originalMask, manager: fixtures);
            }
            if (ent.Comp.OriginalCollisionLayers.TryGetValue(id, out var originalLayer))
            {
                _physics.SetCollisionLayer(ent.Owner, id, fixture, originalLayer, manager: fixtures);
            }
        }

        if (_random.Prob(_deadChance) && _net.IsServer)
        {
            RaiseLocalEvent(ent, new PlayEmoteMessage(EmoteFallOnNeckProto));
        }

        if (TryComp(ent.Owner, out BunnyHopComponent? bunnyHopComp))
        {
            bunnyHopComp.LastLandingTime = _timing.CurTime;
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<BunnyHopComponent, PhysicsComponent>();

        while (query.MoveNext(out var uid, out var bunnyHop, out var physics))
        {
            var timeSinceLastLand = _timing.CurTime - bunnyHop.LastLandingTime;
            var currentSpeed = physics.LinearVelocity.Length();

            if (timeSinceLastLand > _bunnyHopSpeedBoostWindow || currentSpeed < _bunnyHopMinSpeedThreshold)
            {
                RemComp<BunnyHopComponent>(uid);
                _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);
            }
        }
    }
}

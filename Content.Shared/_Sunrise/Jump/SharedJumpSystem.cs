using Content.Shared._Sunrise.Animations;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Climbing.Systems;
using Content.Shared.Emoting;
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
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._Sunrise.Jump;

public abstract partial class SharedJumpSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly SharedGravitySystem _gravity = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly StandingStateSystem _standingState = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly ClimbSystem _climb = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private EntityQuery<PhysicsComponent> _physicsQuery;
    private EntityQuery<FixturesComponent> _fixturesQuery;

    private static readonly ProtoId<StatusEffectPrototype> JumpStatusEffectKey = "Jump";
    private static readonly ProtoId<EmotePrototype> EmoteJumpProto = "Jump";
    private static readonly ProtoId<EmotePrototype> EmoteFallOnNeckProto = "FallOnNeck";

    private static readonly SoundSpecifier JumpSound = new SoundPathSpecifier("/Audio/_Sunrise/jump_mario.ogg");

    private readonly List<ICommonSession> _ignoredRecipients = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<JumpComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<JumpComponent, ComponentShutdown>(OnShutdown);
        SubscribeNetworkEvent<ClientOptionDisableJumpSoundEvent>(OnClientOptionJumpSound);
        SubscribeLocalEvent<BunnyHopComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMoveSpeed);
        SubscribeLocalEvent<EmoteAnimationComponent, BeforeEmoteEvent>(CheckEmote);

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

    private void CheckEmote(EntityUid uid, EmoteAnimationComponent component, BeforeEmoteEvent args)
    {
        if (args.Emote== EmoteJumpProto && !CanJump(uid))
            args.Cancel();
    }

    private static void OnRefreshMoveSpeed(Entity<BunnyHopComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (ent.Comp.CanBunnyHop)
            args.ModifySpeed(ent.Comp.SpeedMultiplier, ent.Comp.SpeedMultiplier);
    }

    public bool CanJump(EntityUid uid)
    {
        if (!_enabled)
            return false;

        if (_gravity.IsWeightless(uid))
            return false;

        if (_standingState.IsDown(uid))
            return false;

        if (!_mobState.IsAlive(uid))
            return false;

        if (_climb.IsClimbing(uid))
            return false;

        return true;
    }

    public bool TryJump(EntityUid uid)
    {
        if (!CanJump(uid))
            return false;

        Jump(uid);
        return true;
    }

    public void Jump(EntityUid uid)
    {
        _statusEffects.TryAddStatusEffect<JumpComponent>(uid,
            JumpStatusEffectKey,
            JumpComponent.JumpInAirTime,
            false);
    }

    private void OnStartup(Entity<JumpComponent> ent, ref ComponentStartup args)
    {
        if (!_physicsQuery.TryGetComponent(ent, out var body) ||
            !_fixturesQuery.TryGetComponent(ent, out var fixtures))
            return;

        // SUNRISE-TODO: Прыжки тратят стамину
        //_staminaSystem.TakeStaminaDamage(uid, 10);

        if (_net.IsServer)
            _audio.PlayEntity(JumpSound, Filter.Pvs(ent).RemovePlayers(_ignoredRecipients), ent, true, AudioParams.Default.WithVolume(-5f));

        EnsureComp<CanMoveInAirComponent>(ent);
        _physics.SetBodyStatus(ent, body, BodyStatus.InAir);
        foreach (var (id, fixture) in fixtures.Fixtures)
        {
            ent.Comp.OriginalCollisionMasks[id] = fixture.CollisionMask;
            ent.Comp.OriginalCollisionLayers[id] = fixture.CollisionLayer;

            _physics.RemoveCollisionMask(ent, id, fixture, (int) CollisionGroup.MidImpassable, manager: fixtures);
        }

        TryBunnyHop(ent, body);
    }

    private bool TryBunnyHop(Entity<JumpComponent> ent, PhysicsComponent body)
    {
        if (!_bunnyHopEnabled)
            return false;

        if (TryComp<PullerComponent>(ent, out var pull) && _pulling.IsPulling(ent, pull))
            return false;

        var currentSpeed = body.LinearVelocity.Length();

        if (currentSpeed < _bunnyHopMinSpeedThreshold)
            return false;

        var bunnyHopComp = EnsureComp<BunnyHopComponent>(ent);
        bunnyHopComp.LastLandingTime = _timing.CurTime;

        var timeSinceLastLand = _timing.CurTime - bunnyHopComp.LastLandingTime;
        if (timeSinceLastLand <= _bunnyHopSpeedBoostWindow)
        {
            var speedMultiplier = bunnyHopComp.SpeedMultiplier += _bunnyHopSpeedUpPerJump;
            bunnyHopComp.SpeedMultiplier = Math.Min(speedMultiplier, _bunnyHopSpeedLimit);

            _movementSpeedModifier.RefreshMovementSpeedModifiers(ent);
        }

        return true;
    }

    private void OnShutdown(Entity<JumpComponent> ent, ref ComponentShutdown args)
    {
        if (!_physicsQuery.TryComp(ent, out var body)
            || !_fixturesQuery.TryComp(ent, out var fixtures))
            return;

        RemCompDeferred<CanMoveInAirComponent>(ent);
        _physics.SetBodyStatus(ent, body, BodyStatus.OnGround);

        foreach (var (id, fixture) in fixtures.Fixtures)
        {
            if (ent.Comp.OriginalCollisionMasks.TryGetValue(id, out var originalMask))
            {
                _physics.SetCollisionMask(ent, id, fixture, originalMask, manager: fixtures);
            }
            if (ent.Comp.OriginalCollisionLayers.TryGetValue(id, out var originalLayer))
            {
                _physics.SetCollisionLayer(ent, id, fixture, originalLayer, manager: fixtures);
            }
        }

        if (_random.Prob(_deadChance) && _net.IsServer)
            RaiseLocalEvent(ent, new PlayEmoteMessage(EmoteFallOnNeckProto));

        if (TryComp<BunnyHopComponent>(ent, out var bunnyHopComp))
            bunnyHopComp.LastLandingTime = _timing.CurTime;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_bunnyHopEnabled)
            return;

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

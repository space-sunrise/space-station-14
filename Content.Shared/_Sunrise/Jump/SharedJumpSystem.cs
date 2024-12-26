using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Gravity;
using Content.Shared.Mobs.Systems;
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
    [Dependency] private readonly StaminaSystem _staminaSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedStandingStateSystem _standingStateSystem = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private EntityQuery<PhysicsComponent> _physicsQuery;
    private EntityQuery<FixturesComponent> _fixturesQuery;

    [ValidatePrototypeId<StatusEffectPrototype>]
    private const string JumpStatusEffectKey = "Jump";
    [ValidatePrototypeId<EmotePrototype>]
    private const string EmoteFallOnNeckProto = "FallOnNeck";
    private static float _deadChance = 1.0f;
    private static readonly TimeSpan SpeedBoostWindow = TimeSpan.FromSeconds(0.650);
    private const float MinSpeedThreshold = 4.0f;
    public bool Enabled;

    private readonly List<ICommonSession> _ignoredRecipients = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<JumpComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<JumpComponent, ComponentShutdown>(OnShutdown);
        SubscribeNetworkEvent<ClientOptionJumpSoundEvent>(OnClientOptionJumpSound);
        SubscribeLocalEvent<BunnyHopComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMoveSpeed);

        _physicsQuery = GetEntityQuery<PhysicsComponent>();
        _fixturesQuery = GetEntityQuery<FixturesComponent>();

        _cfg.OnValueChanged(SunriseCCVars.SunriseCCVars.JumpEnabled, OnJumpEnabledChanged, true);
        _cfg.OnValueChanged(SunriseCCVars.SunriseCCVars.JumpDeadChanse, OnJumpDeadChanseChanged, true);
    }

    private void OnRefreshMoveSpeed(EntityUid uid, BunnyHopComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        if (component.CanBunnyHop)
            args.ModifySpeed(component.SpeedMultiplier, component.SpeedMultiplier);
    }

    private async void OnClientOptionJumpSound(ClientOptionJumpSoundEvent ev, EntitySessionEventArgs args)
    {
        if (ev.Enabled)
            _ignoredRecipients.Remove(args.SenderSession);
        else
            _ignoredRecipients.Add(args.SenderSession);
    }

    private void OnJumpEnabledChanged(bool enanled)
    {
        Enabled = enanled;
    }

    private void OnJumpDeadChanseChanged(float deadChanse)
    {
        _deadChance = deadChanse;
    }

    public void TryJump(EntityUid uid)
    {
        if (_gravity.IsWeightless(uid) ||
            _standingStateSystem.IsDown(uid) ||
            !_mobState.IsAlive(uid))
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
            _audioSystem.PlayEntity("/Audio/_Sunrise/jump_mario.ogg", Filter.Pvs(ent.Owner).RemovePlayers(_ignoredRecipients), ent.Owner, true, AudioParams.Default.WithVolume(-5f));

        _physics.SetBodyStatus(ent.Owner, body, BodyStatus.InAir);
        foreach (var (id, fixture) in fixtures.Fixtures)
        {
            ent.Comp.OriginalCollisionMasks[id] = fixture.CollisionMask;
            ent.Comp.OriginalCollisionLayers[id] = fixture.CollisionLayer;

            _physics.RemoveCollisionMask(ent.Owner, id, fixture, (int) CollisionGroup.LowImpassable, manager: fixtures);
            _physics.RemoveCollisionMask(ent.Owner, id, fixture, (int) CollisionGroup.MidImpassable, manager: fixtures);
            _physics.RemoveCollisionLayer(ent.Owner, id, fixture, (int) CollisionGroup.BulletImpassable, manager: fixtures);
            _physics.RemoveCollisionLayer(ent.Owner, id, fixture, (int) CollisionGroup.Opaque, manager: fixtures);
        }

        var currentSpeed = body.LinearVelocity.Length();

        if (currentSpeed < MinSpeedThreshold)
            return;

        var bunnyHopComp = EnsureComp<BunnyHopComponent>(ent.Owner);
        bunnyHopComp.LastLandingTime = _timing.CurTime;
        var timeSinceLastLand = _timing.CurTime - bunnyHopComp.LastLandingTime;
        if (timeSinceLastLand <= SpeedBoostWindow)
        {
            bunnyHopComp.SpeedMultiplier += 0.05f;
            _movementSpeedModifier.RefreshMovementSpeedModifiers(ent.Owner);
        }
    }

    private void OnShutdown(Entity<JumpComponent> ent, ref ComponentShutdown args)
    {
        if (!_physicsQuery.TryGetComponent(ent.Owner, out var body) ||
            !_fixturesQuery.TryGetComponent(ent.Owner, out var fixtures))
            return;

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

        var query = EntityQueryEnumerator<BunnyHopComponent>();

        while (query.MoveNext(out var uid, out var bunnyHop))
        {
            var timeSinceLastLand = _timing.CurTime - bunnyHop.LastLandingTime;

            if (timeSinceLastLand > SpeedBoostWindow)
            {
                RemComp<BunnyHopComponent>(uid);
                _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);
            }
        }
    }
}

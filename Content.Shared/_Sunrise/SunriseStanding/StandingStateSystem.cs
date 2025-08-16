using Content.Shared._Sunrise.Jump;
using Content.Shared.ActionBlocker;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Gravity;
using Content.Shared.Movement.Events;
using Content.Shared.Standing;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._Sunrise.SunriseStanding;

public sealed class SunriseStandingStateSystem : EntitySystem
{
    [Dependency] private readonly SharedGravitySystem _gravity = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private readonly EntProtoId _fallStatusEffectKey = "StatusEffectFall";
    private readonly ProtoId<EmotePrototype> _emoteFallOnNeckProto = "FallOnNeck";

    private static float _fallDeadChance;

    public const float FallModifier = 0.2f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CrawlerComponent, KnockedDownEvent>(OnDown);
        SubscribeLocalEvent<FallComponent, TileFrictionEvent>(OnFallTileFriction);
        SubscribeLocalEvent<FallComponent, UpdateCanMoveEvent>(OnMoveAttempt);
        SubscribeLocalEvent<FallComponent, ComponentStartup>(UpdateCanMove);
        SubscribeLocalEvent<FallComponent, ComponentShutdown>(UpdateCanMove);

        _cfg.OnValueChanged(SunriseCCVars.SunriseCCVars.FallDeadChance, OnFallDeadChanceChanged, true);
    }
    private void OnFallDeadChanceChanged(float value)
    {
        _fallDeadChance = value;
    }

    private void UpdateCanMove(EntityUid uid, FallComponent component, EntityEventArgs args)
    {
        _blocker.UpdateCanMove(uid);
    }

    private void OnMoveAttempt(EntityUid uid, FallComponent component, UpdateCanMoveEvent args)
    {
        if (component.LifeStage > ComponentLifeStage.Running)
            return;

        args.Cancel();
    }

    private void OnFallTileFriction(EntityUid uid, FallComponent component, ref TileFrictionEvent args)
    {
        args.Modifier *= FallModifier;
    }

    private void OnDown(EntityUid uid, CrawlerComponent comp, KnockedDownEvent ev)
    {
        if (_gravity.IsWeightless(uid))
            return;

        Fall(uid);
    }

    public void Fall(EntityUid uid)
    {
        if (!TryComp<PhysicsComponent>(uid, out var physics) || HasComp<JumpComponent>(uid))
            return;

        if (!TryComp<KnockedDownComponent>(uid, out var knockedDown))
            return;

        if (knockedDown.AutoStand)
            return;

        var velocity = physics.LinearVelocity;

        if (velocity.LengthSquared() < 0.1f)
            return;

        _physics.SetLinearVelocity(uid, physics.LinearVelocity * 4f, body: physics);
        _statusEffects.TryAddStatusEffectDuration(uid,
            _fallStatusEffectKey,
            TimeSpan.FromSeconds(1));

        if (_random.Prob(_fallDeadChance) && _net.IsServer)
        {
            RaiseLocalEvent(uid, new PlayEmoteMessage(_emoteFallOnNeckProto));
        }
    }
}

using System.Numerics;
using Content.Shared._Sunrise.Jump;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared.ActionBlocker;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Emoting;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Physics;
using Content.Shared.Rotation;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;

namespace Content.Shared.Standing;

public abstract class SharedStandingStateSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
    [Dependency] private readonly SharedRotationVisualsSystem _rotation = default!;
    [Dependency] private readonly SharedBuckleSystem _buckle = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    [ValidatePrototypeId<StatusEffectPrototype>]
    private const string FallStatusEffectKey = "Fall";
    [ValidatePrototypeId<EmotePrototype>]
    private const string EmoteFallOnNeckProto = "FallOnNeck";

    private static float _fallDeadChance;

    public const float FallModifier = 0.2f;

    private const int StandingCollisionLayer = (int) CollisionGroup.LowImpassable;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StandingStateComponent, StandUpDoAfterEvent>(OnStandUpDoAfter);
        SubscribeLocalEvent<StandingStateComponent, DownDoAfterEvent>(OnDownDoAfter);
        SubscribeLocalEvent<StandingStateComponent, MoveEvent>(OnMove);
        SubscribeLocalEvent<StandingStateComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeed);
        SubscribeLocalEvent<StandingStateComponent, AttemptMobCollideEvent>(OnMobCollide);
        SubscribeLocalEvent<StandingStateComponent, AttemptMobTargetCollideEvent>(OnMobTargetCollide);
        SubscribeLocalEvent<StandingStateComponent, DropHandItemsEvent>(FallOver);
        SubscribeLocalEvent<FallComponent, TileFrictionEvent>(OnFallTileFriction);
        SubscribeLocalEvent<FallComponent, UpdateCanMoveEvent>(OnMoveAttempt);
        SubscribeLocalEvent<FallComponent, ComponentStartup>(UpdateCanMove);
        SubscribeLocalEvent<FallComponent, ComponentShutdown>(UpdateCanMove);

        _cfg.OnValueChanged(SunriseCCVars.FallDeadChance, OnFallDeadChanceChanged, true);
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

    private void FallOver(EntityUid uid, StandingStateComponent component, DropHandItemsEvent args)
    {
        var direction = EntityManager.TryGetComponent(uid, out PhysicsComponent? comp) ? comp.LinearVelocity / 50 : Vector2.Zero;
        var dropAngle = _random.NextFloat(0.8f, 1.2f);

        if (!TryComp(uid, out HandsComponent? handsComp))
            return;

        var worldRotation = _transform.GetWorldRotation(uid).ToVec();
        foreach (var hand in handsComp.Hands.Values)
        {
            if (hand.HeldEntity is not { } held)
                continue;
            if (!_handsSystem.TryDrop(uid, hand, checkActionBlocker: false, handsComp: handsComp))
                continue;

            _throwing.TryThrow(
                held,
                _random.NextAngle().RotateVec(direction / dropAngle + worldRotation / 50),
                0.5f * dropAngle * _random.NextFloat(-0.9f, 1.1f),
                uid,
                0);
        }
    }

    private void OnMobTargetCollide(Entity<StandingStateComponent> ent, ref AttemptMobTargetCollideEvent args)
    {
        if (ent.Comp.CurrentState == StandingState.Laying)
        {
            args.Cancelled = true;
        }
    }

    private void OnMobCollide(Entity<StandingStateComponent> ent, ref AttemptMobCollideEvent args)
    {
        if (ent.Comp.CurrentState == StandingState.Laying)
        {
            args.Cancelled = true;
        }
    }

    #region Implementation

    private void OnStandUpDoAfter(EntityUid uid, StandingStateComponent component, StandUpDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;
        if (_mobState.IsIncapacitated(uid))
            return;

        Stand(uid);

        args.Handled = true;
    }

    private void OnDownDoAfter(EntityUid uid, StandingStateComponent component, DownDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        Down(uid, dropHeldItems: false);

        args.Handled = true;
    }

    private void OnMove(Entity<StandingStateComponent> ent, ref MoveEvent args)
    {
        if (IsStanding(ent))
            return;

        _movement.RefreshMovementSpeedModifiers(ent);
    }

    private void OnRefreshMovementSpeed(EntityUid uid, StandingStateComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        if (IsDown(uid))
        {
            var baseSpeedModify = component.BaseSpeedModify;

            if (!TryComp<HandsComponent>(uid, out var handsComponent) || handsComponent.Hands.Count == 0)
            {
                args.ModifySpeed(baseSpeedModify, baseSpeedModify);
                return;
            }

            var totalHands = handsComponent.Hands.Count;
            var freeHands = handsComponent.CountFreeHands();

            var occupiedHandsRatio = (float)(totalHands - freeHands) / totalHands;
            var speedModifier = baseSpeedModify * (1 - occupiedHandsRatio * 0.5f);

            args.ModifySpeed(speedModifier, speedModifier);
        }
        else
        {
            args.ModifySpeed(1f, 1f);
        }
    }

    public bool TryStandUp(EntityUid uid, StandingStateComponent? standingState = null)
    {
        if (!Resolve(uid, ref standingState, false)
            || standingState.CurrentState is not StandingState.Laying
            || _mobState.IsIncapacitated(uid)
            || HasComp<KnockedDownComponent>(uid)
            || TerminatingOrDeleted(uid))
            return false;

        var args = new DoAfterArgs(EntityManager, uid, standingState.CycleTime, new StandUpDoAfterEvent(), uid)
        {
            BreakOnHandChange = false,
            RequireCanInteract = false,
            BreakOnDamage = false,
            BreakOnMove = false,
        };

        return _doAfter.TryStartDoAfter(args);
    }

    public bool TryLieDown(EntityUid uid, StandingStateComponent? standingState = null)
    {
        if (!Resolve(uid, ref standingState, false)
            || standingState.CurrentState is not StandingState.Standing
            || !_mobState.IsAlive(uid)
            || TerminatingOrDeleted(uid))
            return false;

        var args = new DoAfterArgs(EntityManager, uid, standingState.CycleTime, new DownDoAfterEvent(), uid)
        {
            BreakOnHandChange = false,
            RequireCanInteract = false,
            BreakOnMove = true,
            BreakOnDamage = true,
        };

        return _doAfter.TryStartDoAfter(args);
    }

    public void Fall(EntityUid uid)
    {
        if (!TryComp<PhysicsComponent>(uid, out var physics) || HasComp<JumpComponent>(uid))
            return;

        var velocity = physics.LinearVelocity;
        if (velocity.LengthSquared() < 0.1f)
        {
            Down(uid, dropHeldItems: false);
            return;
        }

        Down(uid, dropHeldItems: false);

        _physics.SetLinearVelocity(uid, physics.LinearVelocity * 4f, body: physics);
        _statusEffects.TryAddStatusEffect<FallComponent>(uid,
            FallStatusEffectKey,
            TimeSpan.FromSeconds(1),
            false);

        if (_random.Prob(_fallDeadChance) && _net.IsServer)
        {
            RaiseLocalEvent(uid, new PlayEmoteMessage(EmoteFallOnNeckProto));
        }
    }

    public bool Stand(EntityUid uid,
        StandingStateComponent? standingState = null,
        AppearanceComponent? appearance = null,
        bool force = false)
    {
        if (!Resolve(uid, ref standingState, false))
            return false;

        Resolve(uid, ref appearance, false);

        if (TryComp<BuckleComponent>(uid, out var buckleComponent) && buckleComponent.Buckled)
            _buckle.TryUnbuckle(uid, uid, buckleComp: buckleComponent);

        if (IsStanding(uid, standingState))
            return true;

        if (!force)
        {
            var msg = new StandAttemptEvent();
            RaiseLocalEvent(uid, msg);

            if (msg.Cancelled)
                return false;
        }

        standingState.CurrentState = StandingState.Standing;
        Dirty(uid, standingState);
        RaiseLocalEvent(uid, new StoodEvent());

        _appearance.SetData(uid, RotationVisuals.RotationState, RotationState.Vertical, appearance);

        if (TryComp(uid, out FixturesComponent? fixtureComponent))
        {
            foreach (var key in standingState.ChangedFixtures)
            {
                if (fixtureComponent.Fixtures.TryGetValue(key, out var fixture))
                    _physics.SetCollisionMask(uid, key, fixture, fixture.CollisionMask | StandingCollisionLayer, fixtureComponent);
            }
        }

        standingState.ChangedFixtures.Clear();
        _movement.RefreshMovementSpeedModifiers(uid);

        return true;
    }

    public bool Down(EntityUid uid,
        bool playSound = true,
        bool dropHeldItems = true,
        bool force = false,
        StandingStateComponent? standingState = null,
        AppearanceComponent? appearance = null,
        HandsComponent? hands = null)
    {
        if (!Resolve(uid, ref standingState, false))
            return false;

        Resolve(uid, ref appearance, ref hands, false);

        if (TryComp<BuckleComponent>(uid, out var buckleComponent) && buckleComponent.Buckled)
            _buckle.TryUnbuckle(uid, uid, buckleComp: buckleComponent);

        if (IsDown(uid, standingState))
            return true;

        if (dropHeldItems && hands != null)
            RaiseLocalEvent(uid, new DropHandItemsEvent());

        if (!force)
        {
            var msg = new DownAttemptEvent();
            RaiseLocalEvent(uid, msg);

            if (msg.Cancelled)
                return false;
        }

        standingState.CurrentState = StandingState.Laying;
        Dirty(uid, standingState);

        var transform = Transform(uid);
        var rotation = transform.LocalRotation;
        _appearance.TryGetData<bool>(uid, BuckleVisuals.Buckled, out var buckled, appearance);

        if (!buckled && (!_appearance.TryGetData<MobState>(uid, MobStateVisuals.State, out var state, appearance) ||
                         state is MobState.Alive))
        {
            if (rotation.GetDir() is Direction.East
                or Direction.North
                or Direction.NorthEast
                or Direction.SouthEast)
                _rotation.SetHorizontalAngle(uid, Angle.FromDegrees(270));
            else
                _rotation.ResetHorizontalAngle(uid);
        }

        RaiseLocalEvent(uid, new DownedEvent());

        _appearance.SetData(uid, RotationVisuals.RotationState, RotationState.Horizontal, appearance);

        if (TryComp(uid, out FixturesComponent? fixtureComponent))
        {
            foreach (var (key, fixture) in fixtureComponent.Fixtures)
            {
                if ((fixture.CollisionMask & StandingCollisionLayer) == 0)
                    continue;

                standingState.ChangedFixtures.Add(key);
                _physics.SetCollisionMask(uid, key, fixture, fixture.CollisionMask & ~StandingCollisionLayer, manager: fixtureComponent);
            }
        }

        _movement.RefreshMovementSpeedModifiers(uid);

        if (_net.IsServer && playSound)
            _audio.PlayPvs(standingState.DownSound, uid);

        return true;
    }

    #endregion

    #region Helpers

    public bool IsDown(EntityUid uid, StandingStateComponent? standingState = null)
    {
        if (!Resolve(uid, ref standingState, false))
            return false;

        return standingState.CurrentState == StandingState.Laying;
    }

    public bool IsStanding(EntityUid uid, StandingStateComponent? standingState = null)
    {
        if (!Resolve(uid, ref standingState, false))
            return false;

        return standingState.CurrentState == StandingState.Standing;
    }

    #endregion
}

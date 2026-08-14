using System.Linq;
using System.Numerics;
using Content.Server.Actions;
using Content.Server.Destructible;
using Content.Shared._Sunrise.Antags.Vampires.Events;
using Content.Shared._Sunrise.Antags.Vampires.Components;
using Content.Shared._Sunrise.Antags.Vampires.Components.Effects;
using Content.Shared._Sunrise.Antags.Vampires.Components.Classes;
using Content.Shared._Sunrise.Antags.Vampires.Systems;
using Content.Shared.Actions;
using Content.Shared.CombatMode;
using Content.Shared.Damage;
using Content.Shared.Damage.Events;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Components;
using Content.Shared.Ensnaring.Components;
using Content.Shared.Bed.Sleep;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Popups;
using Content.Shared.Prying.Components;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Shared.Physics;

namespace Content.Server._Sunrise.Antags.Vampires.Systems.Classes;

public sealed class GargantuaSystem : EntitySystem
{
    private const string ChargeActionId = "ActionVampireCharge";

    private static readonly ProtoId<DamageGroupPrototype> BruteGroupId = "Brute";
    private static readonly ProtoId<DamageGroupPrototype> BurnGroupId = "Burn";

    [Dependency] private readonly VampireSystem _vampire = default!;

    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly DestructibleSystem _destructible = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedVampireActionUseSystem _vampireActions = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VampireSeismicStompActionEvent>(OnSeismicStomp);
        SubscribeLocalEvent<VampireDemonicGraspActionEvent>(OnDemonicGrasp);
        SubscribeLocalEvent<VampireChargeActionEvent>(OnCharge);

        SubscribeLocalEvent<GargantuaComponent, StartCollideEvent>(OnChargeCollide);

        SubscribeLocalEvent<ActiveBloodSwellComponent, StatusEffectRelayedEvent<BeforeDamageChangedEvent>>(OnBloodSwellIncomingDamage);
        SubscribeLocalEvent<ActiveBloodSwellComponent, StatusEffectRelayedEvent<BeforeStaminaDamageEvent>>(OnBloodSwellStaminaDamage);

        SubscribeLocalEvent<GargantuaComponent, VampireBloodDrankEvent>(OnBloodDrank);
        SubscribeLocalEvent<GargantuaComponent, UserPriedDoorEvent>(OnDoorPried);
        // Status effects are raised on the status effect entity, so hook globally.
        SubscribeLocalEvent<StatusEffectComponent, StatusEffectAppliedEvent>(OnStatusEffectApplied);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        var now = _timing.CurTime;

        var query = EntityQueryEnumerator<GargantuaComponent>();
        while (query.MoveNext(out var uid, out var gargantua))
        {
            if (gargantua.IsCharging)
                ProcessChargeMovement((uid, gargantua));
        }

        ProcessActiveDemonicGrasps(now);
    }

    private void OnBloodDrank(Entity<GargantuaComponent> ent, ref VampireBloodDrankEvent args)
    {
        if (!TryComp<VampireComponent>(ent, out var vampire))
            return;

        if (vampire.TotalBlood < ent.Comp.PassiveHealBloodThreshold)
            return;

        var spec = new DamageSpecifier();
        foreach (var (groupId, amount) in ent.Comp.PassiveHealGroups)
        {
            if (amount <= FixedPoint2.Zero || !_prototype.TryIndex<DamageGroupPrototype>(groupId, out var group))
                continue;

            spec += new DamageSpecifier(group, -amount);
        }

        if (spec.Empty)
            return;

        _damageable.TryChangeDamage(ent.Owner, spec, true);
    }

    private bool TryGetVampireActionEvent<T>(VampireComponent vampire, string actionId, out T ev)
        where T : BaseActionEvent
    {
        ev = default!;

        if (!vampire.ActionEntities.TryGetValue(actionId, out var actionEntity))
            return false;

        if (_actions.GetEvent(actionEntity) is not T typed)
            return false;

        ev = typed;
        return true;
    }

    #region Blood Swell

    private void OnBloodSwellIncomingDamage(Entity<ActiveBloodSwellComponent> ent, ref StatusEffectRelayedEvent<BeforeDamageChangedEvent> args)
    {
        foreach (var entry in args.Args.Damage.DamageDict.ToArray())
        {
            var type = entry.Key;
            var value = entry.Value;
            if (value <= 0)
                continue;

            if (ent.Comp.ReducedDamageTypes.Contains(type))
                args.Args.Damage.DamageDict[type] = value * ent.Comp.IncomingDamageMultiplier;
        }
    }

    private void OnBloodSwellStaminaDamage(Entity<ActiveBloodSwellComponent> ent, ref StatusEffectRelayedEvent<BeforeStaminaDamageEvent> args)
    {
        var ev = args.Args;
        ev.Value *= ent.Comp.StaminaDamageMultiplier;
        args.Args = ev;
    }

    private void OnStatusEffectApplied(Entity<StatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        if (!_statusEffects.TryEffectsWithComp<ActiveBloodSwellComponent>(args.Target, out var effects))
            return;

        if (ent.Comp.EndEffectTime is not { } end)
            return;

        // Only affect the same set of status effects as before.
        if (!IsBloodSwellExtendedStatus(ent.Owner))
            return;

        var now = _timing.CurTime;

        var remaining = end - now;
        if (remaining <= TimeSpan.Zero)
            return;

        if (MetaData(ent.Owner).EntityPrototype is not { ID: var protoId })
            return;

        var multiplier = 1f;
        foreach (var activeEffect in effects)
        {
            multiplier = MathF.Min(multiplier, activeEffect.Comp1.StatusEffectDurationMultiplier);
        }

        _statusEffects.TrySetStatusEffectDuration(args.Target, protoId, remaining * multiplier);
    }

    private bool IsBloodSwellExtendedStatus(EntityUid effectUid)
    {
        if (HasComp<StunnedStatusEffectComponent>(effectUid))
            return true;

        if (HasComp<KnockdownStatusEffectComponent>(effectUid))
            return true;

        if (HasComp<MovementModStatusEffectComponent>(effectUid))
            return true;

        return HasComp<ForcedSleepingStatusEffectComponent>(effectUid);
    }

    #endregion

    #region Seismic Stomp

    private void OnSeismicStomp(VampireSeismicStompActionEvent args)
    {
        if (args.Handled)
            return;

        var uid = args.Performer;
        var actionEntity = args.Action.Owner;
        var canUseAction = Exists(actionEntity)
            && HasComp<GargantuaComponent>(uid)
            && _vampireActions.TryUse(uid, actionEntity);

        if (!canUseAction)
            return;

        var xform = Transform(uid);
        var worldPos = _transform.GetWorldPosition(xform);

        _popup.PopupEntity(Loc.GetString("vampire-seismic-stomp-activate"), uid, uid);

        // Find all entities in radius
        var entities = _lookup.GetEntitiesInRange(xform.Coordinates, args.Radius);

        foreach (var target in entities)
        {
            if (target == uid)
                continue;

            // Only affect mobs
            if (!HasComp<MobStateComponent>(target))
                continue;

            var targetXform = Transform(target);
            var targetPos = _transform.GetWorldPosition(targetXform);
            var direction = targetPos - worldPos;

            if (direction == Vector2.Zero)
                direction = _random.NextVector2();

            direction = direction.Normalized();

            // Knockdown the target
            _stun.TryKnockdown(target, TimeSpan.FromSeconds(2), true);

            // Throw them away from the vampire
            _throwing.TryThrow(target, direction * args.ThrowDistance, 5f, uid);
        }

        _audio.PlayPvs(args.Sound, xform.Coordinates, AudioParams.Default.WithVolume(3f));

        // Spawn visual effect at vampire's position
        Spawn("VampireSeismicStompEffect", xform.Coordinates);

        args.Handled = true;
    }

    #endregion

    #region Overwhelming Force

    private void OnDoorPried(Entity<GargantuaComponent> ent, ref UserPriedDoorEvent args)
    {
        if (!ent.Comp.OverwhelmingForceActive)
            return;

        if (!TryComp<VampireComponent>(ent, out var vampire))
            return;

        Entity<VampireComponent> vampireEnt = (ent.Owner, vampire);
        if (!_vampire.TrySpendBlood(vampireEnt, ent.Comp.OverwhelmingForceDoorPryBloodCost, showPopup: false))
            return;

        _audio.PlayPvs(ent.Comp.OverwhelmingForcePrySound, ent, AudioParams.Default.WithVolume(2f));
    }

    #endregion

    #region Demonic Grasp

    private void OnDemonicGrasp(VampireDemonicGraspActionEvent args)
    {
        if (args.Handled)
            return;

        var uid = args.Performer;
        var actionEntity = args.Action.Owner;
        if (!HasComp<GargantuaComponent>(uid))
            return;

        if (HasComp<ActiveVampireDemonicGraspComponent>(uid))
        {
            args.Handled = true;
            return;
        }

        var xform = Transform(uid);
        if (xform.GridUid is not { } gridUid)
            return;

        if (_transform.GetGrid(args.Target) != gridUid)
            return;

        var direction = (args.Target.Position - xform.Coordinates.Position).Normalized();

        if (direction == Vector2.Zero)
            return;

        if (!Exists(actionEntity) || !_vampireActions.TryUse(uid, actionEntity))
            return;

        args.Handled = true;

        // Check if combat mode is active for pulling
        var shouldPull = TryComp<CombatModeComponent>(uid, out var combat) && combat.IsInCombatMode;

        _audio.PlayPvs(args.Sound, args.Target, AudioParams.Default.WithVolume(3f));

        var distance = MathF.Min(args.Range, (args.Target.Position - xform.Coordinates.Position).Length());
        var maxTiles = Math.Max(1, (int)MathF.Ceiling(distance));
        var tileInterval = args.ProjectileSpeed > 0f
            ? TimeSpan.FromSeconds(1f / args.ProjectileSpeed)
            : args.TileInterval;

        Entity<ActiveVampireDemonicGraspComponent> active = (uid, EnsureComp<ActiveVampireDemonicGraspComponent>(uid));
        active.Comp.StartCoordinates = xform.Coordinates;
        active.Comp.GridUid = gridUid;
        active.Comp.Direction = direction;
        active.Comp.CurrentTile = 0;
        active.Comp.MaxTiles = maxTiles;
        active.Comp.TileInterval = tileInterval;
        active.Comp.ImmobilizeDuration = args.ImmobilizeDuration;
        active.Comp.PullTarget = shouldPull;
        active.Comp.EffectPrototype = args.EffectPrototype;
        active.Comp.ImmobilizedEffectPrototype = args.ImmobilizedEffectPrototype;
        active.Comp.NextTileTime = _timing.CurTime + tileInterval;
    }

    private void ProcessActiveDemonicGrasps(TimeSpan now)
    {
        var query = EntityQueryEnumerator<ActiveVampireDemonicGraspComponent>();
        while (query.MoveNext(out var uid, out var active))
        {
            Entity<ActiveVampireDemonicGraspComponent> ent = (uid, active);
            if (ent.Comp.TileInterval <= TimeSpan.Zero)
            {
                RemComp<ActiveVampireDemonicGraspComponent>(ent);
                continue;
            }

            while (now >= ent.Comp.NextTileTime)
            {
                ent.Comp.CurrentTile++;
                if (ent.Comp.CurrentTile > ent.Comp.MaxTiles || !Exists(ent.Comp.GridUid))
                {
                    RemComp<ActiveVampireDemonicGraspComponent>(ent);
                    break;
                }

                var tileCoords = ent.Comp.StartCoordinates.Offset(ent.Comp.Direction * ent.Comp.CurrentTile);
                if (ProcessDemonicGraspTile(ent, tileCoords))
                {
                    RemComp<ActiveVampireDemonicGraspComponent>(ent);
                    break;
                }

                ent.Comp.NextTileTime += ent.Comp.TileInterval;
            }
        }
    }

    private bool ProcessDemonicGraspTile(Entity<ActiveVampireDemonicGraspComponent> ent, EntityCoordinates tileCoords)
    {
        if (!_vampire.IsValidTile(tileCoords, ent.Comp.GridUid))
            return true;

        var entitiesOnTile = _lookup.GetEntitiesInRange(tileCoords, 0.4f);
        foreach (var target in entitiesOnTile)
        {
            if (target == ent.Owner)
                continue;

            if (TryComp<PhysicsComponent>(target, out var physics)
                && physics.BodyType == BodyType.Static
                && physics.Hard
                && (physics.CollisionLayer & (int)CollisionGroup.Impassable) != 0)
            {
                EntityManager.SpawnAttachedTo(ent.Comp.EffectPrototype, tileCoords);
                return true;
            }
        }

        foreach (var target in entitiesOnTile)
        {
            if (target == ent.Owner || !HasComp<MobStateComponent>(target))
                continue;

            if (ent.Comp.PullTarget)
            {
                _stun.TryAddParalyzeDuration(target, ent.Comp.ImmobilizeDuration);
            }
            else
            {
                _stun.TryAddStunDuration(target, ent.Comp.ImmobilizeDuration);

                if (!HasComp<KnockedDownComponent>(target))
                {
                    var attachCoords = new EntityCoordinates(target, Vector2.Zero);
                    EntityManager.SpawnAttachedTo(ent.Comp.ImmobilizedEffectPrototype, attachCoords);
                }
            }

            if (ent.Comp.PullTarget)
            {
                var vampirePos = _transform.GetWorldPosition(Transform(ent));
                var targetCurrentPos = _transform.GetWorldPosition(Transform(target));
                var pullDirection = (vampirePos - targetCurrentPos).Normalized();
                var distance = (vampirePos - targetCurrentPos).Length();
                if (distance > 1f)
                    _throwing.TryThrow(target, pullDirection * (distance - 1f), 8f, ent);
                _popup.PopupEntity(Loc.GetString("vampire-demonic-grasp-pull"), ent, ent);
            }

            _popup.PopupEntity(Loc.GetString("vampire-demonic-grasp-hit"), target, target, PopupType.LargeCaution);
            return true;
        }

        EntityManager.SpawnAttachedTo(ent.Comp.EffectPrototype, tileCoords);
        return false;
    }

    #endregion

    #region Charge

    private void OnCharge(VampireChargeActionEvent args)
    {
        if (args.Handled)
            return;

        var actionEntity = args.Action.Owner;

        if (!TryComp<GargantuaComponent>(args.Performer, out var gargantua))
            return;

        Entity<GargantuaComponent> ent = (args.Performer, gargantua);
        if (ent.Comp.IsCharging)
            return;

        if (TryComp<EnsnareableComponent>(ent, out var ensnareable) && ensnareable.IsEnsnared)
        {
            _popup.PopupEntity(Loc.GetString("vampire-legs-ensnared"), ent, ent, PopupType.Medium);
            return;
        }

        var xform = Transform(ent);
        var startPos = _transform.GetWorldPosition(xform);
        var targetPos = _transform.ToMapCoordinates(args.Target).Position;
        var delta = targetPos - startPos;
        var direction = delta.Normalized();

        if (direction == Vector2.Zero)
            return;

        if (!TryComp<PhysicsComponent>(ent, out var physics))
            return;

        if (!Exists(actionEntity) || !_vampireActions.TryUse(ent, actionEntity))
            return;

        ent.Comp.IsCharging = true;
        ent.Comp.ChargeDirectionVector = direction;
        ent.Comp.ChargeSpeed = args.ChargeSpeed;
        ent.Comp.ChargeCreatureDamage = args.CreatureDamage;
        ent.Comp.ChargeCreatureThrowDistance = args.CreatureThrowDistance;
        ent.Comp.ChargeStructuralDamage = args.StructuralDamage;
        ent.Comp.ChargeSound = args.Sound;

        // Kick off movement immediately so the charge feels responsive
        _physics.SetLinearVelocity(ent, direction * ent.Comp.ChargeSpeed, body: physics);

        _popup.PopupEntity(Loc.GetString("vampire-charge-start"), ent, ent);

        Dirty(ent);
        args.Handled = true;
    }

    private void ProcessChargeMovement(Entity<GargantuaComponent> ent)
    {
        if (!TryComp<PhysicsComponent>(ent, out var physics))
        {
            EndCharge(ent);
            return;
        }

        var xform = Transform(ent);

        if (xform.GridUid is null || !TryComp<MapGridComponent>(xform.GridUid, out var grid))
        {
            EndCharge(ent);
            return;
        }

        var tileRef = _map.GetTileRef(xform.GridUid.Value, grid, xform.Coordinates);
        if (tileRef.Tile.IsEmpty)
        {
            // Check if were over void/space
            EndCharge(ent);
            return;
        }

        // Keep pushing forward at a constant speed
        _physics.SetLinearVelocity(ent, ent.Comp.ChargeDirectionVector * ent.Comp.ChargeSpeed, body: physics);
    }

    private void OnChargeCollide(Entity<GargantuaComponent> ent, ref StartCollideEvent args)
    {
        if (!ent.Comp.IsCharging)
            return;

        var other = args.OtherEntity;
        if (other == ent.Owner)
            return;

        // Never interact with contained entities
        if (_container.IsEntityInContainer(other))
            return;

        // Mobs
        if (HasComp<MobStateComponent>(other))
        {
            HandleChargeImpact(ent, other);
            EndCharge(ent);
            return;
        }

        if (!TryComp<PhysicsComponent>(ent, out var ourPhysics))
        {
            EndCharge(ent);
            return;
        }

        if (TryComp<PhysicsComponent>(other, out var otherPhysics)
            && otherPhysics.BodyType == BodyType.Static
            && otherPhysics.CanCollide
            && otherPhysics.Hard
            && (ourPhysics.CollisionMask & otherPhysics.CollisionLayer) != 0)
        {
            // Static obstacle
            var obstacleCoords = Transform(other).Coordinates;

            _audio.PlayPvs(ent.Comp.ChargeSound, obstacleCoords, AudioParams.Default.WithVolume(3f));

            if (ent.Comp.ChargeStructuralDamage > 0f && TryComp<DamageableComponent>(other, out _))
            {
                var damageSpec = new DamageSpecifier();
                damageSpec.DamageDict["Blunt"] = FixedPoint2.New(ent.Comp.ChargeStructuralDamage);
                _damageable.TryChangeDamage(other, damageSpec, true, origin: ent);
            }

            EndCharge(ent);
        }
    }

    private void HandleChargeImpact(Entity<GargantuaComponent> ent, EntityUid target)
    {
        _audio.PlayPvs(ent.Comp.ChargeSound, target, AudioParams.Default.WithVolume(3f));

        var damageSpec = new DamageSpecifier();
        damageSpec.DamageDict["Blunt"] = ent.Comp.ChargeCreatureDamage;
        _damageable.TryChangeDamage(target, damageSpec, true, origin: ent);

        // Throw the target
        _throwing.TryThrow(target, ent.Comp.ChargeDirectionVector * ent.Comp.ChargeCreatureThrowDistance, 6f, ent);

        _stun.TryKnockdown(target, TimeSpan.FromSeconds(2), true);

        _popup.PopupEntity(Loc.GetString("vampire-charge-impact", ("target", target)), ent, ent);
    }

    private void EndCharge(Entity<GargantuaComponent> ent)
    {
        ent.Comp.IsCharging = false;
        ent.Comp.ChargeDirectionVector = default;
        ent.Comp.ChargeSpeed = 0f;
        ent.Comp.ChargeCreatureDamage = 0f;
        ent.Comp.ChargeCreatureThrowDistance = 0f;
        ent.Comp.ChargeStructuralDamage = 0f;
        ent.Comp.ChargeSound = null;

        if (TryComp<PhysicsComponent>(ent, out var physics))
            _physics.SetLinearVelocity(ent, Vector2.Zero, body: physics);

        Dirty(ent);
    }

    #endregion
}

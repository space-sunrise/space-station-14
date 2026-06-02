using Content.Server.Actions;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Objectives.Systems;
using Content.Server._Sunrise.Antags.Vampires.Components;
using Content.Shared._Sunrise.Antags.Vampires.Events;
using Content.Shared._Sunrise.Antags.Vampires.UI;
using Content.Shared._Sunrise.Antags.Vampires.Components;
using Content.Shared._Sunrise.Antags.Vampires.Components.Abilities;
using Content.Shared._Sunrise.Antags.Vampires.Components.Effects;
using Content.Shared._Sunrise.Antags.Vampires.Components.Visuals;
using Content.Shared._Sunrise.Antags.Vampires.Components.Classes;
using Content.Shared._Sunrise.Antags.Vampires.Prototypes;
using Content.Shared.Alert;
using Content.Shared.Actions.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Server.Body.Components;
using Content.Server.GameTicking;
using Content.Shared.Nutrition.Components;
using Content.Shared.Objectives.Components;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Content.Shared._Sunrise.NightVision.Components;
using Content.Shared.Prayer;
using Robust.Shared.Audio;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Prometheus;
using Content.Server.Body.Systems;

namespace Content.Server._Sunrise.Antags.Vampires.Systems;

public sealed partial class VampireSystem : EntitySystem
{
    # region Starlight data collection
    private static readonly Counter VampireClasses = Metrics.CreateCounter(
        "Vampire_Classes",
        "Numbers of vampire classes chosen by players",
        ["class"]
    );
    #endregion
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly BloodstreamSystem _blood = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly NumberObjectiveSystem _number = default!;
    [Dependency] private readonly IRobustRandom _rand = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly ILogManager _log = default!;
    //[Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly FlammableSystem _flammable = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;

    private ISawmill? _sawmill;
    private static readonly ProtoId<DamageGroupPrototype> BruteGroupId = "Brute";
    private static readonly ProtoId<DamageGroupPrototype> BurnGroupId = "Burn";
    private static readonly ProtoId<DamageGroupPrototype> GeneticGroupId = "Genetic";
    private static readonly ProtoId<DamageTypePrototype> CellularTypeId = "Cellular";
    private static readonly ProtoId<DamageTypePrototype> PoisonTypeId = "Poison";
    private static readonly ProtoId<DamageTypePrototype> OxyLossTypeId = "Asphyxiation";
    private static readonly ProtoId<DamageTypePrototype> HeatTypeId = "Heat";
    private static readonly ProtoId<DamageTypePrototype> PierceTypeId = "Piercing";
    private static readonly SoundSpecifier SpaceBurnSound = new SoundPathSpecifier("/Audio/Effects/lightburn.ogg");

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = _log.GetSawmill("Vampire");

        SubscribeLocalEvent<VampireComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<VampireComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<VampireComponent, VampireProgressionChangedEvent>(OnProgressionChanged);
        SubscribeLocalEvent<ActionsComponent, ComponentStartup>(OnActionsComponentStartup);
        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<VampireActionUseAttemptEvent>(OnVampireActionUseAttempt);
        InitializeAbilities();
        InitializeObjectives();
    }

    private void OnVampireActionUseAttempt(ref VampireActionUseAttemptEvent args)
    {
        args.Allowed = CheckAndConsumeGrantedVampireAction(args.User, args.ActionEntity, args.BloodCost);
    }

    private void OnPlayerAttached(PlayerAttachedEvent ev)
    {
        if (!TryComp(ev.Entity, out VampireComponent? vampire))
            return;

        SyncVampireActions((ev.Entity, vampire));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        var now = _timing.CurTime;

        var query = EntityQueryEnumerator<VampireComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (now <= comp.NextUpdate)
                continue;

            var elapsed = comp.LastUpdate == TimeSpan.Zero
                ? (float) comp.UpdateDelay.TotalSeconds
                : MathF.Max(0f, (float) (now - comp.LastUpdate).TotalSeconds);

            comp.LastUpdate = now;
            comp.NextUpdate = now + comp.UpdateDelay;

            var ent = (uid, comp);
            ProcessBloodDecay(ent, elapsed);
            HandleHolyWater(ent);
            HandleHolyPlace(ent);
        }

        var sunlightQuery = EntityQueryEnumerator<VampireSunlightComponent, TransformComponent>();
        while (sunlightQuery.MoveNext(out var uid, out var sunlight, out var xform))
        {
            if (!TryComp<VampireComponent>(uid, out var vampire))
                continue;

            HandleSpaceExposure((uid, vampire, sunlight), xform);
        }

        ProcessActiveVampireEffects(now);
    }

    private void HandleSpaceExposure(Entity<VampireComponent, VampireSunlightComponent> ent, TransformComponent xform)
    {
        if (_container.IsEntityInContainer(ent.Owner))
        {
            ResetSpaceExposure(ent.Comp2);
            return;
        }

        if (!IsInSpace(xform))
        {
            ResetSpaceExposure(ent.Comp2);
            return;
        }

        if (TryComp<MobStateComponent>(ent, out var mobState) &&
            mobState.CurrentState == Shared.Mobs.MobState.Dead)
        {
            ResetSpaceExposure(ent.Comp2);
            return;
        }

        var now = _timing.CurTime;

        var damageInterval = ent.Comp2.DamageInterval;
        if (damageInterval < TimeSpan.FromSeconds(0.1f))
            damageInterval = TimeSpan.FromSeconds(0.1f);

        if (ent.Comp2.TimeEnteredSpace is null)
        {
            ent.Comp2.TimeEnteredSpace = now;
            ent.Comp2.NextWarningPopup = now + ent.Comp2.GracePeriod;
            ent.Comp2.NextDamageTime = now + ent.Comp2.GracePeriod + damageInterval;
        }

        var timeInSpace = now - ent.Comp2.TimeEnteredSpace.Value;

        if (timeInSpace < ent.Comp2.GracePeriod)
            return;

        if (_timing.CurTime >= ent.Comp2.NextWarningPopup)
        {
            _popup.PopupEntity(Loc.GetString(ent.Comp2.WarningPopup), ent.Owner, ent.Owner, PopupType.LargeCaution);
            ent.Comp2.NextWarningPopup = _timing.CurTime + ent.Comp2.WarningPopupCooldown;
        }

        var nextDamage = ent.Comp2.NextDamageTime;
        if (nextDamage is null)
        {
            ent.Comp2.NextDamageTime = now + damageInterval;
            nextDamage = ent.Comp2.NextDamageTime;
        }
        if (_timing.CurTime < nextDamage)
            return;

        if (!ProcessSpaceExposureTick(ent))
            return;

        ent.Comp2.NextDamageTime = now + damageInterval;
    }

    private void ResetSpaceExposure(VampireSunlightComponent sunlight)
    {
        sunlight.TimeEnteredSpace = null;
        sunlight.NextDamageTime = null;
        sunlight.NextWarningPopup = TimeSpan.Zero;
    }

    private bool ProcessSpaceExposureTick(Entity<VampireComponent, VampireSunlightComponent> ent)
    {
        var hadBlood = ent.Comp1.DrunkBlood > 0;

        if (hadBlood)
            DrainBlood(ent);

        else
        {
            if (!ApplyGeneticSpaceDamage(ent))
                return false;
        }

        var damageable = CompOrNull<DamageableComponent>(ent.Owner);
        var thresholds = CompOrNull<MobThresholdsComponent>(ent.Owner);
        var healthy = IsAboveHalfHealth(ent.Owner, damageable, thresholds);

        var chance = hadBlood ? ent.Comp2.BloodEffectChance : ent.Comp2.BloodlessEffectChance;
        TryApplySpaceDamage(ent, healthy, chance);

        return true;
    }

    private void DrainBlood(Entity<VampireComponent, VampireSunlightComponent> ent)
    {
        var drain = Math.Min(ent.Comp2.BloodDrainPerInterval, ent.Comp1.DrunkBlood);
        if (drain <= 0)
            return;

        TrySpendBlood(ent, drain, showPopup: false);
    }

    private bool ApplyGeneticSpaceDamage(Entity<VampireComponent, VampireSunlightComponent> ent)
    {
        if (!_proto.TryIndex<DamageGroupPrototype>(GeneticGroupId, out var damageGroup))
            return true;

        var spec = new DamageSpecifier(damageGroup, ent.Comp2.GeneticDamagePerInterval);
        _damageableSystem.TryChangeDamage(ent.Owner, spec, true);

        if (!TryComp(ent, out DamageableComponent? damageable) ||
            damageable is null ||
            !damageable.DamagePerGroup.TryGetValue(GeneticGroupId, out var geneticDamage))
        {
            return true;
        }

        _audio.PlayPvs(SpaceBurnSound, ent.Owner);

        if (geneticDamage < ent.Comp2.GeneticDustThreshold)
            return true;

        DustEntity(ent.Owner);
        return false;
    }

    private void TryApplySpaceDamage(Entity<VampireComponent, VampireSunlightComponent> ent, bool isHealthy, float chance)
    {
        if (!_rand.Prob(Math.Clamp(chance, 0f, 1f)))
            return;

        if (isHealthy)
        {
            if (_proto.TryIndex(HeatTypeId, out var heat))
            {
                var spec = new DamageSpecifier(heat, ent.Comp2.BurnDamage);
                _damageableSystem.TryChangeDamage(ent.Owner, spec, true);
            }
        }
        else
            _flammable.AdjustFireStacks(ent.Owner, ent.Comp2.FireStacksOnIgnite, ignite: true);

        _audio.PlayPvs(SpaceBurnSound, ent.Owner);
    }

    private bool IsAboveHalfHealth(EntityUid uid, DamageableComponent? damageable, MobThresholdsComponent? thresholds)
    {
        damageable ??= CompOrNull<DamageableComponent>(uid);
        thresholds ??= CompOrNull<MobThresholdsComponent>(uid);

        if (damageable is null)
            return true;

        if (!_mobThreshold.TryGetDeadThreshold(uid, out var deadThreshold, thresholds) ||
            deadThreshold is null ||
            deadThreshold.Value == FixedPoint2.Zero)
        {
            return true;
        }

        var max = deadThreshold.Value.Float();
        if (max <= 0f)
            return true;

        var current = damageable.TotalDamage.Float();
        return current <= max * 0.5f;
    }

    private void DustEntity(EntityUid uid)
    {
        var coords = Transform(uid).Coordinates;
        _popup.PopupEntity(Loc.GetString("admin-smite-turned-ash-other", ("name", uid)), uid, PopupType.LargeCaution);
        QueueDel(uid);
        Spawn("Ash", coords);
    }

    private bool IsInSpace(TransformComponent xform)
    {
        if (xform.GridUid is null)
            return true;

        if (!TryComp(xform.GridUid.Value, out MapGridComponent? grid))
            return true;

        if (!_map.TryGetTileRef(xform.GridUid.Value, grid, xform.Coordinates, out var tileRef))
            return true;

        return _turf.IsSpace(tileRef);
    }

    private bool ProcessBloodDecay(Entity<VampireComponent> ent, float elapsed)
    {
        var before = ent.Comp.BloodFullness;
        var wasStarving = before <= 0f;
        var changed = false;

        if (before > 0f && _gameTicker.RunLevel < GameRunLevel.PostRound) // No hunger EOR
        {
            ent.Comp.StarvationDrunkBloodDrainAccumulator = 0f;
            ent.Comp.BloodFullness = MathF.Max(0f, before - (ent.Comp.FullnessDecayPerSecond * elapsed));
            changed = !MathF.Abs(ent.Comp.BloodFullness - before).Equals(0f);

            if (changed)
            {
                Dirty(ent);
                UpdateVampireFedAlert(ent);
            }
        }

        var isStarving = ent.Comp.BloodFullness <= 0f;
        if (wasStarving != isStarving)
            _movementSpeed.RefreshMovementSpeedModifiers(ent.Owner);

        // When blood fullness is empty, burn stored blood
        if (ent.Comp.BloodFullness <= 0f && ent.Comp.StarvationDrunkBloodDrainPerSecond > 0 && ent.Comp.DrunkBlood > 0)
        {
            ent.Comp.StarvationDrunkBloodDrainAccumulator += ent.Comp.StarvationDrunkBloodDrainPerSecond * elapsed;
            var drained = Math.Min(ent.Comp.DrunkBlood, (int) ent.Comp.StarvationDrunkBloodDrainAccumulator);
            if (drained <= 0)
                return changed;

            ent.Comp.StarvationDrunkBloodDrainAccumulator -= drained;
            TrySpendBlood(ent, drained, showPopup: false);
            changed = true;
        }

        return changed;
    }

    private void RefreshAllActions(Entity<VampireComponent> ent)
    {
        ent.Comp.LastRefreshedBloodLevel = ent.Comp.TotalBlood;
        foreach (var (_, actionEntity) in ent.Comp.ActionEntities)
            TryRefreshVampireAction(ent.Owner, actionEntity);
    }

    private void HandleClassSelection(Entity<VampireComponent> ent)
    {
        if (HasChosenClass(ent.Owner))
            return;

        var classSelectAction = ent.Comp.ClassSelectActionId;

        if (ent.Comp.TotalBlood >= ent.Comp.ClassSelectThreshold && !ent.Comp.ActionEntities.ContainsKey(classSelectAction))
        {
            EntityUid? actionEntity = null;
            _actions.AddAction(ent.Owner, ref actionEntity, classSelectAction, ent.Owner);
            if (actionEntity is not null)
            {
                ent.Comp.ActionEntities[classSelectAction] = actionEntity.Value;
                Dirty(ent);
            }
        }

        if (ent.Comp.ActionEntities.TryGetValue(classSelectAction, out var classSelectActionEntity))
            TryRefreshVampireAction(ent.Owner, classSelectActionEntity);
    }

    private void OnProgressionChanged(Entity<VampireComponent> ent, ref VampireProgressionChangedEvent args) =>
        SyncVampireActions(ent);

    private void OnActionsComponentStartup(Entity<ActionsComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp(ent, out VampireComponent? vampire))
            return;
        SyncVampireActions((ent.Owner, vampire));
    }

    private void SyncVampireActions(Entity<VampireComponent> ent)
    {
        CleanMissingActions(ent.Comp);
        HandleClassSelection(ent);
        EnsureRejuvenateUpgrade(ent);
        TryGrantClassAbilities(ent);
        RefreshAllActions(ent);
    }

    private void CleanMissingActions(VampireComponent comp)
    {
        if (comp.ActionEntities.Count == 0)
            return;

        var snapshot = new List<KeyValuePair<EntProtoId, EntityUid>>(comp.ActionEntities);
        foreach (var pair in snapshot)
        {
            if (Exists(pair.Value))
                continue;

            comp.ActionEntities.Remove(pair.Key);
        }
    }

    private void OnStartup(Entity<VampireComponent> ent, ref ComponentStartup args)
    {
        EnsureComp<UnholyComponent>(ent);
        EnsureComp<VampireSunlightComponent>(ent);
        foreach (var actionId in ent.Comp.BaseVampireActions)
        {
            EntityUid? action = null;

            _actions.AddAction(ent.Owner, ref action, actionId, ent.Owner);

            if (action is not null)
                ent.Comp.ActionEntities[actionId] = action.Value;
        }

        RemComp<HungerComponent>(ent);
        RemComp<ThirstComponent>(ent);
        RemComp<RespiratorComponent>(ent);

        _alerts.ClearAlertCategory(ent.Owner, "Hunger");

        UpdateVampireAlert(ent.Owner);
        UpdateVampireFedAlert(ent);

        SyncVampireActions(ent);
        _movementSpeed.RefreshMovementSpeedModifiers(ent.Owner);

    }

    private void OnShutdown(Entity<VampireComponent> ent, ref ComponentShutdown args)
    {
        RemComp<UnholyComponent>(ent);
        RemComp<NightVisionComponent>(ent);
        if (TryComp<VampireDrainBeamComponent>(ent, out var drainBeamComp))
        {
            foreach (var connection in drainBeamComp.ActiveBeams.Values)
            {
                var removeEvent = new VampireDrainBeamEvent(GetNetEntity(connection.Source), GetNetEntity(connection.Target), false, drainBeamComp.VisualPrototype);
                RaiseNetworkEvent(removeEvent);
            }
            drainBeamComp.ActiveBeams.Clear();
        }

        if (TryComp<UmbraeComponent>(ent, out var umbrae))
        {
            _eye.SetDrawFov(ent, true);

            umbrae.ShadowBoxingActive = false;
            umbrae.ShadowBoxingTarget = null;
            umbrae.ShadowBoxingEndTime = null;
            umbrae.ShadowBoxingLoopRunning = false;

            if (umbrae.EternalDarknessAuraEntity is { } aura && Exists(aura))
                QueueDel(aura);

            if (umbrae.SpawnedShadowAnchorBeacon is { } beacon && Exists(beacon))
                QueueDel(beacon);

            foreach (var snare in umbrae.PlacedSnares.ToArray())
            {
                if (Exists(snare))
                    QueueDel(snare);
            }

            umbrae.PlacedSnares.Clear();
            umbrae.EternalDarknessAuraEntity = null;
            umbrae.SpawnedShadowAnchorBeacon = null;
            umbrae.ShadowAnchorAutoReturnTime = null;
        }

        if (_playerShadowSnares.TryGetValue(ent.Owner, out var snares))
        {
            foreach (var trap in snares.ToArray())
            {
                if (Exists(trap))
                    QueueDel(trap);
            }
            _playerShadowSnares.Remove(ent.Owner);
        }
    }

    partial void UpdateVampireAlert(EntityUid uid);
    partial void UpdateVampireFedAlert(Entity<VampireComponent> ent);

    private void TryRefreshVampireAction(EntityUid owner, EntityUid? actionEntity)
    {
        if (actionEntity is null
            || _actions.GetAction(actionEntity) is not { } action
            || !TryComp<VampireComponent>(owner, out var vamp))
            return;

        if (!TryComp<VampireActionComponent>(actionEntity.Value, out var vac))
        {
            _actions.SetEnabled(action.AsNullable(), true);
            return;
        }

        var enabled = vamp.TotalBlood >= vac.BloodToUnlock
             && (vac.RequiredClass is null || ValidateVampireClass(owner, vamp, vac.RequiredClass))
             && (!vac.RequiresFullPower || vamp.FullPower);

        _actions.SetEnabled(action.AsNullable(), enabled);
    }

    private void TryGrantClassAbilities(Entity<VampireComponent> ent)
    {
        if (string.IsNullOrWhiteSpace(ent.Comp.ChosenClassId))
            return;

        if (!_proto.TryIndex<VampireClassPrototype>(ent.Comp.ChosenClassId, out var classProto))
            return;

        foreach (var actionId in classProto.Actions)
            GrantAbility(ent, actionId);
    }

    private void GrantAbility(Entity<VampireComponent> ent, EntProtoId actionId)
    {
        if (ent.Comp.ActionEntities.ContainsKey(actionId))
            return;

        EntityUid? field = null;
        GrantAbility(ent, ref field, actionId);
    }

    private void GrantAbility(Entity<VampireComponent> ent, ref EntityUid? field, EntProtoId actionId)
    {
        if (field is not null)
            return;

        var threshold = GetActionBloodThreshold(actionId);

        if (ent.Comp.TotalBlood >= threshold)
        {
            _actions.AddAction(ent.Owner, ref field, actionId, ent.Owner);
            if (field is not null)
            {
                ent.Comp.ActionEntities[actionId] = field.Value;
                Dirty(ent);
            }
        }
    }

    private void OnComponentRemove(Entity<VampireComponent> ent, ComponentRemove _)
        => TryRemoveAbilities(ent);

    private void TryRemoveAbilities(Entity<VampireComponent> ent)
    {
        foreach (var (_, action) in ent.Comp.ActionEntities)
            _actions.RemoveAction(ent.Owner, action);
        ent.Comp.ActionEntities.Clear();
        Dirty(ent);
    }

    private int GetActionBloodThreshold(EntProtoId actionId)
    {
        if (_proto.TryIndex<EntityPrototype>(actionId, out var proto) &&
            proto.TryGetComponent<VampireActionComponent>(out var vac, _componentFactory))
            return vac.BloodToUnlock;
        return 0;
    }
    private void EnsureRejuvenateUpgrade(Entity<VampireComponent> ent)
    {
        if (ent.Comp.RejuvenateActions.Count < 2)
        {
            _sawmill?.Error($"Vampire {ToPrettyString(ent.Owner)} missing rejuvenate action config");
            return;
        }

        var rejuvenateI = ent.Comp.RejuvenateActions[0];
        var rejuvenateII = ent.Comp.RejuvenateActions[1];

        var unlockThreshold = GetActionBloodThreshold(rejuvenateII);
        if (ent.Comp.TotalBlood < unlockThreshold)
            return;

        if (!ent.Comp.ActionEntities.ContainsKey(rejuvenateII))
        {
            EntityUid? action = null;
            _actions.AddAction(ent.Owner, ref action, rejuvenateII, ent.Owner);
            if (action is not null)
                ent.Comp.ActionEntities[rejuvenateII] = action.Value;
        }

        TryRefreshVampireAction(ent.Owner, ent.Comp.ActionEntities[rejuvenateII]);
        if (ent.Comp.ActionEntities.TryGetValue(rejuvenateI, out var firstAction))
        {
            _actions.RemoveAction(ent.Owner, firstAction);
            ent.Comp.ActionEntities.Remove(rejuvenateI);
        }

        Dirty(ent);
    }

    private void HandleHolyWater(Entity<VampireComponent> ent)
    {
        if (ent.Comp.UniqueHumanoidVictims < 1)
            return;

        if (_timing.CurTime < ent.Comp.NextHolyWaterTick)
            return;

        var holywater = _solution.GetTotalPrototypeQuantity(ent.Owner, ent.Comp.HolyWaterReagentId);
        if (holywater <= FixedPoint2.Zero)
            return;

        if (TryComp(ent, out MobStateComponent? mobState) && mobState.CurrentState == Shared.Mobs.MobState.Dead)
            return;

        ent.Comp.NextHolyWaterTick = _timing.CurTime + ent.Comp.HolyTickDelay;

        if (ent.Comp.DrunkBlood > 0)
        {
            TrySpendBlood(ent, Math.Min(3, ent.Comp.DrunkBlood), showPopup: false);

            ApplyGroupDamage(ent.Owner, BruteGroupId, 3f);

            if (TryComp(ent, out StaminaComponent? stamina))
                _stamina.TakeStaminaDamage(ent.Owner, 5f, stamina);

            return;
        }

        ApplyGroupDamage(ent.Owner, BurnGroupId, 2f);
        if (_rand.Prob(0.25f))
            _flammable.AdjustFireStacks(ent.Owner, 2f, ignite: true);
    }

    private void HandleHolyPlace(Entity<VampireComponent> ent)
    {
        if (ent.Comp.UniqueHumanoidVictims < 1)
            return;

        if (_timing.CurTime < ent.Comp.NextHolyPlaceTick)
            return;

        if (!IsInHolyPlace(ent))
            return;

        if (TryComp(ent, out MobStateComponent? mobState) && mobState.CurrentState == Shared.Mobs.MobState.Dead)
            return;

        ent.Comp.NextHolyPlaceTick = _timing.CurTime + ent.Comp.HolyTickDelay;

        if (_timing.CurTime >= ent.Comp.NextHolyPlacePopup)
        {
            _popup.PopupEntity(Loc.GetString("vampire-holy-place-burn"), ent.Owner, ent.Owner, PopupType.MediumCaution);
            ent.Comp.NextHolyPlacePopup = _timing.CurTime + TimeSpan.FromSeconds(5);
        }

        var health = GetApproximateHealth(ent.Owner);
        if (health <= 50f)
        {
            _flammable.AdjustFireStacks(ent.Owner, 3f, ignite: true);
            return;
        }

        if (_proto.TryIndex<DamageTypePrototype>(HeatTypeId, out var heat))
        {
            var spec = new DamageSpecifier(heat, FixedPoint2.New(3f));
            _damageableSystem.TryChangeDamage(ent.Owner, spec, true);
        }
    }

    private bool IsInHolyPlace(Entity<VampireComponent> ent)
    {
        if (_container.IsEntityInContainer(ent.Owner))
            return false;

        var coords = Transform(ent).Coordinates;
        foreach (var target in _lookup.GetEntitiesInRange(coords, ent.Comp.HolyPlaceRange, LookupFlags.Static))
        {
            if (target == ent.Owner)
                continue;

            if (!HasComp<PrayableComponent>(target))
                continue;

            if (!Transform(target).Anchored)
                continue;

            if (!_interaction.InRangeUnobstructed(ent.Owner, target, ent.Comp.HolyPlaceRange))
                continue;

            return true;
        }

        return false;
    }

    private float GetApproximateHealth(EntityUid uid)
    {
        if (!TryComp(uid, out DamageableComponent? damageable))
            return 100f;

        if (!_mobThreshold.TryGetDeadThreshold(uid, out var deadThreshold, CompOrNull<MobThresholdsComponent>(uid))
            || deadThreshold is null
            || deadThreshold.Value == FixedPoint2.Zero)
        {
            return 100f - damageable.TotalDamage.Float();
        }

        return deadThreshold.Value.Float() - damageable.TotalDamage.Float();
    }

    private void ApplyGroupDamage(EntityUid uid, ProtoId<DamageGroupPrototype> groupId, float amount)
    {
        if (!_proto.TryIndex<DamageGroupPrototype>(groupId, out var group))
            return;

        var spec = new DamageSpecifier(group, FixedPoint2.New(amount));
        _damageableSystem.TryChangeDamage(uid, spec, true);
    }

    private void OpenClassUi(EntityUid uid, VampireComponent comp)
    {
        if (!string.IsNullOrWhiteSpace(comp.ChosenClassId))
            return;
        _ui.CloseUi(uid, VampireClassUiKey.Key);
        _ui.OpenUi(uid, VampireClassUiKey.Key, uid);
    }

    private void OnVampireClassChosen(EntityUid uid, VampireComponent comp, VampireClassChosenBuiMsg msg)
    {
        if (!string.IsNullOrWhiteSpace(comp.ChosenClassId))
        {
            _ui.CloseUi(uid, VampireClassUiKey.Key);
            return;
        }

        if (string.IsNullOrWhiteSpace(msg.Choice) || !_proto.TryIndex<VampireClassPrototype>(msg.Choice, out var classProto))
        {
            _ui.CloseUi(uid, VampireClassUiKey.Key);
            return;
        }

        var reg = _componentFactory.GetRegistration(classProto.ClassComponent, ignoreCase: true);
        var classComp = _componentFactory.GetComponent(reg.Type);
        EntityManager.AddComponent(uid, classComp);

        if (classProto.ID == "Umbrae")
            EnsureComp<NightVisionComponent>(uid);

        comp.ChosenClassId = classProto.ID;
        VampireClasses.WithLabels(classProto.ID).Inc();

        var classSelectAction = comp.ClassSelectActionId;
        if (comp.ActionEntities.TryGetValue(classSelectAction, out var actionEntity))
        {
            _actions.RemoveAction(uid, actionEntity);
            comp.ActionEntities.Remove(classSelectAction);
        }

        _ui.CloseUi(uid, VampireClassUiKey.Key);

        SyncVampireActions((uid, comp));

        Dirty(uid, comp);
    }

    private void OnVampireClassClosed(EntityUid uid, VampireComponent comp, VampireClassClosedBuiMsg _)
    {
        if (!string.IsNullOrWhiteSpace(comp.ChosenClassId))
            return;

        _sawmill?.Debug($"Vampire class UI closed without selection for {ToPrettyString(uid)} (blood={comp.TotalBlood})");
    }

    #region Objectives
    private void InitializeObjectives()
        => SubscribeLocalEvent<BloodDrainConditionComponent, ObjectiveGetProgressEvent>(OnBloodDrainGetProgress);

    private void OnBloodDrainGetProgress(Entity<BloodDrainConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        var target = _number.GetTarget(ent.Owner);
        if (args.Mind.OwnedEntity is not null && TryComp<VampireComponent>(args.Mind.OwnedEntity.Value, out var vampComp))
            args.Progress = target > 0 ? MathF.Min(vampComp.TotalBlood / target, 1f) : 1f;
        else
            args.Progress = 0f;
    }

    #endregion
}

using System.Linq;
using Content.Server.Actions;
using Content.Server.Antag;
using Content.Server.Body.Systems;
using Content.Server.DoAfter;
using Content.Server.Mind;
using Content.Server.Objectives;
using Content.Server.Objectives.Systems;
using Content.Server.Popups;
using Content.Server.Roles;
using Content.Server.Stunnable;
using Content.Server._Sunrise.Antags.Vampires.Components;
using Content.Shared._Sunrise.Antags.Vampires;
using Content.Shared._Sunrise.Antags.Vampires.Components;
using Content.Shared._Sunrise.Antags.Vampires.Systems;
using Content.Shared.Alert;
using Content.Shared.Actions.Components;
using Content.Shared.Body;
using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Mind;
using Content.Shared.Metabolism;
using Content.Shared.Movement.Systems;
using Content.Shared.Objectives.Components;
using Content.Server.Body.Components;
using Content.Server.Damage.Systems;
using Content.Server.GameTicking;
using Content.Shared.Damage.Components;
using Content.Shared.Nutrition.Components;
using Content.Shared.StatusEffectNew;
using Content.Shared.Prayer;
using Content.Shared.Popups;
using Content.Shared.Random;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._Sunrise.Antags.Vampires.Systems;

public sealed partial class VampireSystem : SharedVampireSystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly BloodstreamSystem _blood = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly NumberObjectiveSystem _number = default!;
    [Dependency] private readonly ObjectivesSystem _objectives = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly StunSystem _stun = default!;
    [Dependency] private readonly StaminaSystem _stamina = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly ILogManager _log = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly MetabolizerSystem _metabolizer = default!;
    [Dependency] private readonly RoleSystem _role = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedChargesSystem _charges = default!;

    private const float VampireObjectiveMaxDifficulty = 10f;
    private const string VampireMetabolizerTypeId = "Vampire";

    private static readonly ProtoId<DamageTypePrototype>[] BruteDamageTypes = ["Blunt", "Slash", "Piercing"];
    private static readonly ProtoId<DamageTypePrototype>[] BurnDamageTypes = ["Heat", "Shock", "Cold", "Caustic"];
    private static readonly ProtoId<DamageTypePrototype> HeatTypeId = "Heat";
    private static readonly ProtoId<ReagentPrototype> MuteToxinReagentId = "MuteToxin";

    private static readonly SoundSpecifier VampireBriefingSound =
        new SoundPathSpecifier("/Audio/_Sunrise/Ambience/Antag/vampire_start.ogg");

    private static readonly EntProtoId VampireMindRoleId = "MindRoleVampire";
    private static readonly EntProtoId VampireKillObjectiveId = "VampireKillRandomPersonObjective";
    private static readonly EntProtoId VampireDrainObjectiveId = "VampireDrainObjective";
    private static readonly EntProtoId VampireFangsActionId = "ActionVampireToggleFangs";
    private static readonly EntProtoId VampireGlareActionId = "ActionVampireGlare";
    private static readonly EntProtoId VampireSleepActionId = "ActionVampireSleep";
    private static readonly EntProtoId VampireRejuvenateIActionId = "ActionVampireRejuvenateI";
    private static readonly EntProtoId VampireRejuvenateIiActionId = "ActionVampireRejuvenateII";

    private static readonly EntProtoId[] BaseVampireActions =
    [
        VampireFangsActionId,
        VampireGlareActionId,
        VampireRejuvenateIActionId,
        VampireSleepActionId,
    ];

    private static readonly ProtoId<WeightedRandomPrototype> VampireStateObjectiveGroupId =
        "VampireObjectiveGroupsStateOnly";

    private static readonly ProtoId<WeightedRandomPrototype> VampireStealObjectiveGroupId =
        "VampireObjectiveGroupsStealOnly";

    private readonly List<EntProtoId> _missingActions = [];
    private ISawmill? _sawmill;

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = _log.GetSawmill("Vampire");

        SubscribeLocalEvent<VampireComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<VampireComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ActionsComponent, ComponentStartup>(OnActionsComponentStartup);
        SubscribeLocalEvent<MetabolizerComponent, BodyRelayedEvent<SetVampireMetabolismEvent>>(OnSetVampireMetabolism);
        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<BloodDrainConditionComponent, ObjectiveGetProgressEvent>(OnBloodDrainGetProgress);
        InitializeAbilities();
    }

    private void OnPlayerAttached(PlayerAttachedEvent ev)
    {
        if (!TryComp(ev.Entity, out VampireComponent? vampire))
            return;

        SyncVampireActions((ev.Entity, vampire));
    }

    private void OnBloodDrainGetProgress(Entity<BloodDrainConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        var target = _number.GetTarget(ent);
        if (args.Mind.OwnedEntity is not { } body ||
            !TryComp<VampireComponent>(body, out var vampire))
        {
            args.Progress = 0f;
            return;
        }

        args.Progress = target > 0f
            ? MathF.Min((float)vampire.TotalBlood / target, 1f)
            : 1f;
    }

    /// <summary>
    /// Проверяет возможность обращения.
    /// </summary>
    public bool CanMakeVampire(EntityUid target)
    {
        return Exists(target) &&
               !HasComp<VampireComponent>(target) &&
               _mind.TryGetMind(target, out _, out _);
    }

    /// <summary>
    /// Обращает сущность в вампира.
    /// </summary>
    public bool TryMakeVampire(EntityUid target)
    {
        if (!CanMakeVampire(target))
            return false;

        if (!_mind.TryGetMind(target, out var mindId, out var mind))
            return false;

        _role.MindAddRole(mindId, VampireMindRoleId, mind, silent: true);
        EnsureComp<VampireComponent>(target);

        TryAddVampireObjective((mindId, mind), VampireKillObjectiveId);
        TryAddVampireObjective((mindId, mind), VampireDrainObjectiveId);
        TryAddRandomVampireObjective((mindId, mind), VampireStateObjectiveGroupId);
        TryAddRandomVampireObjective((mindId, mind), VampireStealObjectiveGroupId);

        var briefing = Loc.GetString("vampire-role-greeting");
        _antag.SendBriefing(target, briefing, Color.Yellow, VampireBriefingSound);
        return true;
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
                ? (float)comp.UpdateDelay.TotalSeconds
                : MathF.Max(0f, (float)(now - comp.LastUpdate).TotalSeconds);

            comp.LastUpdate = now;
            comp.NextUpdate = now + comp.UpdateDelay;

            var ent = (uid, comp);
            ProcessBloodDecay(ent, elapsed);
            HandleHolyWater(ent);
            HandleHolyPlace(ent);
        }

        ProcessActiveRejuvenation(now);
    }

    private void ProcessBloodDecay(Entity<VampireComponent> ent, float elapsed)
    {
        var before = ent.Comp.BloodFullness;
        var wasStarving = before <= 0f;

        if (before > 0f && _gameTicker.RunLevel < GameRunLevel.PostRound)
        {
            ent.Comp.StarvationDrunkBloodDrainAccumulator = 0f;
            ent.Comp.BloodFullness = MathF.Max(0f, before - ent.Comp.FullnessDecayPerSecond * elapsed);

            if (!MathHelper.CloseTo(ent.Comp.BloodFullness, before))
            {
                DirtyField(ent, ent.Comp, nameof(VampireComponent.BloodFullness));
                UpdateVampireFedAlert(ent);
            }
        }

        var isStarving = ent.Comp.BloodFullness <= 0f;
        if (wasStarving != isStarving)
            _movementSpeed.RefreshMovementSpeedModifiers(ent.Owner);

        // Расходуем кровь при голоде.
        if (!(ent.Comp.BloodFullness <= 0f) || ent.Comp.StarvationDrunkBloodDrainPerSecond <= 0 ||
            ent.Comp.DrunkBlood <= 0)
            return;

        ent.Comp.StarvationDrunkBloodDrainAccumulator += ent.Comp.StarvationDrunkBloodDrainPerSecond * elapsed;
        var drained = Math.Min(ent.Comp.DrunkBlood, (int)ent.Comp.StarvationDrunkBloodDrainAccumulator);
        if (drained <= 0)
            return;

        ent.Comp.StarvationDrunkBloodDrainAccumulator -= drained;
        TrySpendBlood(ent, drained, showPopup: false);
    }

    private void ApplyPowerLevelSettings(Entity<VampireComponent> ent)
    {
        if (!TryGetPowerLevelPrototype(ent.Comp.PowerLevel, out var level) ||
            !TryComp<VampireFeedingComponent>(ent, out var feeding))
        {
            return;
        }

        var fangs = level.Fangs;

        ent.Comp.MaxBloodFullness = level.MaxBloodFullness;
        ent.Comp.FullnessDecayPerSecond = level.FullnessDecayPerSecond;
        ent.Comp.BloodFullness = MathF.Min(ent.Comp.BloodFullness, ent.Comp.MaxBloodFullness);

        feeding.SipInterval = fangs.SipInterval;
        feeding.BloodGainPerSip = fangs.BloodGain;
        feeding.TargetBloodDrainPerSip = fangs.TargetBloodDrain;
        feeding.AnimalEfficiency = fangs.AnimalEfficiency;
        feeding.CorpseEfficiency = fangs.CorpseEfficiency;
        feeding.BiteDamage = new DamageSpecifier(fangs.BiteDamage);
        feeding.BiteBleedAmount = fangs.BleedAmount;
        feeding.BiteDistanceThreshold = fangs.Range;
        feeding.MaxBloodPerTarget = fangs.MaxBloodPerTarget;
        feeding.Healing = new DamageSpecifier(fangs.Healing);

        DirtyField(ent, ent.Comp, nameof(VampireComponent.MaxBloodFullness));
        DirtyField(ent, ent.Comp, nameof(VampireComponent.BloodFullness));
        UpdateVampireFedAlert(ent);
    }

    private bool TryGetPowerLevelPrototype(
        VampirePowerLevel powerLevel,
        out VampirePowerLevelPrototype prototype)
    {
        foreach (var candidate in _prototype.EnumeratePrototypes<VampirePowerLevelPrototype>())
        {
            if (candidate.Level != powerLevel)
                continue;

            prototype = candidate;
            return true;
        }

        _sawmill?.Error($"Missing vampire power level prototype for {powerLevel}");
        prototype = null!;
        return false;
    }

    private void RefreshAllActions(Entity<VampireComponent> ent)
    {
        if (!TryComp<VampireActionStateComponent>(ent, out var actionState))
            return;

        foreach (var (actionId, actionEntity) in actionState.Actions)
        {
            TryRefreshVampireAction(ent, actionId, actionEntity);
        }
    }

    private void OnActionsComponentStartup(Entity<ActionsComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp(ent, out VampireComponent? vampire))
            return;

        SyncVampireActions((ent.Owner, vampire));
    }

    private void SyncVampireActions(Entity<VampireComponent> ent)
    {
        if (!TryComp<VampireActionStateComponent>(ent, out var actionState))
            return;

        CleanMissingActions(actionState);
        EnsureRejuvenateUpgrade(ent);
        RefreshAllActions(ent);
    }

    private void CleanMissingActions(VampireActionStateComponent actionState)
    {
        if (actionState.Actions.Count == 0)
            return;

        _missingActions.Clear();
        foreach (var (actionId, actionEntity) in actionState.Actions)
        {
            if (Exists(actionEntity))
                continue;

            _missingActions.Add(actionId);
        }

        foreach (var actionId in _missingActions)
        {
            actionState.Actions.Remove(actionId);
        }
    }

    private void OnStartup(Entity<VampireComponent> ent, ref ComponentStartup args)
    {
        var actionState = EnsureComp<VampireActionStateComponent>(ent);
        EnsureComp<VampireFeedingComponent>(ent);
        EnsureComp<VampireHolyComponent>(ent);
        SetVampireMetabolism(ent.Owner, enabled: true);
        UpdatePowerLevel(ent, syncActions: false);
        ApplyPowerLevelSettings(ent);

        foreach (var actionId in BaseVampireActions)
        {
            EntityUid? action = null;

            _actions.AddAction(ent.Owner, ref action, actionId, ent.Owner);

            if (action is not null)
                actionState.Actions[actionId] = action.Value;
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
        SetVampireMetabolism(ent.Owner, enabled: false);

        if (TryComp<VampireActionStateComponent>(ent, out var actionState))
        {
            foreach (var action in actionState.Actions.Values)
            {
                _actions.RemoveAction(ent.Owner, action);
            }

            actionState.Actions.Clear();
        }

        RemCompDeferred<VampireActionStateComponent>(ent);
        RemCompDeferred<VampireFeedingComponent>(ent);
        RemCompDeferred<VampireHolyComponent>(ent);
        RemCompDeferred<ActiveVampireRejuvenateComponent>(ent);
    }

    private void SetVampireMetabolism(EntityUid uid, bool enabled)
    {
        if (!TryComp<BodyComponent>(uid, out var body))
            return;

        var ev = new SetVampireMetabolismEvent(enabled);
        _body.RelayEvent((uid, body), ref ev);
    }

    private void OnSetVampireMetabolism(
        Entity<MetabolizerComponent> ent,
        ref BodyRelayedEvent<SetVampireMetabolismEvent> args)
    {
        if (args.Args.Enabled)
            _metabolizer.TryAddMetabolizerType(ent, VampireMetabolizerTypeId);
        else
            _metabolizer.TryRemoveMetabolizerType(ent, VampireMetabolizerTypeId);
    }

    partial void UpdateVampireAlert(EntityUid uid);
    partial void UpdateVampireFedAlert(Entity<VampireComponent> ent);

    private void TryRefreshVampireAction(
        Entity<VampireComponent> ent,
        EntProtoId actionId,
        EntityUid? actionEntity)
    {
        if (actionEntity is null)
            return;

        if (_actions.GetAction(actionEntity) is not { } action)
            return;

        if (!TryComp<VampireActionComponent>(actionEntity.Value, out var vac))
        {
            _actions.SetEnabled(action.AsNullable(), true);
            return;
        }

        ConfigureVampireAction(ent, actionId, actionEntity.Value);

        var enabled = ent.Comp.PowerLevel >= vac.RequiredPowerLevel;

        _actions.SetEnabled(action.AsNullable(), enabled);
    }

    private void ConfigureVampireAction(
        Entity<VampireComponent> ent,
        EntProtoId actionId,
        EntityUid actionEntity,
        VampireActionChargeState? previousChargeState = null)
    {
        if (!TryGetPowerLevelPrototype(ent.Comp.PowerLevel, out var level))
            return;

        VampireActionChargeSettings? chargeSettings = null;

        if (actionId == VampireFangsActionId)
        {
            _actions.SetUseDelay(actionEntity, TimeSpan.FromSeconds(2));
        }
        else if (actionId == VampireGlareActionId)
        {
            chargeSettings = level.Glare.Action;
        }
        else if (actionId == VampireSleepActionId)
        {
            chargeSettings = level.Sleep.Action;

            if (TryComp<VampireActionComponent>(actionEntity, out var vampireAction))
                vampireAction.BloodCost = level.Sleep.BloodCost;

            _actions.SetRange(actionEntity, level.Sleep.TargetRange);
        }
        else if (actionId == VampireRejuvenateIActionId ||
                 actionId == VampireRejuvenateIiActionId)
        {
            chargeSettings = level.Rejuvenation.Action;
        }

        if (chargeSettings is null)
            return;

        _actions.SetUseDelay(actionEntity, chargeSettings.UseDelay);
        ConfigureActionCharges(actionEntity, chargeSettings, previousChargeState);
    }

    private void ConfigureActionCharges(
        EntityUid actionEntity,
        VampireActionChargeSettings settings,
        VampireActionChargeState? previousChargeState = null)
    {
        if (!TryComp<LimitedChargesComponent>(actionEntity, out var charges) ||
            !TryComp<AutoRechargeComponent>(actionEntity, out var recharge))
        {
            return;
        }

        var previous = previousChargeState ?? CaptureActionChargeState(actionEntity);
        var addedCapacity = Math.Max(0, settings.MaxCharges - previous.MaxCharges);
        var newCharges = Math.Clamp(previous.CurrentCharges + addedCapacity, 0, settings.MaxCharges);

        _charges.SetMaxCharges((actionEntity, charges), settings.MaxCharges);
        _charges.SetCharges((actionEntity, charges), newCharges);
        _charges.SetRechargeDuration(
            (actionEntity, charges, recharge),
            settings.RechargeDuration,
            newCharges < settings.MaxCharges ? previous.RechargeProgress : 0f);
    }

    private VampireActionChargeState CaptureActionChargeState(EntityUid actionEntity)
    {
        if (!TryComp<LimitedChargesComponent>(actionEntity, out var charges) ||
            !TryComp<AutoRechargeComponent>(actionEntity, out var recharge))
        {
            return new VampireActionChargeState(0, 0, 0f);
        }

        var currentCharges = _charges.GetCurrentCharges((actionEntity, charges, recharge));
        var progress = 0f;

        if (currentCharges >= charges.MaxCharges || recharge.RechargeDuration <= TimeSpan.Zero)
            return new VampireActionChargeState(currentCharges, charges.MaxCharges, progress);

        var elapsed = _timing.CurTime - charges.LastUpdate;
        var elapsedTicks = Math.Max(0L, elapsed.Ticks % recharge.RechargeDuration.Ticks);
        progress = Math.Clamp((float)elapsedTicks / recharge.RechargeDuration.Ticks, 0f, 1f);

        return new VampireActionChargeState(currentCharges, charges.MaxCharges, progress);
    }

    private readonly record struct VampireActionChargeState(
        int CurrentCharges,
        int MaxCharges,
        float RechargeProgress);

    private readonly record struct SetVampireMetabolismEvent(bool Enabled);

    private VampirePowerLevel GetRequiredPowerLevel(EntProtoId actionId)
    {
        if (_prototype.TryIndex<EntityPrototype>(actionId, out var proto) &&
            proto.TryGetComponent<VampireActionComponent>(out var vac, _componentFactory))
            return vac.RequiredPowerLevel;

        return VampirePowerLevel.Neonate;
    }

    private void EnsureRejuvenateUpgrade(Entity<VampireComponent> ent)
    {
        if (!TryComp<VampireActionStateComponent>(ent, out var actionState))
            return;

        var requiredPowerLevel = GetRequiredPowerLevel(VampireRejuvenateIiActionId);
        if (ent.Comp.PowerLevel < requiredPowerLevel)
            return;

        VampireActionChargeState? previousChargeState = null;
        if (actionState.Actions.TryGetValue(VampireRejuvenateIActionId, out var firstAction))
            previousChargeState = CaptureActionChargeState(firstAction);

        if (!actionState.Actions.ContainsKey(VampireRejuvenateIiActionId))
        {
            EntityUid? action = null;
            _actions.AddAction(ent.Owner, ref action, VampireRejuvenateIiActionId, ent.Owner);
            if (action is not null)
            {
                actionState.Actions[VampireRejuvenateIiActionId] = action.Value;
                ConfigureVampireAction(ent, VampireRejuvenateIiActionId, action.Value, previousChargeState);
            }
        }

        if (actionState.Actions.TryGetValue(VampireRejuvenateIiActionId, out var secondAction))
            TryRefreshVampireAction(ent, VampireRejuvenateIiActionId, secondAction);

        if (!actionState.Actions.TryGetValue(VampireRejuvenateIActionId, out firstAction))
            return;

        _actions.RemoveAction(ent.Owner, firstAction);
        actionState.Actions.Remove(VampireRejuvenateIActionId);
    }

    private void HandleHolyWater(Entity<VampireComponent> ent)
    {
        if (!TryComp<VampireFeedingComponent>(ent, out var feeding) ||
            !TryComp<VampireHolyComponent>(ent, out var holy) ||
            feeding.UniqueHumanoidVictims < 1)
        {
            return;
        }

        if (_timing.CurTime < holy.NextHolyWaterEffect)
            return;

        var holywater = _solution.GetTotalPrototypeQuantity(ent.Owner, holy.HolyWaterReagent);
        if (holywater <= FixedPoint2.Zero)
            return;

        if (TryComp(ent, out MobStateComponent? mobState) && mobState.CurrentState == Shared.Mobs.MobState.Dead)
            return;

        holy.NextHolyWaterEffect = _timing.CurTime + holy.EffectInterval;

        if (ent.Comp.DrunkBlood > 0)
        {
            TrySpendBlood(ent, Math.Min(3, ent.Comp.DrunkBlood), showPopup: false);

            ApplyDistributedDamage(ent.Owner, BruteDamageTypes, 3f);

            if (TryComp(ent, out StaminaComponent? stamina))
                _stamina.TakeStaminaDamage(ent.Owner, 5f, stamina);

            return;
        }

        ApplyDistributedDamage(ent.Owner, BurnDamageTypes, 2f);
    }

    private void HandleHolyPlace(Entity<VampireComponent> ent)
    {
        if (!TryComp<VampireFeedingComponent>(ent, out var feeding) ||
            !TryComp<VampireHolyComponent>(ent, out var holy) ||
            feeding.UniqueHumanoidVictims < 1)
        {
            return;
        }

        if (_timing.CurTime < holy.NextHolyPlaceEffect)
            return;

        if (!IsInHolyPlace(ent, holy.HolyPlaceRange))
            return;

        if (TryComp(ent, out MobStateComponent? mobState) && mobState.CurrentState == Shared.Mobs.MobState.Dead)
            return;

        holy.NextHolyPlaceEffect = _timing.CurTime + holy.EffectInterval;

        if (_timing.CurTime >= holy.NextHolyPlacePopup)
        {
            _popup.PopupEntity(Loc.GetString("vampire-holy-place-burn"), ent.Owner, ent.Owner, PopupType.MediumCaution);
            holy.NextHolyPlacePopup = _timing.CurTime + TimeSpan.FromSeconds(5);
        }

        if (!_prototype.TryIndex(HeatTypeId, out var heat))
            return;

        var spec = new DamageSpecifier(heat, FixedPoint2.New(3f));
        _damageable.TryChangeDamage(ent.Owner, spec, true);
    }

    private bool IsInHolyPlace(EntityUid uid, float range)
    {
        if (_container.IsEntityInContainer(uid))
            return false;

        var coords = Transform(uid).Coordinates;
        return (from target in _lookup.GetEntitiesInRange(coords, range, LookupFlags.Static)
            where target != uid
            where HasComp<PrayableComponent>(target)
            where Transform(target).Anchored
            select target).Any(target => _interaction.InRangeUnobstructed(uid, target, range));
    }

    private void ApplyDistributedDamage(
        EntityUid uid,
        ReadOnlySpan<ProtoId<DamageTypePrototype>> damageTypes,
        FixedPoint2 amount)
    {
        var damage = CreateDistributedDamage(damageTypes, amount);
        _damageable.TryChangeDamage(uid, damage, true);
    }

    private static DamageSpecifier CreateDistributedDamage(
        ReadOnlySpan<ProtoId<DamageTypePrototype>> damageTypes,
        FixedPoint2 amount)
    {
        var damage = new DamageSpecifier();
        var remaining = amount;

        for (var i = 0; i < damageTypes.Length; i++)
        {
            var value = remaining / FixedPoint2.New(damageTypes.Length - i);
            damage.DamageDict.Add(damageTypes[i], value);
            remaining -= value;
        }

        return damage;
    }

    private void TryAddVampireObjective(Entity<MindComponent> mind, EntProtoId objective)
    {
        if (_mind.TryAddObjective(mind, mind.Comp, objective))
            return;

        _sawmill?.Error(
            $"Failed to add vampire objective {objective} to {ToPrettyString(mind.Comp.OwnedEntity)}");
    }

    private void TryAddRandomVampireObjective(
        Entity<MindComponent> mind,
        ProtoId<WeightedRandomPrototype> objectiveGroup)
    {
        if (_objectives.GetRandomObjective(
                mind,
                mind.Comp,
                objectiveGroup,
                VampireObjectiveMaxDifficulty) is { } objective)
        {
            _mind.AddObjective(mind, mind.Comp, objective);
            return;
        }

        _sawmill?.Error(
            $"Failed to select vampire objective from group {objectiveGroup} for {ToPrettyString(mind.Comp.OwnedEntity)}");
    }
}

using Content.Server.Bible.Components;
using Content.Server._Sunrise.Antags.Vampires.Components;
using Content.Shared._Sunrise.Antags.Vampires;
using Content.Shared._Sunrise.Antags.Vampires.Events;
using Content.Shared._Sunrise.Antags.Vampires.Components;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;
using Content.Shared.Nutrition.Components;
using Content.Shared.Stunnable;
using System.Linq;
using System.Numerics;
using Content.Shared.Popups;
using Content.Shared.Bed.Sleep;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Mindshield.Components;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Flash;
using Robust.Shared.Audio;
using Robust.Server.Audio;
using Robust.Server.Containers;
using Robust.Shared.Random;

namespace Content.Server._Sunrise.Antags.Vampires.Systems;

public sealed partial class VampireSystem
{
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly BlindableSystem _blindable = default!;

    private static readonly SoundSpecifier BiteSound = new SoundPathSpecifier("/Audio/Effects/bite.ogg");
    private static readonly string[] MouthCoveringSlots = ["mask", "head"];
    private static readonly LocId VampirePowerAwakenedMessage = "vampire-power-awakened-message";
    private static readonly LocId VampirePowerNightbornMessage = "vampire-power-nightborn-message";
    private static readonly LocId VampirePowerAncientMessage = "vampire-power-ancient-message";

    // Антимета
    private static readonly LocId[] SleepTargetPopupIds =
    [
        "vampire-sleep-target-warning-1",
        "vampire-sleep-target-warning-2",
        "vampire-sleep-target-warning-3",
        "vampire-sleep-target-warning-4",
        "vampire-sleep-target-warning-5",
    ];

    private void InitializeAbilities()
    {
        SubscribeLocalEvent<VampireComponent, VampireToggleFangsActionEvent>(OnToggleFangs);

        SubscribeLocalEvent<VampireComponent, VampireGlareActionEvent>(OnGlare);

        SubscribeLocalEvent<VampireComponent, VampireSleepActionEvent>(OnSleep);
        SubscribeLocalEvent<VampireComponent, DoAfterAttemptEvent<VampireSleepDoAfterEvent>>(OnSleepDoAfterAttempt);
        SubscribeLocalEvent<VampireComponent, VampireSleepDoAfterEvent>(OnSleepDoAfter);

        SubscribeLocalEvent<VampireComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<VampireComponent, BeforeInteractHandEvent>(OnBeforeInteractHand);
        SubscribeLocalEvent<VampireComponent, VampireDrinkBloodDoAfterEvent>(OnDrinkDoAfter);

        SubscribeLocalEvent<VampireComponent, VampireRejuvenateIActionEvent>(OnRejuvenateI);
        SubscribeLocalEvent<VampireComponent, VampireRejuvenateIiActionEvent>(OnRejuvenateII);
    }

    #region Вспомогательное

    internal bool CheckAndConsumeBloodCost(Entity<VampireComponent> ent,
        EntityUid? actionEntity = null,
        int bloodCost = 0)
    {
        if (!TryResolveVampireActionCost(ent, actionEntity, bloodCost, out var resolvedCost))
            return false;

        if (!CanSpendBlood(ent, resolvedCost))
            return false;

        return TrySpendBlood(ent, resolvedCost);
    }

    internal bool CanSpendBlood(Entity<VampireComponent> ent, int bloodCost, bool showPopup = true)
    {
        if (bloodCost <= 0)
            return true;

        if (ent.Comp.DrunkBlood >= bloodCost)
            return true;

        if (showPopup)
            _popup.PopupEntity(Loc.GetString("vampire-not-enough-blood"), ent.Owner, ent.Owner);

        return false;
    }

    internal bool TrySpendBlood(Entity<VampireComponent> ent, int bloodCost, bool showPopup = true)
    {
        if (!CanSpendBlood(ent, bloodCost, showPopup))
            return false;

        if (bloodCost <= 0)
            return true;

        ent.Comp.DrunkBlood -= bloodCost;
        DirtyField(ent, ent.Comp, nameof(VampireComponent.DrunkBlood));
        UpdateVampireAlert(ent.Owner);
        return true;
    }

    internal int AddBlood(
        Entity<VampireComponent> ent,
        float amount,
        EntityUid? target = null,
        bool countTotalBlood = true,
        bool recordTarget = true)
    {
        if (amount <= 0f || !TryComp<VampireFeedingComponent>(ent, out var feeding))
            return 0;

        var storedAmount = amount + feeding.DrunkBloodRemainder;
        var integerAmount = Math.Max(0, (int)storedAmount);
        feeding.DrunkBloodRemainder = storedAmount - integerAmount;
        var wasStarving = ent.Comp.BloodFullness <= 0f;

        if (integerAmount > 0)
        {
            ent.Comp.DrunkBlood += integerAmount;
            DirtyField(ent, ent.Comp, nameof(VampireComponent.DrunkBlood));
        }

        var totalBloodAdded = 0;
        if (countTotalBlood)
        {
            var totalAmount = amount + feeding.TotalBloodRemainder;
            totalBloodAdded = Math.Max(0, (int)totalAmount);
            feeding.TotalBloodRemainder = totalAmount - totalBloodAdded;
            ent.Comp.TotalBlood += totalBloodAdded;
        }

        if (recordTarget && target is { } targetUid)
        {
            var isNewTarget = !feeding.BloodDrunkFromTargets.TryGetValue(targetUid, out var targetBlood);
            feeding.BloodDrunkFromTargets[targetUid] = targetBlood + amount;

            if (isNewTarget && countTotalBlood)
                feeding.UniqueHumanoidVictims++;
        }

        ent.Comp.BloodFullness = MathF.Min(ent.Comp.MaxBloodFullness, ent.Comp.BloodFullness + amount);
        DirtyField(ent, ent.Comp, nameof(VampireComponent.BloodFullness));

        var isStarving = ent.Comp.BloodFullness <= 0f;
        if (wasStarving != isStarving)
            _movementSpeed.RefreshMovementSpeedModifiers(ent.Owner);

        UpdateVampireAlert(ent.Owner);
        UpdateVampireFedAlert(ent);

        if (totalBloodAdded > 0)
            UpdatePowerLevel(ent);

        return integerAmount;
    }

    private bool TryResolveVampireActionCost(
        Entity<VampireComponent> ent,
        EntityUid? actionEntity,
        int bloodCost,
        out int resolvedCost,
        bool showPopup = true)
    {
        resolvedCost = Math.Max(0, bloodCost);

        if (actionEntity is not { } action)
            return true;

        if (!Exists(action))
            return false;

        if (!TryComp<VampireActionComponent>(action, out var vac))
            return true;

        if (ent.Comp.PowerLevel < vac.RequiredPowerLevel)
        {
            if (showPopup)
                _popup.PopupEntity(Loc.GetString("action-vampire-not-enough-power"), ent.Owner, ent.Owner);

            return false;
        }

        if (resolvedCost <= 0 && vac.BloodCost > 0)
            resolvedCost = vac.BloodCost;

        return true;
    }

    internal bool IsProtectedByFaith(EntityUid target)
        => HasComp<BibleUserComponent>(target);

    private bool IsInvalidDrinkTarget(EntityUid user, EntityUid target, bool showPopup = true)
    {
        if (!HasComp<VampireComponent>(target))
            return false;

        if (showPopup)
            _popup.PopupEntity(Loc.GetString("vampire-drink-invalid-target"), user, user, PopupType.MediumCaution);

        return true;
    }

    #endregion

    #region Способности

    private void OnToggleFangs(Entity<VampireComponent> ent, ref VampireToggleFangsActionEvent args)
    {
        if (args.Handled)
            return;

        ent.Comp.FangsExtended = !ent.Comp.FangsExtended;
        if (!ent.Comp.FangsExtended && TryComp<VampireFeedingComponent>(ent, out var feeding))
            feeding.IsDrinking = false;

        if (TryComp<VampireActionStateComponent>(ent, out var actionState) &&
            actionState.Actions.TryGetValue(VampireFangsActionId, out var actionEntity) &&
            _actions.GetAction(actionEntity) is { } action)
        {
            _actions.SetToggled(action.AsNullable(), ent.Comp.FangsExtended);
        }

        DirtyField(ent, ent.Comp, nameof(VampireComponent.FangsExtended));
        args.Handled = true;
    }

    private void OnAfterInteract(Entity<VampireComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || !ent.Comp.FangsExtended || !Exists(args.Target))
            return;

        var target = args.Target.Value;

        if (target == ent.Owner || !HasComp<BloodstreamComponent>(target))
            return;

        if (TryStartDrinkBlood(ent, target))
            args.Handled = true;
    }

    private void OnBeforeInteractHand(Entity<VampireComponent> ent, ref BeforeInteractHandEvent args)
    {
        if (args.Handled || !ent.Comp.FangsExtended)
            return;

        var target = args.Target;
        if (!Exists(target) || target == ent.Owner)
            return;

        if (!TryComp<BloodstreamComponent>(target, out var bloodstream))
        {
            if (HasComp<InteractionPopupComponent>(target))
                args.Handled = true;
            return;
        }

        if (!HasBloodToDrink((target, bloodstream)))
            return;

        args.Handled = true;
        TryStartDrinkBlood(ent, target);
    }

    private bool TryStartDrinkBlood(Entity<VampireComponent> ent, EntityUid target)
    {
        if (IsInvalidDrinkTarget(ent.Owner, target))
            return false;

        if (IsProtectedByFaith(target) && ent.Comp.PowerLevel < VampirePowerLevel.Ancient)
        {
            _popup.PopupEntity(
                Loc.GetString("vampire-target-protected-by-faith"),
                ent.Owner,
                ent.Owner,
                PopupType.MediumCaution);
            return false;
        }

        if (!TryComp<BloodstreamComponent>(target, out var bloodstream) ||
            !HasBloodToDrink((target, bloodstream)))
        {
            _popup.PopupEntity(
                Loc.GetString("vampire-drink-target-empty"),
                ent.Owner,
                ent.Owner,
                PopupType.MediumCaution);
            return false;
        }

        if (!IsMouthBlocked(ent.Owner))
            return StartDrinkDoAfter(ent, target, showPopup: true);

        _popup.PopupEntity(Loc.GetString("vampire-mouth-covered"), ent.Owner, ent.Owner);
        return false;
    }

    private void OnDrinkDoAfter(Entity<VampireComponent> ent, ref VampireDrinkBloodDoAfterEvent args)
    {
        if (args.Handled || !TryComp<VampireFeedingComponent>(ent, out var feeding))
            return;

        if (args.Cancelled || !ent.Comp.FangsExtended || args.Args.Target is not { } targetUid
            || !HasComp<BloodstreamComponent>(targetUid)
            || IsInvalidDrinkTarget(ent.Owner, targetUid, showPopup: false))
        {
            feeding.IsDrinking = false;
            return;
        }

        if (!feeding.BloodDrunkFromTargets.TryGetValue(targetUid, out var drunkFromTarget))
            drunkFromTarget = 0;

        if (drunkFromTarget >= feeding.MaxBloodPerTarget)
        {
            _popup.PopupEntity(Loc.GetString("vampire-drink-target-maxed", ("amount", feeding.MaxBloodPerTarget)),
                ent.Owner,
                ent.Owner,
                PopupType.MediumCaution);
            feeding.IsDrinking = false;
            return;
        }

        var targetIsHumanoid = HasComp<HumanoidProfileComponent>(targetUid);
        var bloodEfficiency = targetIsHumanoid ? 1f : feeding.AnimalEfficiency;

        if (TryComp<MobStateComponent>(targetUid, out var mobState) &&
            mobState.CurrentState == Shared.Mobs.MobState.Dead)
        {
            bloodEfficiency *= feeding.CorpseEfficiency;
        }

        if (TryComp<PerishableComponent>(targetUid, out var rot))
        {
            var stage = Math.Clamp(rot.Stage, 0, 4);
            bloodEfficiency *= feeding.RotEfficiencyByStage.GetValueOrDefault(stage);
        }

        if (bloodEfficiency <= 0f)
        {
            _popup.PopupEntity(Loc.GetString("vampire-drink-target-rot"), ent.Owner, ent, PopupType.MediumCaution);
            feeding.IsDrinking = false;
            return;
        }

        var maxCanDrink = feeding.MaxBloodPerTarget - drunkFromTarget;
        var fullSipGain = feeding.BloodGainPerSip * bloodEfficiency;
        var cappedSipGain = MathF.Min(fullSipGain, maxCanDrink);
        if (cappedSipGain <= 0f ||
            feeding.TargetBloodDrainPerSip <= 0f ||
            !TryComp<BloodstreamComponent>(targetUid, out var blood))
        {
            feeding.IsDrinking = false;
            _popup.PopupEntity(Loc.GetString("vampire-drink-target-empty"), ent.Owner, ent, PopupType.MediumCaution);
            return;
        }

        var targetBloodLevel =
            _blood.GetBloodLevel(targetUid) * blood.BloodReferenceSolution.MaxVolume.Value / 100;
        if (targetBloodLevel <= 0f)
        {
            feeding.IsDrinking = false;
            _popup.PopupEntity(Loc.GetString("vampire-drink-target-empty"), ent.Owner, ent, PopupType.MediumCaution);
            return;
        }

        var intendedDrain = feeding.TargetBloodDrainPerSip * (cappedSipGain / fullSipGain);
        var actualDrain = MathF.Min(intendedDrain, targetBloodLevel);
        var actualSipGain = cappedSipGain * (actualDrain / intendedDrain);

        if (_blood.TryModifyBloodLevel(targetUid, -actualDrain))
        {
            AddBlood(ent, actualSipGain, targetUid, countTotalBlood: targetIsHumanoid);

            _damageable.TryChangeDamage(targetUid, feeding.BiteDamage, ignoreResistances: true);
            _blood.TryModifyBleedAmount(targetUid, feeding.BiteBleedAmount);

            if (TryComp<BlindableComponent>(targetUid, out var blindable))
            {
                var biteCount = feeding.BiteCountsByTarget.GetValueOrDefault(targetUid) + 1;
                if (biteCount >= 3)
                {
                    _blindable.AdjustEyeDamage((targetUid, blindable), 1);
                    biteCount = 0;
                }

                feeding.BiteCountsByTarget[targetUid] = biteCount;
            }

            var healingScale = actualSipGain / feeding.BloodGainPerSip;
            _damageable.TryChangeDamage(ent.Owner, feeding.Healing * healingScale, true);

            _audio.PlayPvs(BiteSound, targetUid, AudioParams.Default.WithVolume(-7f));
            var targetCoords = Transform(targetUid).Coordinates;
            Spawn("WeaponArcBite", targetCoords);

            var currentDrunkFromTarget = feeding.BloodDrunkFromTargets.GetValueOrDefault(targetUid, 0);
            if (ent.Comp.FangsExtended && currentDrunkFromTarget < feeding.MaxBloodPerTarget)
            {
                feeding.IsDrinking = false;
                StartDrinkDoAfter(ent, targetUid, showPopup: false);
            }
            else
            {
                feeding.IsDrinking = false;
                if (currentDrunkFromTarget >= feeding.MaxBloodPerTarget)
                {
                    _popup.PopupEntity(
                        Loc.GetString("vampire-drink-target-hard-max", ("amount", feeding.MaxBloodPerTarget)),
                        ent.Owner,
                        ent,
                        PopupType.MediumCaution);
                }
            }
        }
        else
        {
            feeding.IsDrinking = false;
            _popup.PopupEntity(Loc.GetString("vampire-drink-target-empty"), ent.Owner, ent, PopupType.MediumCaution);
        }
    }

    partial void UpdateVampireAlert(EntityUid uid)
        => _alerts.ShowAlert(uid, "VampireBlood");

    partial void UpdateVampireFedAlert(Entity<VampireComponent> ent)
    {
        var frac = ent.Comp.MaxBloodFullness <= 0f ? 0f : ent.Comp.BloodFullness / ent.Comp.MaxBloodFullness;
        var sev = (short)Math.Clamp((int)MathF.Ceiling(frac * 4f) + 1, 1, 5);
        _alerts.ShowAlert(ent.Owner, "VampireFed", sev);
    }

    private bool StartDrinkDoAfter(Entity<VampireComponent> ent, EntityUid target, bool showPopup)
    {
        if (!TryComp<VampireFeedingComponent>(ent, out var feeding) || feeding.IsDrinking)
            return false;

        if (IsMouthBlocked(ent.Owner))
        {
            if (showPopup)
                _popup.PopupEntity(Loc.GetString("vampire-mouth-covered"), ent.Owner, ent.Owner);
            return false;
        }

        var dargs = new DoAfterArgs(EntityManager,
            ent.Owner,
            feeding.SipInterval,
            new VampireDrinkBloodDoAfterEvent(),
            ent.Owner,
            target)
        {
            DistanceThreshold = feeding.BiteDistanceThreshold,
            BreakOnDamage = true,
            BreakOnHandChange = true,
            BreakOnMove = true,
            BreakOnWeightlessMove = true,
            BlockDuplicate = true,
            CancelDuplicate = true,
            AttemptFrequency = AttemptFrequency.StartAndEnd
        };

        if (!_doAfter.TryStartDoAfter(dargs))
            return false;

        feeding.IsDrinking = true;
        if (showPopup)
        {
            _popup.PopupEntity(Loc.GetString("vampire-drink-start", ("target", Identity.Entity(target, EntityManager))),
                ent.Owner,
                ent.Owner);
        }

        return true;
    }

    private void OnSleep(Entity<VampireComponent> ent, ref VampireSleepActionEvent args)
    {
        if (args.Handled || !Exists(args.Target))
            return;

        var actionEntity = args.Action.Owner;
        if (!TryGetPowerLevelPrototype(ent.Comp.PowerLevel, out var level))
            return;

        var settings = level.Sleep;
        var target = args.Target;

        if (target == ent.Owner ||
            !_interaction.InRangeAndAccessible(ent.Owner, target, settings.TargetRange))
        {
            return;
        }

        if (IsProtectedByFaith(target) && !settings.IgnoresFaith)
        {
            _popup.PopupEntity(Loc.GetString("vampire-target-protected-by-faith"),
                ent.Owner,
                ent.Owner,
                PopupType.MediumCaution);
            return;
        }

        if (HasFlashProtection(target))
        {
            _popup.PopupEntity(Loc.GetString("vampire-sleep-protected"), ent.Owner, ent.Owner, PopupType.MediumCaution);
            return;
        }

        if (HasComp<MindShieldComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("vampire-sleep-shielded"), ent.Owner, ent.Owner, PopupType.SmallCaution);
            return;
        }

        if (!CanSpendBlood(ent, settings.BloodCost))
            return;

        var doAfterEvent = new VampireSleepDoAfterEvent
        {
            Victim = GetNetEntity(target),
            Action = GetNetEntity(actionEntity),
            MaxDistance = settings.BreakRange,
            BloodCost = settings.BloodCost,
            Duration = settings.Duration,
            IgnoresFaith = settings.IgnoresFaith
        };
        var doAfter = new DoAfterArgs(EntityManager, ent.Owner, settings.ChannelTime, doAfterEvent, ent.Owner)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            BreakOnWeightlessMove = true,
            MovementThreshold = 0.1f,
            RequireCanInteract = true,
            BlockDuplicate = true,
            CancelDuplicate = true,
            AttemptFrequency = AttemptFrequency.EveryTick,
            Hidden = true
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
            return;

        _popup.PopupEntity(Loc.GetString(_random.Pick(SleepTargetPopupIds)), target, target, PopupType.SmallCaution);
        args.Handled = true;
    }

    private void OnSleepDoAfterAttempt(Entity<VampireComponent> ent,
        ref DoAfterAttemptEvent<VampireSleepDoAfterEvent> args)
    {
        var target = GetEntity(args.Event.Victim);
        if (!Exists(target) || !_interaction.InRangeAndAccessible(ent.Owner, target, args.Event.MaxDistance))
            args.Cancel();
    }

    private bool HasFlashProtection(EntityUid target)
    {
        var attempt = new FlashAttemptEvent(target, null, null);
        RaiseLocalEvent(target, ref attempt, true);
        return attempt.Cancelled;
    }

    private void OnSleepDoAfter(Entity<VampireComponent> ent, ref VampireSleepDoAfterEvent args)
    {
        if (args.Handled)
            return;

        if (args.Cancelled)
        {
            RefundSleepAction(args.Action);
            args.Handled = true;
            return;
        }

        var target = GetEntity(args.Victim);
        if (!Exists(target))
        {
            RefundSleepAction(args.Action);
            args.Handled = true;
            return;
        }

        if (IsProtectedByFaith(target) && !args.IgnoresFaith)
        {
            _popup.PopupEntity(Loc.GetString("vampire-target-protected-by-faith"),
                ent.Owner,
                ent.Owner,
                PopupType.MediumCaution);
            RefundSleepAction(args.Action);
            args.Handled = true;
            return;
        }

        if (HasFlashProtection(target))
        {
            _popup.PopupEntity(Loc.GetString("vampire-sleep-protected"), ent.Owner, ent.Owner, PopupType.MediumCaution);
            RefundSleepAction(args.Action);
            args.Handled = true;
            return;
        }

        if (HasComp<MindShieldComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("vampire-sleep-shielded"), ent.Owner, ent.Owner, PopupType.SmallCaution);
            RefundSleepAction(args.Action);
            args.Handled = true;
            return;
        }

        if (!CheckAndConsumeBloodCost(ent, null, args.BloodCost))
        {
            RefundSleepAction(args.Action);
            args.Handled = true;
            return;
        }

        _statusEffects.TryAddStatusEffectDuration(target, SleepingSystem.StatusEffectForcedSleeping, args.Duration);
        args.Handled = true;
    }

    private void RefundSleepAction(NetEntity netAction)
    {
        var action = GetEntity(netAction);
        if (!Exists(action) ||
            !TryComp<Content.Shared.Charges.Components.LimitedChargesComponent>(action, out var charges))
        {
            return;
        }

        TryComp<Content.Shared.Charges.Components.AutoRechargeComponent>(action, out var recharge);
        _charges.AddCharges((action, charges, recharge), 1);
        _actions.ClearCooldown(action);
    }

    private void OnGlare(Entity<VampireComponent> ent, ref VampireGlareActionEvent args)
    {
        if (TryComp<BlindableComponent>(ent, out var blindable) && blindable.IsBlind)
            return;

        if (args.Handled)
            return;

        if (!TryGetPowerLevelPrototype(ent.Comp.PowerLevel, out var level))
            return;

        if (!TryComp<VampireActionStateComponent>(ent, out var actionState) ||
            !actionState.Actions.TryGetValue(VampireGlareActionId, out var actionEntity))
            return;

        if (!CheckAndConsumeBloodCost(ent, actionEntity))
            return;

        var settings = level.Glare;
        var targets = _lookup.GetEntitiesInRange(ent.Owner,
            settings.Range,
            LookupFlags.Dynamic | LookupFlags.Sundries);

        var (ourPosition, ourRotation) = _transform.GetWorldPositionRotation(Transform(ent));
        var ourDirection = ourRotation.ToWorldVec();

        foreach (var target in targets)
        {
            if (target == ent.Owner)
                continue;

            var effectScale = HasFlashProtection(target)
                ? settings.FlashProtectionEffectScale
                : 1f;

            if (effectScale <= 0)
                continue;

            var offset = _transform.GetWorldPosition(target) - ourPosition;
            var dot = offset.LengthSquared() > 0f
                ? Vector2.Dot(ourDirection, Vector2.Normalize(offset))
                : 0f;

            if (!TryComp<StaminaComponent>(target, out var stam))
                continue;

            var knockedDown = HasComp<KnockedDownComponent>(target);

            switch (dot)
            {
                case > 0.7f when !knockedDown:
                    _stun.TryAddParalyzeDuration(target, settings.FrontParalyzeDuration * effectScale);

                    _stamina.TakeStaminaDamage(target, settings.StaminaDamage * effectScale, stam, source: ent.Owner);

                    TryInjectMuteToxin(target, settings.MuteToxinAmount * effectScale);
                    break;
                case < -0.7f when !knockedDown:
                    _stamina.TakeStaminaDamage(target,
                        settings.StaminaDamage * effectScale,
                        stam,
                        source: ent.Owner);
                    break;
                default:
                    _stun.TryAddParalyzeDuration(target, settings.SideParalyzeDuration * effectScale);

                    _stamina.TakeStaminaDamage(target,
                        settings.StaminaDamage * effectScale,
                        stam,
                        source: ent.Owner);
                    break;
            }
        }

        args.Handled = true;
    }

    private bool TryInjectMuteToxin(EntityUid target, float amount)
    {
        if (amount <= 0f)
            return false;

        var solution = new Solution();
        solution.AddReagent(MuteToxinReagentId, FixedPoint2.New(amount));

        if (!_solution.TryGetInjectableSolution(target, out var targetSolution, out _))
            return false;

        return _solution.TryAddSolution(targetSolution.Value, solution);
    }

    private void OnRejuvenateI(Entity<VampireComponent> ent, ref VampireRejuvenateIActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<VampireActionStateComponent>(ent, out var actionState) ||
            !actionState.Actions.TryGetValue(VampireRejuvenateIActionId, out var actionEntity))
            return;

        if (!CheckAndConsumeBloodCost(ent, actionEntity))
            return;

        RemoveRejuvenateStuns(ent.Owner);

        args.Handled = true;
    }

    private void OnRejuvenateII(Entity<VampireComponent> ent, ref VampireRejuvenateIiActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<VampireActionStateComponent>(ent, out var actionState) ||
            !actionState.Actions.TryGetValue(VampireRejuvenateIiActionId, out var actionEntity))
            return;

        if (!CheckAndConsumeBloodCost(ent, actionEntity))
            return;

        if (!TryGetPowerLevelPrototype(ent.Comp.PowerLevel, out var level))
            return;

        var settings = level.Rejuvenation;

        RemoveRejuvenateStuns(ent.Owner);
        PurgeRejuvenateReagents(ent.Owner, settings.ReagentPurgeAmount);
        StartRejuvenateHealing(ent.Owner, settings);

        args.Handled = true;
    }

    private void RemoveRejuvenateStuns(EntityUid uid)
    {
        _statusEffects.TryRemoveStatusEffect(uid, SharedStunSystem.StunId);
        _stun.TryUnstun(uid);
        RemComp<KnockedDownComponent>(uid);
    }

    private void PurgeRejuvenateReagents(EntityUid uid, float configuredAmount)
    {
        var purgeAmount = FixedPoint2.New(configuredAmount);
        if (purgeAmount <= FixedPoint2.Zero)
            return;

        if (!TryComp<BloodstreamComponent>(uid, out var blood))
            return;

        if (!_solution.ResolveSolution(uid,
                blood.BloodSolutionName,
                ref blood.BloodSolution,
                out var bloodstreamSolution))
            return;

        var solEnt = blood.BloodSolution.Value;
        var toRemove = FixedPoint2.Zero;

        foreach (var quant in bloodstreamSolution.Contents.ToArray())
        {
            if (toRemove >= purgeAmount)
                break;

            if (!_prototype.TryIndex<ReagentPrototype>(quant.Reagent.Prototype, out var proto))
                continue;

            if (proto.Metabolisms is null)
                continue;

            if (proto.Metabolisms.Metabolisms.Keys.All(k => k.Id != "Poison"))
                continue;

            var remaining = purgeAmount - toRemove;
            var removeAmt = FixedPoint2.Min(quant.Quantity, remaining);

            _solution.RemoveReagent(solEnt, quant.Reagent, removeAmt);
            toRemove += removeAmt;
        }
    }

    private void StartRejuvenateHealing(EntityUid uid, VampireRejuvenationLevelSettings settings)
    {
        if (settings.HealApplications <= 0 || settings.Healing.Empty)
            return;

        var active = EnsureComp<ActiveVampireRejuvenateComponent>(uid);
        active.ApplicationsRemaining = settings.HealApplications;
        active.ApplicationInterval = settings.HealInterval;
        active.NextApplication = _timing.CurTime;
        active.Healing = new DamageSpecifier(settings.Healing);
    }

    private void ProcessActiveRejuvenation(TimeSpan now)
    {
        var rejuvenateQuery = EntityQueryEnumerator<ActiveVampireRejuvenateComponent>();
        while (rejuvenateQuery.MoveNext(out var uid, out var rejuvenate))
        {
            if (now < rejuvenate.NextApplication)
                continue;

            _damageable.TryChangeDamage(uid, rejuvenate.Healing, true);
            rejuvenate.ApplicationsRemaining--;

            if (rejuvenate.ApplicationsRemaining <= 0)
            {
                RemCompDeferred<ActiveVampireRejuvenateComponent>(uid);
                continue;
            }

            rejuvenate.NextApplication = now + rejuvenate.ApplicationInterval;
        }
    }

    #endregion

    #region Прогрессия

    private void UpdatePowerLevel(Entity<VampireComponent> ent, bool syncActions = true)
    {
        var oldLevel = ent.Comp.PowerLevel;
        var newLevel = oldLevel;

        foreach (var prototype in _prototype.EnumeratePrototypes<VampirePowerLevelPrototype>())
        {
            if (prototype.Level > VampirePowerLevel.Ancient ||
                prototype.Level <= newLevel ||
                prototype.RequiredTotalBlood is not { } requiredTotalBlood ||
                ent.Comp.TotalBlood < requiredTotalBlood)
            {
                continue;
            }

            newLevel = prototype.Level;
        }

        if (newLevel == oldLevel)
            return;

        ent.Comp.PowerLevel = newLevel;
        ApplyPowerLevelSettings(ent);
        DirtyField(ent, ent.Comp, nameof(VampireComponent.PowerLevel));

        if (syncActions)
            SyncVampireActions(ent);

        LocId levelUpMessage;
        switch (newLevel)
        {
            case VampirePowerLevel.Awakened:
                levelUpMessage = VampirePowerAwakenedMessage;
                break;
            case VampirePowerLevel.Nightborn:
                levelUpMessage = VampirePowerNightbornMessage;
                break;
            case VampirePowerLevel.Ancient:
                levelUpMessage = VampirePowerAncientMessage;
                break;
            default:
                return;
        }

        _antag.SendBriefing(ent, Loc.GetString(levelUpMessage), Color.Crimson, null);
    }

    private bool IsMouthBlocked(EntityUid uid)
    {
        if (!HasComp<InventoryComponent>(uid))
            return false;

        foreach (var slot in MouthCoveringSlots)
        {
            if (_inventory.TryGetSlotEntity(uid, slot, out var ent) &&
                TryComp<IngestionBlockerComponent>(ent.Value, out var blocker) &&
                blocker.Enabled)

                return true;
        }

        return false;
    }

    #endregion
}

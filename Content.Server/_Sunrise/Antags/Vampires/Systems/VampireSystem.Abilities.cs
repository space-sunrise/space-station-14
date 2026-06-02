using Content.Server.Bible.Components;
using Content.Shared._Sunrise.Antags.Vampires.Events;
using Content.Shared._Sunrise.Antags.Vampires.UI;
using Content.Shared._Sunrise.Antags.Vampires.Components;
using Content.Shared._Sunrise.Antags.Vampires.Components.Abilities;
using Content.Shared._Sunrise.Antags.Vampires.Components.Effects;
using Content.Shared._Sunrise.Antags.Vampires.Components.Visuals;
using Content.Shared._Sunrise.Antags.Vampires.Prototypes;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Events;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Nutrition.Components;
using Content.Shared.Physics;
using Content.Shared.Stunnable;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Content.Shared.Popups;
using Content.Shared.Bed.Sleep;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Mindshield.Components;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Stealth;
using Content.Shared.Stealth.Components;
using Content.Shared.Flash;


namespace Content.Server._Sunrise.Antags.Vampires.Systems;

public sealed partial class VampireSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly BlindableSystem _blindable = default!;
    [Dependency] private readonly SharedStealthSystem _stealth = default!;
    private static readonly SoundSpecifier BiteSound = new SoundPathSpecifier("/Audio/Effects/bite.ogg");
    private static readonly SoundSpecifier DevourSound = new SoundPathSpecifier("/Audio/Effects/demon_consume.ogg");
    private readonly Dictionary<EntityUid, List<EntityUid>> _playerShadowSnares = new();
    private void InitializeAbilities()
    {
        SubscribeLocalEvent<VampireComponent, VampireToggleFangsActionEvent>(OnToggleFangs);

        SubscribeLocalEvent<VampireComponent, VampireGlareActionEvent>(OnGlare);

        SubscribeLocalEvent<VampireComponent, VampireSleepActionEvent>(OnSleep);
        SubscribeLocalEvent<VampireComponent, VampireSleepDoAfterEvent>(OnSleepDoAfter);

        SubscribeLocalEvent<VampireComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<VampireComponent, BeforeInteractHandEvent>(OnBeforeInteractHand);
        SubscribeLocalEvent<VampireComponent, VampireDrinkBloodDoAfterEvent>(OnDrinkDoAfter);

        SubscribeLocalEvent<VampireDevourableComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<VampireComponent, VampireDevourDoAfterEvent>(OnDevourDoAfter);

        SubscribeLocalEvent<VampireComponent, VampireRejuvenateIActionEvent>(OnRejuvenateI);
        SubscribeLocalEvent<VampireComponent, VampireRejuvenateIIActionEvent>(OnRejuvenateII);

        SubscribeLocalEvent<VampireComponent, VampireClassSelectActionEvent>(OnClassSelect);

        Subs.BuiEvents<VampireComponent>(VampireClassUiKey.Key, subs =>
        {
            subs.Event<VampireClassChosenBuiMsg>(OnVampireClassChosen);
            subs.Event<VampireClassClosedBuiMsg>(OnVampireClassClosed);
        });

    }

    private void OnUseInHand(Entity<VampireDevourableComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        var user = args.User;
        if (!TryComp<VampireComponent>(user, out var vamp))
            return;

        if (IsMouthBlocked(user))
        {
            _popup.PopupEntity(Loc.GetString("vampire-mouth-covered"), user, user);
            return;
        }

        if (vamp.MaxBloodFullness > 0f && vamp.BloodFullness >= vamp.MaxBloodFullness)
            return;

        if (!Exists(ent.Owner))
            return;

        var doAfterEv = new VampireDevourDoAfterEvent
        {
            BloodFullnessRestore = ent.Comp.BloodFullnessRestore
        };

        var dargs = new DoAfterArgs(EntityManager, user, ent.Comp.DevourDelay, doAfterEv, user, used: ent.Owner)
        {
            NeedHand = true,
            BreakOnHandChange = true,
            BreakOnDropItem = true,
            BreakOnMove = false,
            BreakOnDamage = false,
            AttemptFrequency = AttemptFrequency.StartAndEnd
        };

        if (_doAfter.TryStartDoAfter(dargs))
            args.Handled = true;
    }

    private void OnDevourDoAfter(Entity<VampireComponent> ent, ref VampireDevourDoAfterEvent args)
    {
        if (args.Handled)
            return;

        if (args.Cancelled)
            return;

        if (args.Used is not { } used || !Exists(used))
            return;

        var wasStarving = ent.Comp.BloodFullness <= 0f;
        ent.Comp.BloodFullness = MathF.Min(ent.Comp.MaxBloodFullness, ent.Comp.BloodFullness + args.BloodFullnessRestore);
        var isStarving = ent.Comp.BloodFullness <= 0f;
        if (wasStarving != isStarving)
            _movementSpeed.RefreshMovementSpeedModifiers(ent.Owner);

        Dirty(ent);
        UpdateVampireFedAlert(ent);

        _audio.PlayPvs(DevourSound, ent);
        QueueDel(used);

        args.Handled = true;
    }

    #region Helper Methods

    /// <summary>
    /// Check if tile coordinates are valid and not blocked
    /// </summary>
    internal bool IsValidTile(EntityCoordinates coords, EntityUid? gridUid = null, MapGridComponent? gridComp = null)
    {
        gridUid ??= _transform.GetGrid(coords);
        if (gridUid is null
            || (gridComp is null && !TryComp(gridUid.Value, out gridComp))
            || !_map.TryGetTileRef(gridUid.Value, gridComp, coords, out var tileRef))
            return false;

        return !_turf.IsSpace(tileRef) &&
            !_turf.IsTileBlocked(tileRef, CollisionGroup.Impassable) &&
            !IsTileBlockedByEntities(coords);
    }

    internal bool HasChosenClass(EntityUid uid)
        => TryComp<VampireComponent>(uid, out var vamp) && !string.IsNullOrWhiteSpace(vamp.ChosenClassId);

    internal bool ValidateVampireClass(EntityUid uid, VampireComponent comp, ProtoId<VampireClassPrototype>? requiredClass)
    {
        _ = uid;
        if (requiredClass is null)
            return true;

        return string.Equals(comp.ChosenClassId, requiredClass.Value.Id, StringComparison.Ordinal);
    }

    /// <summary>
    /// Common validation for vampire abilities
    /// component check + class validation + action cost
    /// </summary>
    internal bool ValidateVampireAbility(EntityUid uid, [NotNullWhen(true)] out VampireComponent? comp, ProtoId<VampireClassPrototype>? requiredClass = null, EntityUid? actionEntity = null)
    {
        if (!TryComp(uid, out comp))
            return false;

        if (!ValidateVampireClass(uid, comp, requiredClass))
            return false;

        if (actionEntity.HasValue && !CheckAndConsumeBloodCost((uid, comp), actionEntity.Value))
            return false;

        return true;
    }

    internal bool CanUseVampireAbility(Entity<VampireComponent> ent, EntityUid? actionEntity = null, int bloodCost = 0, bool showPopup = true)
    {
        return TryResolveVampireActionCost(ent, actionEntity, bloodCost, out var resolvedCost, showPopup)
            && CanSpendBlood(ent, resolvedCost, showPopup);
    }

    internal bool CanUseGrantedVampireAction(EntityUid uid, EntityUid? actionEntity = null, int bloodCost = 0, bool showPopup = true)
    {
        if (TryComp<VampireComponent>(uid, out var comp))
        {
            var ent = (uid, comp);
            return CanUseVampireAbility(ent, actionEntity, bloodCost, showPopup);
        }

        return CanUseNonVampireGrantedAction(uid, actionEntity, showPopup);
    }

    internal bool CheckAndConsumeGrantedVampireAction(EntityUid uid, EntityUid? actionEntity = null, int bloodCost = 0)
    {
        if (TryComp<VampireComponent>(uid, out var comp))
        {
            var ent = (uid, comp);
            return CheckAndConsumeBloodCost(ent, actionEntity, bloodCost);
        }

        return CanUseNonVampireGrantedAction(uid, actionEntity);
    }

    internal bool CheckAndConsumeBloodCost(Entity<VampireComponent> ent, EntityUid? actionEntity = null, int bloodCost = 0)
    {
        if (!TryResolveVampireActionCost(ent, actionEntity, bloodCost, out var resolvedCost)
            || !CanSpendBlood(ent, resolvedCost))
        {
            return false;
        }

        return TrySpendBlood(ent, resolvedCost);
    }

    internal bool CheckAndConsumeActionCost(Entity<VampireComponent> ent, EntityUid? actionEntity)
        => CheckAndConsumeBloodCost(ent, actionEntity);

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
        Dirty(ent);
        UpdateVampireAlert(ent.Owner);
        return true;
    }

    internal int AddBlood(
        Entity<VampireComponent> ent,
        float amount,
        EntityUid? target = null,
        bool countTotalBlood = true,
        bool recordTarget = true,
        bool raiseBloodDrankEvent = true)
    {
        if (amount <= 0f)
            return 0;

        var integerAmount = Math.Max(0, (int)amount);
        var wasStarving = ent.Comp.BloodFullness <= 0f;

        if (integerAmount > 0)
        {
            ent.Comp.DrunkBlood += integerAmount;

            if (countTotalBlood)
                ent.Comp.TotalBlood += integerAmount;

            if (recordTarget && target is { } targetUid)
            {
                if (!ent.Comp.BloodDrunkFromTargets.ContainsKey(targetUid))
                    ent.Comp.BloodDrunkFromTargets[targetUid] = 0;

                ent.Comp.BloodDrunkFromTargets[targetUid] += integerAmount;
            }
        }

        ent.Comp.BloodFullness = MathF.Min(ent.Comp.MaxBloodFullness, ent.Comp.BloodFullness + amount);

        var isStarving = ent.Comp.BloodFullness <= 0f;
        if (wasStarving != isStarving)
            _movementSpeed.RefreshMovementSpeedModifiers(ent.Owner);

        Dirty(ent);
        UpdateVampireAlert(ent.Owner);
        UpdateVampireFedAlert(ent);

        if (integerAmount > 0)
        {
            UpdateFullPower(ent);
            RaiseLocalEvent(ent.Owner, new VampireProgressionChangedEvent());
        }

        if (raiseBloodDrankEvent && target is { } drankTarget)
            RaiseLocalEvent(ent.Owner, new VampireBloodDrankEvent(drankTarget, amount));

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

        if (ent.Comp.TotalBlood < vac.BloodToUnlock)
            return false;

        if (!ValidateVampireClass(ent.Owner, ent.Comp, vac.RequiredClass))
            return false;

        if (vac.RequiresFullPower && !ent.Comp.FullPower)
        {
            if (showPopup)
                _popup.PopupEntity(Loc.GetString("action-vampire-not-enough-power"), ent.Owner, ent.Owner);

            return false;
        }

        if (resolvedCost <= 0 && vac.BloodCost > 0)
            resolvedCost = (int)vac.BloodCost;

        return true;
    }

    private bool CanUseNonVampireGrantedAction(EntityUid uid, EntityUid? actionEntity, bool showPopup = true)
    {
        if (actionEntity is not { } action)
            return true;

        if (!Exists(action))
            return false;

        if (!TryComp<VampireActionComponent>(action, out var vac))
            return true;

        if (vac.AllowNonVampireUsers)
            return true;

        return false;
    }

    internal bool IsProtectedByFaith(EntityUid target)
        => HasComp<BibleUserComponent>(target);

    private bool IsInvalidDrinkTarget(EntityUid user, EntityUid target, bool showPopup = true)
    {
        if (!HasComp<VampireComponent>(target) && !HasComp<VampireThrallComponent>(target))
            return false;

        if (showPopup)
            _popup.PopupEntity(Loc.GetString("vampire-drink-invalid-target"), user, user, PopupType.MediumCaution);

        return true;
    }

    /// <summary>
    /// Checks if a tile position is blocked by solid entities(walls etc.)
    /// </summary>
    internal bool IsTileBlockedByEntities(EntityCoordinates coords)
    {
        // Check for anchored entities in this position that block movement
        foreach (var ent in _lookup.GetEntitiesIntersecting(_transform.ToMapCoordinates(coords), LookupFlags.Static))
        {
            // Skip non anchored entities
            if (!Transform(ent).Anchored)
                continue;

            // Check if entity has a physics component with impassable collision
            if (TryComp<PhysicsComponent>(ent, out var physics) &&
                physics.CanCollide &&
                ((physics.CollisionLayer & (int)CollisionGroup.Impassable) != 0 ||
                 (physics.CollisionMask & (int)CollisionGroup.Impassable) != 0))
                return true;

            // Check for door components that typically block movement
            if (HasComp<Shared.Doors.Components.DoorComponent>(ent))
                return true;
        }
        return false;
    }

    #endregion

    #region Base Abilities
    private void OnToggleFangs(Entity<VampireComponent> ent, ref VampireToggleFangsActionEvent args)
    {
        if (args.Handled)
            return;

        ent.Comp.FangsExtended = !ent.Comp.FangsExtended;
        if (!ent.Comp.FangsExtended)
            ent.Comp.IsDrinking = false;

        if (ent.Comp.ActionEntities.TryGetValue("ActionVampireToggleFangs", out var actionEntity) && _actions.GetAction(actionEntity) is { } action)
            _actions.SetToggled(action.AsNullable(), ent.Comp.FangsExtended);

        Dirty(ent);
        args.Handled = true;
    }

    private void OnAfterInteract(Entity<VampireComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || !ent.Comp.FangsExtended || !Exists(args.Target))
            return;

        var target = args.Target.Value;

        if (target == ent.Owner
            || !HasComp<BloodstreamComponent>(target)
            )
            return;

        if (IsInvalidDrinkTarget(ent.Owner, target))
            return;

        if (IsProtectedByFaith(target) && ent.Comp.FullPower != true)
        {
            _popup.PopupEntity(Loc.GetString("vampire-target-protected-by-faith"), ent.Owner, ent.Owner, Shared.Popups.PopupType.MediumCaution);
            return;
        }

        if (IsMouthBlocked(ent.Owner))
        {
            _popup.PopupEntity(Loc.GetString("vampire-mouth-covered"), ent.Owner, ent.Owner);
            return;
        }

        StartDrinkDoAfter(ent, target, showPopup: true);
        args.Handled = true;
    }

    private void OnBeforeInteractHand(Entity<VampireComponent> ent, ref BeforeInteractHandEvent args)
    {
        if (args.Handled || !ent.Comp.FangsExtended)
            return;

        var target = args.Target;
        if (!Exists(target)
            || target == ent.Owner
            || !HasComp<BloodstreamComponent>(target)
            )
            return;

        if (IsInvalidDrinkTarget(ent.Owner, target))
            return;

        if (IsProtectedByFaith(target) && ent.Comp.FullPower != true)
        {
            _popup.PopupEntity(Loc.GetString("vampire-target-protected-by-faith"), ent.Owner, ent.Owner, Shared.Popups.PopupType.MediumCaution);
            return;
        }

        if (IsMouthBlocked(ent.Owner))
        {
            _popup.PopupEntity(Loc.GetString("vampire-mouth-covered"), ent.Owner, ent.Owner);
            return;
        }

        StartDrinkDoAfter(ent, target, showPopup: true);
        args.Handled = true;
    }

    /// <summary>
    /// System for checking if a target can be drank from and handling the drinking
    /// </summary>
    private void OnDrinkDoAfter(Entity<VampireComponent> ent, ref VampireDrinkBloodDoAfterEvent args)
    {
        if (args.Handled)
            return;

        if (args.Cancelled)
        {
            ent.Comp.IsDrinking = false;
            return;
        }

        if (!ent.Comp.FangsExtended
            || args.Args.Target is null
            || !HasComp<BloodstreamComponent>(args.Args.Target.Value)
            )
        {
            ent.Comp.IsDrinking = false;
            return;
        }

        var target = args.Args.Target.Value;

        if (IsInvalidDrinkTarget(ent.Owner, target, showPopup: false))
        {
            ent.Comp.IsDrinking = false;
            return;
        }

        if (!ent.Comp.BloodDrunkFromTargets.TryGetValue(target, out var drunkFromTarget))
            drunkFromTarget = 0;

        if (drunkFromTarget >= ent.Comp.MaxBloodPerTarget)
        {
            _popup.PopupEntity(Loc.GetString("vampire-drink-target-maxed", ("amount", ent.Comp.MaxBloodPerTarget)), ent.Owner, ent.Owner, Shared.Popups.PopupType.MediumCaution);
            ent.Comp.IsDrinking = false;
            return;
        }

        // Нужно будет добавить когда будет механическая раса
        // if (HasComp<IPCBatteryComponent>(target) //IPCs don't have blood
        //     || (!TryComp<MobStateComponent>(target, out var mobState) //Is the entity a mob at all?
        //     || (mobState.CurrentState == Shared.Mobs.MobState.Dead && ent.Comp.DeadEfficiency == 0f)  //Dead things aren't a good source of blood if configured to not allow drinking from the dead at all
        //     ))
        // {
        //     _popup.PopupEntity(Loc.GetString("vampire-drink-target-not-viable"), ent.Owner, ent.Owner, Shared.Popups.PopupType.MediumCaution);
        //     ent.Comp.IsDrinking = false;
        //     return;
        // }

        var sipInefficiency = 0f;
        var sipAmount = ent.Comp.SipAmount;

        if (HasComp<HumanoidAppearanceComponent>(args.Args.Target.Value))
            sipInefficiency = ent.Comp.HumanoidEfficiency;
        else
            sipInefficiency = ent.Comp.NonHumanoidEfficiency;

        if (TryComp<MobStateComponent>(target, out var mobState) && mobState.CurrentState == Shared.Mobs.MobState.Dead)
            sipInefficiency *= ent.Comp.DeadEfficiency; // Dead things aren't as good source of blood
        if (TryComp<PerishableComponent>(target, out var rot)) //Is the target rotting?
        {
            switch (rot.Stage)
            {
                case 0: //fresh or not rotted at all
                    sipInefficiency *= ent.Comp.Rot0Efficiency;
                    break;
                case 1: //initial stages
                    sipInefficiency *= ent.Comp.Rot1Efficiency;
                    break;
                case 2: //mid rot
                    sipInefficiency *= ent.Comp.Rot2Efficiency;
                    break;
                case 3: //late rot
                    sipInefficiency *= ent.Comp.Rot3Efficiency;
                    break;
                case 4: //full rot
                    sipInefficiency *= ent.Comp.Rot4Efficiency;
                    break;
                default: //if we push past 4 for some reason, just assume same level as 4
                    sipInefficiency *= ent.Comp.Rot4Efficiency;
                    break;
            }
        }

        if (sipInefficiency <= 0f) //If we have set the efficiency to 0, then no point continuing
        {
            _popup.PopupEntity(Loc.GetString("vampire-drink-target-rot"), ent.Owner, ent, PopupType.MediumCaution);
            ent.Comp.IsDrinking = false;
            return;
        }

        sipInefficiency = 1f / sipInefficiency;

        var maxCanDrink = ent.Comp.MaxBloodPerTarget - drunkFromTarget;
        var actualSipAmount = MathF.Min(sipAmount, maxCanDrink);
        if (!TryComp<BloodstreamComponent>(target, out var blood)) //Does the target have a blood stream?
        {
            ent.Comp.IsDrinking = false; //Blood level reduction failed
            _popup.PopupEntity(Loc.GetString("vampire-drink-target-empty"), ent.Owner, ent, PopupType.MediumCaution);
            return;
        }

        //attempt to drain the target's blood level
        var targetBloodLevel = _blood.GetBloodLevel(target) * blood.BloodReferenceSolution.MaxVolume.Value / 100; //get target's current blood volume in u
        if (targetBloodLevel <= 0.0f) //Check the target has blood to drink at all
        {
            ent.Comp.IsDrinking = false; //Blood level reduction failed
            _popup.PopupEntity(Loc.GetString("vampire-drink-target-empty"), ent.Owner, ent, PopupType.MediumCaution);
            return;
        }
        else if (targetBloodLevel <= actualSipAmount * sipInefficiency) //Check if we are attempting to drain too much blood and reduce the amount drank if so
            actualSipAmount = targetBloodLevel / sipInefficiency;

        // Drain extra blood from the target to account for sipInefficiency. This logic is a bit backwards in that it would make more sense for the sip amount from target to remain constant and the blood gained to vary, but for gameplay this works better for vampires
        if (_blood.TryModifyBloodLevel(target, -actualSipAmount * sipInefficiency)) //Blood lost to Inefficiency is just deleted, overly complex to add system to dump it on the ground, though that would be a nice thing to add in the future maybe?
        {
            var targetIsHumanoid = HasComp<HumanoidAppearanceComponent>(target);
            AddBlood(ent, actualSipAmount, target, countTotalBlood: targetIsHumanoid);

            //Biting Damage
            //A little bit of additional damage to disincentivize blood donations
            var biteDamage = new DamageSpecifier();
            biteDamage += new DamageSpecifier(_proto.Index<DamageTypePrototype>(PierceTypeId), ent.Comp.SipPierceDamage * actualSipAmount); //5 pierce per 10u
            _damageableSystem.TryChangeDamage(target, biteDamage, ignoreResistances: true);
            _blood.TryModifyBleedAmount(target, 1);


            //Add in blindness instead of cancer
            if (TryComp<BlindableComponent>(target, out var blindable) && 2 <= ent.Comp.BlindInc)
            {
                _blindable.AdjustEyeDamage((target, blindable), 1);
                ent.Comp.BlindInc = 0;
            }
            else if (ent.Comp.BlindInc < 2)
                ent.Comp.BlindInc += 1;

            // Base healing
            var baseHealSpec = new DamageSpecifier();
            baseHealSpec += new DamageSpecifier(_proto.Index<DamageGroupPrototype>(BruteGroupId), -ent.Comp.VampHealBrute);
            baseHealSpec += new DamageSpecifier(_proto.Index<DamageGroupPrototype>(BurnGroupId), -ent.Comp.VampHealBurn);
            baseHealSpec += new DamageSpecifier(_proto.Index<DamageTypePrototype>(PoisonTypeId), -ent.Comp.VampHealPois);
            baseHealSpec += new DamageSpecifier(_proto.Index<DamageTypePrototype>(OxyLossTypeId), -ent.Comp.VampHealAsphyxiation);
            _damageableSystem.TryChangeDamage(ent.Owner, baseHealSpec, true);

            _audio.PlayPvs(BiteSound, target, AudioParams.Default.WithVolume(-7f));
            var targetCoords = Transform(target).Coordinates;
            Spawn("WeaponArcBite", targetCoords);

            var currentDrunkFromTarget = ent.Comp.BloodDrunkFromTargets.GetValueOrDefault(target, 0);
            if (ent.Comp.FangsExtended && currentDrunkFromTarget < ent.Comp.MaxBloodPerTarget)
            {
                ent.Comp.IsDrinking = false;
                StartDrinkDoAfter(ent, target, showPopup: false);
            }
            else
            {
                ent.Comp.IsDrinking = false;
                if (currentDrunkFromTarget >= ent.Comp.MaxBloodPerTarget)
                    _popup.PopupEntity(Loc.GetString("vampire-drink-target-hard-max", ("amount", ent.Comp.MaxBloodPerTarget)), ent.Owner, ent, PopupType.MediumCaution);
            }
        }
        else
        {
            ent.Comp.IsDrinking = false; //Blood level reduction failed
            _popup.PopupEntity(Loc.GetString("vampire-drink-target-empty"), ent.Owner, ent, PopupType.MediumCaution);
            return;
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

    private void StartDrinkDoAfter(Entity<VampireComponent> ent, EntityUid target, bool showPopup)
    {
        if (ent.Comp.IsDrinking)
            return;

        if (IsMouthBlocked(ent.Owner))
        {
            if (showPopup)
                _popup.PopupEntity(Loc.GetString("vampire-mouth-covered"), ent.Owner, ent.Owner);
            return;
        }

        var dargs = new DoAfterArgs(EntityManager, ent.Owner, TimeSpan.FromSeconds(1.25), new VampireDrinkBloodDoAfterEvent(), ent.Owner, target)
        {
            DistanceThreshold = ent.Comp.BiteDistanceThreshold,
            BreakOnDamage = true,
            BreakOnHandChange = true,
            BreakOnMove = true,
            BreakOnWeightlessMove = true,
            BlockDuplicate = true,
            CancelDuplicate = true,
            AttemptFrequency = AttemptFrequency.StartAndEnd
        };

        if (_doAfter.TryStartDoAfter(dargs))
        {
            ent.Comp.IsDrinking = true;
            if (showPopup)
                _popup.PopupEntity(Loc.GetString("vampire-drink-start", ("target", Identity.Entity(target, EntityManager))), ent.Owner, ent.Owner);
        }
    }

    /// <summary>
	///     On use of action to attempt to sleep a single target; check if target can be slept, if vamp has enough blood, and trigger a doafter
	/// </summary>
    private void OnSleep(Entity<VampireComponent> ent, ref VampireSleepActionEvent args)
    {
        if (args.Handled || !Exists(args.Target))
            return;

        var actionEntity = args.Action.Owner;

        if (!TryGetActionBloodCost(actionEntity, out var bloodCost))
            return;

        var target = args.Target;

        if (target == ent.Owner)
            return;

        if (IsProtectedByFaith(target) && ent.Comp.FullPower != true)
        {
            _popup.PopupEntity(Loc.GetString("vampire-target-protected-by-faith"), ent.Owner, ent.Owner, Shared.Popups.PopupType.MediumCaution);
            return;
        }

        if (HasFlashProtection(target))
        {
            _popup.PopupEntity(Loc.GetString("vampire-sleep-protected"), ent.Owner, ent.Owner, PopupType.MediumCaution);
            return;
        }

        if (!CanSpendBlood(ent, bloodCost))
            return;

        var doAfter = new DoAfterArgs(EntityManager, ent.Owner, args.ChannelTime, new VampireSleepDoAfterEvent { BloodCost = bloodCost }, ent.Owner, target)
        {
            DistanceThreshold = args.SleepDistanceThreshold,
            BreakOnDamage = true,
            BreakOnMove = true,
            BreakOnWeightlessMove = true,
            MovementThreshold = args.SleepMovementThreshold,
            RequireCanInteract = true,
            BlockDuplicate = true,
            CancelDuplicate = true
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
            return;

        args.Handled = true;
    }

    private bool TryGetActionBloodCost(EntityUid actionEntity, out int bloodCost)
    {
        bloodCost = 0;

        if (!Exists(actionEntity) || !TryComp<VampireActionComponent>(actionEntity, out var actionComp))
            return false;

        bloodCost = (int)Math.Max(actionComp.BloodCost, 0);
        return true;
    }

    private bool HasFlashProtection(EntityUid target)
    {
        var attempt = new FlashAttemptEvent(target, null, null);
        RaiseLocalEvent(target, ref attempt, true);
        return attempt.Cancelled;
    }

    /// <summary>
	///     Triggered once sleep do after is completed, check one more time to see if the target has somehow gained immunity during the do after and if not consume the blood cost and apply the sleep.
	/// </summary>
    private void OnSleepDoAfter(Entity<VampireComponent> ent, ref VampireSleepDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target is null)
            return;

        var target = args.Target.Value;

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

        if (!CheckAndConsumeBloodCost(ent, null, args.BloodCost))
            return;

        //Put the target to sleep
        _statusEffects.TryAddStatusEffectDuration(target, SleepingSystem.StatusEffectForcedSleeping, args.Duration);
        args.Handled = true;
    }

    /// <summary>
    /// Action that stuns nearby mobs for a short duration
    /// </summary>
    private void OnGlare(Entity<VampireComponent> ent, ref VampireGlareActionEvent args)
    {
        //If vampire cannot see, they cannot glare
        if (TryComp<BlindableComponent>(ent, out var blindable) && blindable.IsBlind)
            return;

        if (args.Handled
            || !ent.Comp.ActionEntities.TryGetValue("ActionVampireGlare", out var actionEntity)
            || !CheckAndConsumeBloodCost(ent, actionEntity))
            return;

        // Find targets within 1 tile around the vampire
        var targets = _lookup.GetEntitiesInRange(ent.Owner, args.Range, LookupFlags.Dynamic | LookupFlags.Sundries);

        var ourXform = Transform(ent);
        var ourDirection = ourXform.LocalRotation.ToWorldVec();
        var ourPosition = ourXform.LocalPosition;
        float effectScale = 1.0f;

        foreach (var target in targets)
        {
            if (target == ent.Owner)
                continue;

            //reset effectScale for next possible target
            effectScale = 1.0f;

            if (HasFlashProtection(target))
            {
                if (ent.Comp.TotalBlood < ent.Comp.MidPowerThreshold)
                    effectScale = args.FlashImmunityEffectScaleWeak; //below mid
                else if (ent.Comp.TotalBlood < ent.Comp.HighPowerThreshold)
                    effectScale = args.FlashImmunityEffectScaleMid; //mid - high
                else if (ent.Comp.TotalBlood < ent.Comp.FullPowerThreshold)
                    effectScale = args.FlashImmunityEffectScaleStrong; //high - full
            }

            if (ent.Comp.FullPower) //If vamp is at full power, effect gets scaled up a bit regardless of flash protection
                effectScale = args.GlareEffectScaleFull;

            if (effectScale <= 0) //If the effect is nullified, no point doing anything more.
                continue;

            var targetPosition = Transform(target).LocalPosition;
            var vectorToTarget = Vector2.Normalize(targetPosition - ourPosition);

            var dot = Vector2.Dot(ourDirection, vectorToTarget);

            if (!TryComp<StaminaComponent>(target, out var stam))
                continue;

            var knockedDown = HasComp<KnockedDownComponent>(target);

            // If target in front
            if (dot > args.DotForwardLimit && !knockedDown)
            {
                _stun.TryAddParalyzeDuration(target, args.FrontParalyzeDuration * effectScale);

                _stamina.TakeStaminaDamage(target, args.FrontStaminaDamage * effectScale, stam, source: ent.Owner);

                // Mute target
                TryInjectReagents(target, args.Reagents, effectScale);

                StartGlareDotEffect(target, ent.Owner, args.DotStaminaDamage * effectScale, args.DotTickCount, args.DotTickInterval);
            }
            // If target behind
            else if (dot < args.DotBackwardLimit && !knockedDown)
                _stamina.TakeStaminaDamage(target, args.BehindStaminaDamage * effectScale, stam, source: ent.Owner);
            // else target is to the side
            else
            {
                _stun.TryAddParalyzeDuration(target, args.SideParalyzeDuration * effectScale);

                _stamina.TakeStaminaDamage(target, args.SideStaminaDamage * effectScale, stam, source: ent.Owner);
            }
        }

        args.Handled = true;
    }

    /// <summary>
    /// Try to inject whatever chem is specified
    /// </summary>
    private bool TryInjectReagents(EntityUid target, Dictionary<string, FixedPoint2> reagents, float effectScale)
    {
        var solution = new Solution();
        foreach (var reagent in reagents)
            solution.AddReagent(reagent.Key, reagent.Value * effectScale);
        if (!_solution.TryGetInjectableSolution(target, out var targetSolution, out var _))
            return false;

        if (!_solution.TryAddSolution(targetSolution.Value, solution))
            return false;

        return true;
    }

    private void StartGlareDotEffect(EntityUid target, EntityUid source, float damage, int tickCount, TimeSpan tickInterval)
    {
        if (tickCount <= 0 || !Exists(target) || !Exists(source))
            return;

        var active = EnsureComp<ActiveVampireGlareDotComponent>(target);
        active.Source = source;
        active.StaminaDamage = damage;
        active.TicksRemaining = tickCount;
        active.TickInterval = tickInterval;
        active.NextTick = _timing.CurTime;
    }

    private void OnRejuvenateI(Entity<VampireComponent> ent, ref VampireRejuvenateIActionEvent args)
    {
        if (args.Handled
            || !ent.Comp.ActionEntities.TryGetValue("ActionVampireRejuvenateI", out var actionEntity)
            || !CheckAndConsumeBloodCost(ent, actionEntity))
            return;

        ResetRejuvenateEffects(ent.Owner, args.ResetStamina, args.RemoveStuns);

        args.Handled = true;
    }

    private void OnRejuvenateII(Entity<VampireComponent> ent, ref VampireRejuvenateIIActionEvent args)
    {
        if (args.Handled
            || !ent.Comp.ActionEntities.TryGetValue("ActionVampireRejuvenateII", out var actionEntity)
            || !CheckAndConsumeBloodCost(ent, actionEntity))
            return;

        ResetRejuvenateEffects(ent.Owner, args.ResetStamina, args.RemoveStuns);
        PurgeRejuvenateReagents(ent.Owner, args);
        StartRejuvenateHealing(ent.Owner, args);

        args.Handled = true;
    }

    private void ResetRejuvenateEffects(EntityUid uid, bool resetStamina, bool removeStuns)
    {
        if (resetStamina && TryComp<StaminaComponent>(uid, out var stamina))
        {
            stamina.StaminaDamage = 0f;
            stamina.Critical = false;
            stamina.AfterCritical = false;
            RemComp<ActiveStaminaComponent>(uid);
            _statusEffects.TryRemoveStatusEffect(uid, SharedStaminaSystem.StaminaLow);

            Dirty(uid, stamina);
        }

        if (!removeStuns)
            return;

        _statusEffects.TryRemoveStatusEffect(uid, SharedStunSystem.StunId);
        _stun.TryUnstun(uid);
        RemComp<KnockedDownComponent>(uid);
    }

    private void PurgeRejuvenateReagents(EntityUid uid, VampireRejuvenateIIActionEvent args)
    {
        if (args.ReagentPurgeAmount <= FixedPoint2.Zero
            || !TryComp<BloodstreamComponent>(uid, out var blood)
            || !_solution.ResolveSolution(uid, blood.BloodSolutionName, ref blood.BloodSolution, out var bloodstreamSolution))
        {
            return;
        }

        var solEnt = blood.BloodSolution.Value;
        var toRemove = FixedPoint2.Zero;

        foreach (var quant in bloodstreamSolution.Contents.ToArray())
        {
            if (toRemove >= args.ReagentPurgeAmount)
                break;

            if (!_proto.TryIndex<ReagentPrototype>(quant.Reagent.Prototype, out var proto)
                || proto.Metabolisms is null
                || !proto.Metabolisms.Keys.Any(k => args.PurgedMetabolismGroups.Contains(k.Id)))
                continue;

            var remaining = args.ReagentPurgeAmount - toRemove;
            var removeAmt = FixedPoint2.Min(quant.Quantity, remaining);

            _solution.RemoveReagent(solEnt, quant.Reagent, removeAmt);
            toRemove += removeAmt;
        }
    }

    private void StartRejuvenateHealing(EntityUid uid, VampireRejuvenateIIActionEvent args)
    {
        if (args.HealTicks <= 0)
            return;

        var active = EnsureComp<ActiveVampireRejuvenateComponent>(uid);
        active.TicksRemaining = args.HealTicks;
        active.TickInterval = args.HealTickInterval;
        active.NextTick = _timing.CurTime;
        active.HealGroups = new Dictionary<string, FixedPoint2>(args.HealGroups);
        active.HealTypes = new Dictionary<string, FixedPoint2>(args.HealTypes);
    }

    private void ProcessActiveVampireEffects(TimeSpan now)
    {
        var rejuvenateQuery = EntityQueryEnumerator<ActiveVampireRejuvenateComponent>();
        while (rejuvenateQuery.MoveNext(out var uid, out var rejuvenate))
        {
            if (now < rejuvenate.NextTick)
                continue;

            ApplyConfiguredHeal(uid, rejuvenate.HealGroups, rejuvenate.HealTypes);
            rejuvenate.TicksRemaining--;

            if (rejuvenate.TicksRemaining <= 0)
            {
                RemComp<ActiveVampireRejuvenateComponent>(uid);
                continue;
            }

            rejuvenate.NextTick = now + rejuvenate.TickInterval;
        }

        var glareQuery = EntityQueryEnumerator<ActiveVampireGlareDotComponent>();
        while (glareQuery.MoveNext(out var uid, out var glare))
        {
            if (now < glare.NextTick)
                continue;

            if (!Exists(glare.Source))
            {
                RemComp<ActiveVampireGlareDotComponent>(uid);
                continue;
            }

            if (TryComp<StaminaComponent>(uid, out var stam) && !stam.Critical)
                _stamina.TakeStaminaDamage(uid, glare.StaminaDamage, stam, source: glare.Source);

            glare.TicksRemaining--;
            if (glare.TicksRemaining <= 0)
            {
                RemComp<ActiveVampireGlareDotComponent>(uid);
                continue;
            }

            glare.NextTick = now + glare.TickInterval;
        }

        var pacifyQuery = EntityQueryEnumerator<ActiveVampirePacifyComponent>();
        while (pacifyQuery.MoveNext(out var uid, out var pacify))
        {
            if (now < pacify.EndTime)
                continue;

            RemComp<ActiveVampirePacifyComponent>(uid);
            RemComp<PacifiedComponent>(uid);
        }

        var invisibleQuery = EntityQueryEnumerator<ActiveVampireInvisibilityComponent>();
        while (invisibleQuery.MoveNext(out var uid, out var invis))
        {
            if (now < invis.EndTime)
                continue;

            RemComp<ActiveVampireInvisibilityComponent>(uid);
            RestoreVampireInvisibilityStealth(uid, invis);
        }
    }

    private void RestoreVampireInvisibilityStealth(EntityUid uid, ActiveVampireInvisibilityComponent invis)
    {
        if (!TryComp<StealthComponent>(uid, out var stealth))
            return;

        if (!invis.HadStealthComponent)
        {
            RemComp<StealthComponent>(uid);
            return;
        }

        _stealth.SetEnabled(uid, invis.PreviousStealthEnabled, stealth);
        _stealth.SetVisibility(uid, invis.PreviousStealthVisibility, stealth);
    }

    private void ApplyConfiguredHeal(
        EntityUid uid,
        IReadOnlyDictionary<string, FixedPoint2> healGroups,
        IReadOnlyDictionary<string, FixedPoint2> healTypes)
    {
        var healSpec = new DamageSpecifier();

        foreach (var (groupId, amount) in healGroups)
        {
            if (amount <= FixedPoint2.Zero || !_proto.TryIndex<DamageGroupPrototype>(groupId, out var group))
                continue;

            healSpec += new DamageSpecifier(group, -amount);
        }

        foreach (var (typeId, amount) in healTypes)
        {
            if (amount <= FixedPoint2.Zero || !_proto.TryIndex<DamageTypePrototype>(typeId, out var type))
                continue;

            healSpec += new DamageSpecifier(type, -amount);
        }

        if (healSpec.Empty)
            return;

        _damageableSystem.TryChangeDamage(uid, healSpec, true);
    }

    private void OnClassSelect(Entity<VampireComponent> ent, ref VampireClassSelectActionEvent args)
    {
        if (args.Handled)
            return;

        if (HasChosenClass(ent.Owner))
        {
            args.Handled = true;
            return;
        }

        OpenClassUi(ent.Owner, ent.Comp);
        args.Handled = true;
        Dirty(ent);
    }

    #endregion

    #region Full Power, Passives
    /// <summary>
    /// Vampire full power level check
    /// </summary>
    private void UpdateFullPower(Entity<VampireComponent> ent)
    {
        var uniqueHumanoids = 0;
        foreach (var target in ent.Comp.BloodDrunkFromTargets.Keys)
        {
            if (Exists(target) && HasComp<HumanoidAppearanceComponent>(target))
                uniqueHumanoids++;
        }

        ent.Comp.UniqueHumanoidVictims = uniqueHumanoids;
        var prev = ent.Comp.FullPower;
        ent.Comp.FullPower = ent.Comp.TotalBlood >= ent.Comp.FullPowerThreshold && uniqueHumanoids >= ent.Comp.FullPowerUniqueHumanoids;
        if (!prev && ent.Comp.FullPower)
        {
            _popup.PopupEntity(Loc.GetString("vampire-full-power-achieved"), ent.Owner, ent.Owner);
            RaiseLocalEvent(ent.Owner, new VampireFullPowerAchievedEvent());
        }

        Dirty(ent);
    }

    private bool IsMouthBlocked(EntityUid uid)
    {
        if (!HasComp<InventoryComponent>(uid))
            return false;

        var slots = new[] { "mask", "head" };
        foreach (var slot in slots)
            if (_inventory.TryGetSlotEntity(uid, slot, out var ent) &&
                TryComp<IngestionBlockerComponent>(ent.Value, out var blocker) &&
                blocker.Enabled)

                return true;

        return false;
    }

    #endregion
}

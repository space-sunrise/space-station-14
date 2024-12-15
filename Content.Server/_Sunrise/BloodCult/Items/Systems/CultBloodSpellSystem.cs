using System.Linq;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Hands.Systems;
using Content.Server.Popups;
using Content.Shared._Sunrise.BloodCult.Components;
using Content.Shared._Sunrise.BloodCult.Items;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;
using Content.Shared.Hands;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Timing;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.BloodCult.Items.Systems;

public sealed class CultBloodSpellSystem: EntitySystem
{
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly HandsSystem _handsSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultBloodSpellComponent, GotUnequippedHandEvent>(OnGotUnequippedHand);
        SubscribeLocalEvent<CultBloodSpellComponent, AfterInteractEvent>(OnInteractEvent);
        SubscribeLocalEvent<CultBloodSpellComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<CultBloodSpellComponent, CultBloodSpellCreateOrbBuiMessage>(OnRequestCreateOrb);
        SubscribeLocalEvent<CultBloodSpellComponent, CountSelectorMessage>(OnCreateOrb);
        SubscribeLocalEvent<CultBloodSpellComponent, CultBloodSpellCreateBloodSpearBuiMessage>(OnRequestCreateBloodSpear);
        SubscribeLocalEvent<CultBloodSpellComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(EntityUid uid, CultBloodSpellComponent component, ExaminedEvent args)
    {
        if (!TryComp<BloodCultistComponent>(args.Examiner, out var cultistComponent))
            return;

        args.PushMarkup($"[bold][color=white]Доступно {cultistComponent.BloodCharges} зарядов[/color][bold]");
    }


    private void OnCreateOrb(EntityUid uid, CultBloodSpellComponent component, CountSelectorMessage args)
    {
        if (!TryComp<BloodCultistComponent>(args.Actor, out var comp))
            return;

        var count = Math.Min(component.BloodOrbMinCost, args.Count);

        if (comp.BloodCharges < count)
            return;

        var orb = Spawn(component.BlodOrbSpawnId, _transformSystem.GetMapCoordinates(uid));
        var bloodOrb = EnsureComp<CultBloodOrbComponent>(orb);
        bloodOrb.BloodCharges = args.Count;
    }

    private void OnRequestCreateOrb(EntityUid uid, CultBloodSpellComponent component, CultBloodSpellCreateOrbBuiMessage args)
    {
        if (!TryComp<BloodCultistComponent>(args.Actor, out var comp))
            return;

        if (!TryComp<ActorComponent>(args.Actor, out var actorComponent))
            return;

        _ui.OpenUi(uid, CountSelectorUIKey.Key, args.Actor);
    }

    private void OnRequestCreateBloodSpear(EntityUid uid, CultBloodSpellComponent component, CultBloodSpellCreateBloodSpearBuiMessage args)
    {
        if (!TryComp<BloodCultistComponent>(args.Actor, out var comp))
            return;

        if (!TryComp<ActorComponent>(args.Actor, out var actorComponent))
            return;

        if (comp.BloodCharges < component.BloodSpearCost)
            return;

        var bloodSpear = Spawn(component.BloodSpearSpawnId, _transformSystem.GetMapCoordinates(uid));
        var bloodSpearOwner = EnsureComp<BloodSpearOwnerComponent>(args.Actor);
        bloodSpearOwner.Spear = bloodSpear;
        var bloodSpearComp = EnsureComp<CultBloodSpearComponent>(bloodSpear);
        bloodSpearComp.SpearOwner = args.Actor;
        _handsSystem.TryDrop(args.Actor, uid, checkActionBlocker: false);
        _handsSystem.TryPickup(args.Actor, bloodSpear, checkActionBlocker: false);
        comp.BloodCharges -= component.BloodSpearCost;
    }

    private void OnUseInHand(EntityUid uid, CultBloodSpellComponent component, UseInHandEvent args)
    {
        if (!TryComp<BloodCultistComponent>(args.User, out _) || !TryComp<ActorComponent>(args.User, out var actor))
            return;

        _ui.OpenUi(uid, CultBloodSpellUiKey.Key, actor.PlayerSession);
    }

    private void OnInteractEvent(EntityUid uid, CultBloodSpellComponent component, AfterInteractEvent args)
    {
        if (!args.CanReach || args.Handled)
            return;

        if (!TryComp(uid, out UseDelayComponent? useDelay) || _useDelay.IsDelayed((uid, useDelay)))
            return;

        if (!TryComp<BloodCultistComponent>(args.User, out var cultistComponent))
            return;

        if (TryComp<CultBloodOrbComponent>(args.Target, out var bloodOrbComponent))
        {
            cultistComponent.BloodCharges += bloodOrbComponent.BloodCharges;
            QueueDel(args.Target);
            _popupSystem.PopupEntity($"Собрано {bloodOrbComponent.BloodCharges} зарядов",
                args.User, args.User, PopupType.Large);
            _audioSystem.PlayPvs(component.BloodAbsorbSound, args.User, component.BloodAbsorbSound.Params);
            args.Handled = true;
            return;
        }

        if (HasComp<BloodCultistComponent>(args.Target) || HasComp<ConstructComponent>(args.Target))
        {
            args.Handled = HealCultist(args.Target.Value, args.User, cultistComponent, component);
            return;
        }

        if (TryComp<BloodstreamComponent>(args.Target, out var bloodstreamComponent))
        {
            if (_solutionSystem.ResolveSolution(args.Target.Value, bloodstreamComponent.BloodSolutionName,
                    ref bloodstreamComponent.BloodSolution, out var bloodSolution))
            {
                if (bloodSolution.Volume > 250)
                {
                    var blood = bloodSolution.SplitSolutionWithOnly(
                        100, "Blood");
                    cultistComponent.BloodCharges += blood.Volume / 2;
                    _popupSystem.PopupEntity($"Собрано {blood.Volume / 2} зарядов",
                        args.User, args.User, PopupType.Large);
                    _audioSystem.PlayPvs(component.BloodAbsorbSound, args.User, component.BloodAbsorbSound.Params);
                    blood.RemoveAllSolution();
                }
            }
            args.Handled = true;
            return;
        }

        var getCharges = AbsorbBloodPools(args.User, args.ClickLocation, component);
        cultistComponent.BloodCharges += getCharges;
    }

    private FixedPoint2 AbsorbBloodPools(EntityUid user, EntityCoordinates coordinates, CultBloodSpellComponent bloodSpell)
    {
        var puddles = new ValueList<(EntityUid Entity, string Solution)>();
        puddles.Clear();
        foreach (var entity in _lookup.GetEntitiesInRange(coordinates, bloodSpell.RadiusAbsorbBloodPools))
        {
            if (TryComp<PuddleComponent>(entity, out var puddle))
            {
                puddles.Add((entity, puddle.SolutionName));
            }
        }

        if (puddles.Count == 0)
        {
            return 0;
        }

        var absorbBlood = new Solution();
        foreach (var (puddle, solution) in puddles)
        {
            if (!_solutionSystem.TryGetSolution(puddle, solution, out var puddleSolution))
            {
                continue;
            }
            foreach (var puddleSolutionContent in puddleSolution.Value.Comp.Solution.ToList())
            {
                if (puddleSolutionContent.Reagent.Prototype != "Blood")
                    continue;

                var blood = puddleSolution.Value.Comp.Solution.SplitSolutionWithOnly(
                    puddleSolutionContent.Quantity, puddleSolutionContent.Reagent.Prototype);

                if (blood.Volume == 0)
                    continue;

                absorbBlood.AddSolution(blood, _prototypeManager);
                Spawn("CultTileSpawnEffect", Transform(puddle).Coordinates);

                var ev = new SolutionContainerChangedEvent(puddleSolution.Value.Comp.Solution, solution);
                RaiseLocalEvent(puddle, ref ev);
            }
        }

        if (absorbBlood.Volume == 0)
            return 0;

        var getCharges = absorbBlood.Volume / 2;
        _popupSystem.PopupEntity($"Собрано {getCharges} зарядов",
            user, user, PopupType.Large);
        _audioSystem.PlayPvs(bloodSpell.BloodAbsorbSound, user, bloodSpell.BloodAbsorbSound.Params);
        absorbBlood.RemoveAllSolution();
        return getCharges;
    }

    private bool HealCultist(EntityUid target, EntityUid user, BloodCultistComponent bloodCultistComponent, CultBloodSpellComponent bloodSpell)
    {
        var selfHeal = target == user;

        var availableCharges = bloodCultistComponent.BloodCharges;

        if (availableCharges <= 0)
            return false;

        var fillBlood = FixedPoint2.Zero;

        if (TryComp<BloodstreamComponent>(target, out var bloodstreamComponent))
        {
            if (_solutionSystem.ResolveSolution(target, bloodstreamComponent.BloodSolutionName,
                    ref bloodstreamComponent.BloodSolution, out var bloodSolution))
            {
                var lossBlood = bloodSolution.MaxVolume - bloodSolution.Volume;
                if (lossBlood > 0)
                {
                    fillBlood = FixedPoint2.Min(lossBlood, availableCharges / 2);
                    _bloodstreamSystem.TryModifyBloodLevel(target, fillBlood, bloodstreamComponent);
                    availableCharges -= fillBlood * 2;
                    bloodCultistComponent.BloodCharges -= fillBlood * 2;
                }
            }
        }

        var totalHeal = FixedPoint2.Zero;

        var healingDamage = new DamageSpecifier();

        if (TryComp<DamageableComponent>(target, out var damageableComponent))
        {
            var totalDamage = FixedPoint2.Zero;

            if (selfHeal)
                availableCharges /= 1.65f;

            foreach (var (damageGroup, damage) in damageableComponent.DamagePerGroup.ToList())
            {
                if (!bloodSpell.HealingGroups.Contains(damageGroup))
                    continue;

                totalDamage += damage;
            }

            foreach (var (damageGroup, damage) in damageableComponent.DamagePerGroup.ToList())
            {
                if (availableCharges <= 0)
                    break;

                if (!bloodSpell.HealingGroups.Contains(damageGroup))
                    continue;

                var damageGroupSpecifier = _prototypeManager.Index<DamageGroupPrototype>(damageGroup);

                // Calculate the total damage in the group
                var totalDamageInGroup = FixedPoint2.Zero;

                foreach (var damageType in damageGroupSpecifier.DamageTypes)
                {
                    totalDamageInGroup += damageableComponent.Damage.DamageDict[damageType];
                }

                if (totalDamageInGroup == 0 || totalDamage == 0)
                    continue;

                // Distribute healing proportionally to the total damage in the group
                var proportionalHealGroup = (availableCharges * (damage / totalDamage));

                // Ensure that the proportionalHealGroup does not exceed the availableHeal
                proportionalHealGroup = FixedPoint2.Min(proportionalHealGroup, availableCharges);

                // Update availableHeal by subtracting the allocated healing for the group
                availableCharges -= proportionalHealGroup;

                // Distribute healing within the group proportionally to each type
                foreach (var damageType in damageGroupSpecifier.DamageTypes.ToList())
                {
                    var damageInType = damageableComponent.Damage.DamageDict[damageType];

                    // Calculate the proportional share of healing for the current damageType
                    var proportionalHealType = (proportionalHealGroup * (damageInType / totalDamageInGroup));

                    // Ensure that the proportionalHealType does not exceed the availableHeal for the type
                    proportionalHealType = FixedPoint2.Min(proportionalHealType, damageInType);

                    // Update the healingDamage dictionary with the proportionalHealType for the current damageType
                    healingDamage.DamageDict.Add(damageType, -proportionalHealType);
                    totalHeal += proportionalHealType;
                }
            }
        }

        if (totalHeal == 0 && fillBlood == 0)
        {
            return false;
        }

        var usedCharges = totalHeal;
        if (selfHeal)
            usedCharges *= 1.65;

        bloodCultistComponent.BloodCharges -= usedCharges;
        _damageableSystem.TryChangeDamage(target, healingDamage, ignoreResistances: true);
        _popupSystem.PopupEntity($"Излечено {totalHeal} урона и восстановлено {fillBlood} крови",
            user, user, PopupType.Large);
        _audioSystem.PlayPvs(bloodSpell.BloodAbsorbSound, user, bloodSpell.BloodAbsorbSound.Params);
        return true;
    }

    private void OnGotUnequippedHand(EntityUid uid, CultBloodSpellComponent component, GotUnequippedHandEvent args)
    {
        QueueDel(uid);
    }
}

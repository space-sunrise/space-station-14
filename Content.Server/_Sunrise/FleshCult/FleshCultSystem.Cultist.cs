using System.Linq;
using Content.Server.Atmos.Components;
using Content.Server.Body.Components;
using Content.Server.Flash.Components;
using Content.Server.Forensics;
using Content.Server.Temperature.Components;
using Content.Shared._Sunrise.CollectiveMind;
using Content.Shared._Sunrise.FleshCult;
using Content.Shared.Actions;
using Content.Shared.Body.Part;
using Content.Shared.Chemistry.Components;
using Content.Shared.Cuffs.Components;
using Content.Shared.Electrocution;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.Components;
using Content.Shared.Humanoid;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory.Events;
using Content.Shared.Mobs;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Content.Shared.Sunrise.CollectiveMind;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.FleshCult;

public sealed partial class FleshCultSystem
{
    [ValidatePrototypeId<CollectiveMindPrototype>]
    private const string FleshCollectiveMindProto = "FleshCult";

    [ValidatePrototypeId<TagPrototype>]
    private const string FleshTagProto = "Flesh";

    [ValidatePrototypeId<EntityPrototype>]
    private const string DefaultFleshCultRule = "FleshCult";

    [ValidatePrototypeId<EntityPrototype>]
    private const string CreateFleshHeartObjective = "CreateFleshHeartObjective";

    [ValidatePrototypeId<EntityPrototype>]
    private const string FleshCultSurviveObjective = "FleshCultSurviveObjective";

    [ValidatePrototypeId<CurrencyPrototype>]
    private const string StolenMutationPointPrototype = "StolenMutationPoint";

    private void InitializeCultist()
    {
        SubscribeLocalEvent<FleshCultistComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<FleshCultistComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<FleshCultistComponent, FleshCultistInsulatedImmunityMutationEvent>(OnInsulatedImmunityMutation);
        SubscribeLocalEvent<FleshCultistComponent, FleshCultistPressureImmunityMutationEvent>(OnPressureImmunityMutation);
        SubscribeLocalEvent<FleshCultistComponent, FleshCultistFlashImmunityMutationEvent>(OnFlashImmunityMutation);
        SubscribeLocalEvent<FleshCultistComponent, FleshCultistRespiratorImmunityMutationEvent>(OnRespiratorImmunityMutation);
        SubscribeLocalEvent<FleshCultistComponent, FleshCultistColdTempImmunityMutationEvent>(OnColdTempImmunityMutation);
        SubscribeLocalEvent<FleshCultistComponent, IsEquippingAttemptEvent>(OnBeingEquippedAttempt);
        SubscribeLocalEvent<FleshCultistComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<FleshCultistComponent, FleshCultistShopActionEvent>(OnShop);
    }

    private void OnShop(EntityUid uid, FleshCultistComponent component, FleshCultistShopActionEvent args)
    {
        if (!TryComp<StoreComponent>(uid, out var store))
            return;
        _store.ToggleUi(uid, uid, store);
    }

    private void OnMobStateChanged(EntityUid uid, FleshCultistComponent component, MobStateChangedEvent args)
    {
        switch (args.NewMobState)
        {
            case MobState.Critical:
            {
                EnsureComp<CuffableComponent>(uid);
                var hands = _handsSystem.EnumerateHands(uid);
                var enumerateHands = hands as Hand[] ?? hands.ToArray();
                foreach (var enumerateHand in enumerateHands)
                {
                    if (enumerateHand.Container == null)
                        continue;
                    foreach (var containerContainedEntity in enumerateHand.Container.ContainedEntities)
                    {
                        if (HasComp<FleshHandModComponent>(containerContainedEntity))
                            continue;
                        QueueDel(containerContainedEntity);
                        _audioSystem.PlayPvs(component.SoundMutation, uid, component.SoundMutation.Params);
                    }
                }

                break;
            }
            case MobState.Dead:
            {
                _inventory.TryGetSlotEntity(uid, "shoes", out var shoes);
                if (shoes != null)
                {
                    if (HasComp<FleshBodyModComponent>(shoes))
                    {
                        EntityManager.DeleteEntity(shoes.Value);
                        _movement.RefreshMovementSpeedModifiers(uid);
                        _audioSystem.PlayPvs(component.SoundMutation, uid, component.SoundMutation.Params);
                    }
                }

                _inventory.TryGetSlotEntity(uid, "outerClothing", out var outerClothing);
                if (outerClothing != null)
                {
                    if (HasComp<FleshBodyModComponent>(outerClothing))
                    {
                        EntityManager.DeleteEntity(outerClothing.Value);
                        _movement.RefreshMovementSpeedModifiers(uid);
                        _audioSystem.PlayPvs(component.SoundMutation, uid, component.SoundMutation.Params);
                    }
                }

                ParasiteComesOut(uid, component);
                break;
            }
        }
    }

    private void OnBeingEquippedAttempt(EntityUid uid, FleshCultistComponent component, IsEquippingAttemptEvent args)
    {
        if (args.Slot is not ("socks" or "outerClothing"))
            return;
        _inventory.TryGetSlotEntity(uid, "shoes", out var shoes);
        if (shoes == null)
            return;
        if (HasComp<FleshBodyModComponent>(shoes))
            return;
        if (args.Slot is "outerClothing" && !_tagSystem.HasTag(args.Equipment, "FullBodyOuter"))
            return;
        _popup.PopupEntity(Loc.GetString("flesh-cultist-equiped-outer-clothing-blocked",
            ("Entity", uid)), uid, PopupType.Large);
        args.Cancel();

    }

    private void OnStartup(EntityUid uid, FleshCultistComponent component, ComponentStartup args)
    {
        ChangeParasiteHunger(uid, 0, component);

        if (TryComp(uid, out ActionsComponent? actionsComponent))
        {
            _action.AddAction(uid, ref component.ActionFleshCultistShopEntity, component.ActionFleshCultistShop, component: actionsComponent);
            var fleshAbilities = EnsureComp<FleshAbilitiesComponent>(uid);
            var devourAction = _action.AddAction(uid, fleshAbilities.ActionFleshCultistDevourId, component: actionsComponent);
            if (devourAction != null)
                fleshAbilities.Actions.Add(devourAction.Value);
        }

        var storeComp = EnsureComp<StoreComponent>(uid);

        var collectiveMindComponent = EnsureComp<CollectiveMindComponent>(uid);
        if (!collectiveMindComponent.Minds.Contains(FleshCollectiveMindProto))
            collectiveMindComponent.Minds.Add(FleshCollectiveMindProto);

        storeComp.Categories.Add("FleshCultistPassiveSkills");
        storeComp.Categories.Add("FleshCultistActiveSkills");
        storeComp.Categories.Add("FleshCultistWeapon");
        storeComp.Categories.Add("FleshCultistArmor");
        storeComp.CurrencyWhitelist.Add(StolenMutationPointPrototype);
        storeComp.BuySuccessSound = component.BuySuccesSound;
        storeComp.RefundAllowed = false;

        EnsureComp<IgnoreFleshSpiderWebComponent>(uid);

        _tagSystem.AddTag(uid, FleshTagProto);

        if (TryComp<HumanoidAppearanceComponent>(uid, out var appearance))
        {
            appearance.HideLayersOnEquip.Add(HumanoidVisualLayers.RLeg);
            appearance.HideLayersOnEquip.Add(HumanoidVisualLayers.LLeg);
            appearance.HideLayersOnEquip.Add(HumanoidVisualLayers.RFoot);
            appearance.HideLayersOnEquip.Add(HumanoidVisualLayers.LFoot);
            Dirty(uid, appearance);
        }

        _store.TryAddCurrency(new Dictionary<string, FixedPoint2>
            { {StolenMutationPointPrototype, component.StartingMutationPoints} },
            uid);
    }

    private void OnShutdown(EntityUid uid, FleshCultistComponent component, ComponentShutdown args)
    {
        if (TryComp(uid, out ActionsComponent? actionsComponent) && TryComp(uid, out FleshAbilitiesComponent? abilitiesComponent))
        {
            _action.RemoveAction(uid, component.ActionFleshCultistShopEntity, comp: actionsComponent);

            foreach (var abilitiesComponentAction in abilitiesComponent.Actions)
            {
                _action.RemoveAction(uid, abilitiesComponentAction, comp: actionsComponent);
            }
        }

        RemCompDeferred<IgnoreFleshSpiderWebComponent>(uid);
        RemCompDeferred<InsulatedComponent>(uid);
        RemCompDeferred<FlashImmunityComponent>(uid);
        RemCompDeferred<RespiratorImmunityComponent>(uid);
        RemCompDeferred<PressureImmunityComponent>(uid);
        RemCompDeferred<FlashImmunityComponent>(uid);

        if (TryComp(uid, out CollectiveMindComponent? collectiveMind))
        {
            if (collectiveMind.Minds.Contains(FleshCollectiveMindProto))
                collectiveMind.Minds.Remove(FleshCollectiveMindProto);
        }

        _alerts.ClearAlert(uid, component.MutationPointAlert);

        _tagSystem.RemoveTag(uid, FleshTagProto);

        if (_mindSystem.TryGetMind(uid, out var mindId, out var mind))
        {
            _roles.MindRemoveRole<FleshCultistRoleComponent>((mindId, mind));

            var indexesToRemove = new List<int>();

            for (var i = 0; i < mind.Objectives.Count; i++)
            {
                var mindObjective = mind.Objectives[i];
                var prototypeId = MetaData(mindObjective).EntityPrototype!.ID;

                if (prototypeId is CreateFleshHeartObjective || prototypeId is FleshCultSurviveObjective)
                {
                    indexesToRemove.Add(i);
                }
            }

            for (var i = indexesToRemove.Count - 1; i >= 0; i--)
            {
                _mindSystem.TryRemoveObjective(mindId, mind, indexesToRemove[i]);
            }
        }
    }

    private void OnInsulatedImmunityMutation(EntityUid uid, FleshCultistComponent component,
        FleshCultistInsulatedImmunityMutationEvent args)
    {
        EnsureComp<InsulatedComponent>(uid);
    }


    private void OnPressureImmunityMutation(EntityUid uid, FleshCultistComponent component,
        FleshCultistPressureImmunityMutationEvent args)
    {
        EnsureComp<PressureImmunityComponent>(uid);
    }

    private void OnFlashImmunityMutation(EntityUid uid, FleshCultistComponent component,
        FleshCultistFlashImmunityMutationEvent args)
    {
        EnsureComp<FlashImmunityComponent>(uid);
    }

    private void OnRespiratorImmunityMutation(EntityUid uid, FleshCultistComponent component,
        FleshCultistRespiratorImmunityMutationEvent args)
    {
        EnsureComp<RespiratorImmunityComponent>(uid);
    }

    private void OnColdTempImmunityMutation(EntityUid uid, FleshCultistComponent component,
        FleshCultistColdTempImmunityMutationEvent args)
    {
        if (TryComp<TemperatureComponent>(uid, out var tempComponent))
        {
            tempComponent.ColdDamageThreshold = 0;
        }
    }

    private bool ChangeParasiteHunger(EntityUid uid, FixedPoint2 amount, FleshCultistComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        component.Hunger += amount;

        if (TryComp<StoreComponent>(uid, out var store))
            _store.UpdateUserInterface(uid, uid, store);

        _alerts.ShowAlert(uid, component.MutationPointAlert, (short) Math.Clamp(Math.Round(component.Hunger.Float() / 10f), 0, 16));

        return true;
    }

    private int MatchSaturation(int bloodVolume, bool hasAppearance)
    {
        if (hasAppearance)
        {
            return 80;
        }
        return bloodVolume switch
        {
            >= 300 => 60,
            >= 150 => 40,
            >= 100 => 20,
            _ => 10
        };
    }

    private int MatchEvolutionPoint(int bloodVolume, bool hasAppearance)
    {
        if (hasAppearance)
        {
            return 30;
        }
        return bloodVolume switch
        {
            >= 300 => 20,
            >= 150 => 15,
            >= 100 => 10,
            _ => 0
        };
    }

    private float MatchHealPoint(int bloodVolume, bool hasAppearance)
    {
        if (hasAppearance)
        {
            return 1;
        }
        return bloodVolume switch
        {
            >= 300 => 0.8f,
            >= 150 => 0.6f,
            >= 100 => 0.4f,
            _ => 0.2f
        };
    }

    private bool ParasiteComesOut(EntityUid uid, FleshCultistComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        var xform = Transform(uid);
        var coordinates = xform.Coordinates;

        var abommob = Spawn(component.FleshMutationMobId, _transformSystem.GetMapCoordinates(uid));

        if (_mindSystem.TryGetMind(uid, out var mindId, out var mind))
        {
            _mindSystem.TransferTo(mindId, abommob, ghostCheckOverride: true);
        }

        _popup.PopupEntity(Loc.GetString("flesh-pudge-transform-user", ("EntityTransform", uid)),
            uid, uid, PopupType.LargeCaution);
        _popup.PopupEntity(Loc.GetString("flesh-pudge-transform-others",
            ("Entity", uid), ("EntityTransform", abommob)), abommob, Filter.PvsExcept(abommob),
            true, PopupType.LargeCaution);

        _audioSystem.PlayPvs(component.SoundMutation, coordinates, AudioParams.Default.WithVariation(0.025f));

        if (TryComp(uid, out ContainerManagerComponent? container))
        {
            foreach (var cont in container.GetAllContainers().ToArray())
            {
                foreach (var ent in cont.ContainedEntities.ToArray())
                {
                    if (HasComp<BodyPartComponent>(ent))
                        continue;
                    if (HasComp<UnremoveableComponent>(ent))
                        continue;
                    _containerSystem.Remove(ent, cont, force: true);
                    Transform(ent).Coordinates = coordinates;
                }
            }
        }

        if (TryComp<BloodstreamComponent>(uid, out var bloodstream))
        {
            var tempSol = new Solution() { MaxVolume = 5 };

            if (bloodstream.BloodSolution == null)
                return false;

            tempSol.AddSolution(bloodstream.BloodSolution.Value.Comp.Solution, _prototypeManager);

            if (_puddleSystem.TrySpillAt(uid, tempSol.SplitSolution(50), out var puddleUid))
            {
                if (TryComp<DnaComponent>(uid, out var dna))
                {
                    var comp = EnsureComp<ForensicsComponent>(puddleUid);
                    comp.DNAs.Add(dna.DNA);
                }
            }
        }

        QueueDel(uid);
        return true;
    }

    public void UpdateCultist(float frameTime)
    {
        base.Update(frameTime);
        var curTime = _timing.CurTime;

        foreach (var rev in EntityQuery<FleshCultistComponent>())
        {
            rev.Accumulator += frameTime;

            if (rev.Accumulator <= 1)
                continue;
            rev.Accumulator -= 1;

            if (rev.Hunger <= 40)
            {
                rev.AccumulatorStarveNotify += 1;
                if (rev.AccumulatorStarveNotify > 30)
                {
                    rev.AccumulatorStarveNotify = 0;
                    _popup.PopupEntity(Loc.GetString("flesh-cultist-hungry"),
                        rev.Owner, rev.Owner, PopupType.Large);
                }
            }

            if (rev.Hunger < 0)
            {
                ParasiteComesOut(rev.Owner, rev);
            }

            ChangeParasiteHunger(rev.Owner, rev.HungerСonsumption, rev);
        }
    }
}

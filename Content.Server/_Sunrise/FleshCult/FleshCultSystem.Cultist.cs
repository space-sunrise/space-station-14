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
using Content.Shared.Forensics.Components;
using Content.Shared.Humanoid;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory.Events;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.FleshCult;

/// <summary>
/// System for managing Flesh Cultist components and their related events and behaviors.
/// </summary>
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
        if (TryComp<StoreComponent>(uid, out var store))
            _store.ToggleUi(uid, uid, store);
    }

    private void OnMobStateChanged(EntityUid uid, FleshCultistComponent component, MobStateChangedEvent args)
    {
        switch (args.NewMobState)
        {
            case MobState.Critical:
                HandleCriticalState(uid, component);
                break;
            case MobState.Dead:
                HandleDeadState(uid, component);
                break;
        }
    }

    private void HandleCriticalState(EntityUid uid, FleshCultistComponent component)
    {
        EnsureComp<CuffableComponent>(uid);
        foreach (var hand in _handsSystem.EnumerateHands(uid).Where(hand => hand.Container != null))
        {
            foreach (var entity in hand.Container!.ContainedEntities.Where(entity => !HasComp<FleshHandModComponent>(entity)))
            {
                QueueDel(entity);
                _audioSystem.PlayPvs(component.SoundMutation, uid, component.SoundMutation.Params);
            }
        }
    }

    private void HandleDeadState(EntityUid uid, FleshCultistComponent component)
    {
        DeleteFleshBodyModComponent(uid, "shoes", component);
        DeleteFleshBodyModComponent(uid, "outerClothing", component);
        ParasiteComesOut(uid, component);
    }

    private void DeleteFleshBodyModComponent(EntityUid uid, string slot, FleshCultistComponent component)
    {
        if (_inventory.TryGetSlotEntity(uid, slot, out var entity) && HasComp<FleshBodyModComponent>(entity))
        {
            EntityManager.DeleteEntity(entity.Value);
            _movement.RefreshMovementSpeedModifiers(uid);
            _audioSystem.PlayPvs(component.SoundMutation, uid, component.SoundMutation.Params);
        }
    }

    private void OnBeingEquippedAttempt(EntityUid uid, FleshCultistComponent component, IsEquippingAttemptEvent args)
    {
        if (args.Slot is not "outerClothing")
            return;
        _inventory.TryGetSlotEntity(uid, "shoes", out var shoes);
        if (shoes == null)
            return;
        if (!HasComp<FleshBodyModComponent>(shoes))
            return;
        if (!_tagSystem.HasTag(args.Equipment, "FullBodyOuter"))
            return;
        _popup.PopupEntity(Loc.GetString("flesh-cultist-equiped-outer-clothing-blocked",
            ("Entity", uid)), uid, PopupType.Large);
        args.Cancel();
    }

    private void OnStartup(EntityUid uid, FleshCultistComponent component, ComponentStartup args)
    {
        ChangeParasiteHunger(uid, 0, component);
        InitializeActions(uid, component);
        InitializeStore(uid, component);
        InitializeAppearance(uid);
        _store.TryAddCurrency(new Dictionary<string, FixedPoint2> { { StolenMutationPointPrototype, component.StartingMutationPoints } }, uid);
    }

    private void InitializeActions(EntityUid uid, FleshCultistComponent component)
    {
        if (TryComp(uid, out ActionsComponent? actionsComponent))
        {
            _action.AddAction(uid, ref component.ActionFleshCultistShopEntity, component.ActionFleshCultistShop, component: actionsComponent);
            var fleshAbilities = EnsureComp<FleshAbilitiesComponent>(uid);
            var devourAction = _action.AddAction(uid, fleshAbilities.ActionFleshCultistDevourId, component: actionsComponent);
            if (devourAction != null)
                fleshAbilities.Actions.Add(devourAction.Value);
        }
    }

    private void InitializeStore(EntityUid uid, FleshCultistComponent component)
    {
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
    }

    private void InitializeAppearance(EntityUid uid)
    {
        if (TryComp<HumanoidAppearanceComponent>(uid, out var appearance))
        {
            appearance.HideLayersOnEquip.Add(HumanoidVisualLayers.RLeg);
            appearance.HideLayersOnEquip.Add(HumanoidVisualLayers.LLeg);
            appearance.HideLayersOnEquip.Add(HumanoidVisualLayers.RFoot);
            appearance.HideLayersOnEquip.Add(HumanoidVisualLayers.LFoot);
            Dirty(uid, appearance);
        }
    }

    private void OnShutdown(EntityUid uid, FleshCultistComponent component, ComponentShutdown args)
    {
        RemoveActions(uid, component);
        RemoveComponents(uid);
        RemoveCollectiveMind(uid);
        _alerts.ClearAlert(uid, component.MutationPointAlert);
        _tagSystem.RemoveTag(uid, FleshTagProto);
        RemoveObjectives(uid);
    }

    private void RemoveActions(EntityUid uid, FleshCultistComponent component)
    {
        if (TryComp(uid, out ActionsComponent? actionsComponent) && TryComp(uid, out FleshAbilitiesComponent? abilitiesComponent))
        {
            _action.RemoveAction(uid, component.ActionFleshCultistShopEntity, comp: actionsComponent);
            foreach (var action in abilitiesComponent.Actions)
                _action.RemoveAction(uid, action, comp: actionsComponent);
        }
    }

    private void RemoveComponents(EntityUid uid)
    {
        RemCompDeferred<IgnoreFleshSpiderWebComponent>(uid);
        RemCompDeferred<InsulatedComponent>(uid);
        RemCompDeferred<FlashImmunityComponent>(uid);
        RemCompDeferred<RespiratorImmunityComponent>(uid);
        RemCompDeferred<PressureImmunityComponent>(uid);
        RemCompDeferred<FlashImmunityComponent>(uid);
    }

    private void RemoveCollectiveMind(EntityUid uid)
    {
        if (TryComp(uid, out CollectiveMindComponent? collectiveMind) && collectiveMind.Minds.Contains(FleshCollectiveMindProto))
            collectiveMind.Minds.Remove(FleshCollectiveMindProto);
    }

    private void RemoveObjectives(EntityUid uid)
    {
        if (_mindSystem.TryGetMind(uid, out var mindId, out var mind))
        {
            _roles.MindRemoveRole<FleshCultistRoleComponent>((mindId, mind));
            var indexesToRemove = mind.Objectives
                .Select((objective, index) => new { objective, index })
                .Where(x => MetaData(x.objective).EntityPrototype!.ID is CreateFleshHeartObjective or FleshCultSurviveObjective)
                .Select(x => x.index)
                .ToList();

            foreach (var index in indexesToRemove.OrderByDescending(i => i))
                _mindSystem.TryRemoveObjective(mindId, mind, index);
        }
    }

    private void OnInsulatedImmunityMutation(EntityUid uid, FleshCultistComponent component, FleshCultistInsulatedImmunityMutationEvent args)
    {
        EnsureComp<InsulatedComponent>(uid);
    }

    private void OnPressureImmunityMutation(EntityUid uid, FleshCultistComponent component, FleshCultistPressureImmunityMutationEvent args)
    {
        EnsureComp<PressureImmunityComponent>(uid);
    }

    private void OnFlashImmunityMutation(EntityUid uid, FleshCultistComponent component, FleshCultistFlashImmunityMutationEvent args)
    {
        EnsureComp<FlashImmunityComponent>(uid);
    }

    private void OnRespiratorImmunityMutation(EntityUid uid, FleshCultistComponent component, FleshCultistRespiratorImmunityMutationEvent args)
    {
        EnsureComp<RespiratorImmunityComponent>(uid);
    }

    private void OnColdTempImmunityMutation(EntityUid uid, FleshCultistComponent component, FleshCultistColdTempImmunityMutationEvent args)
    {
        if (TryComp<TemperatureComponent>(uid, out var tempComponent))
            tempComponent.ColdDamageThreshold = 0;
    }

    private bool ChangeParasiteHunger(EntityUid uid, FixedPoint2 amount, FleshCultistComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        component.Hunger += amount;
        if (TryComp<StoreComponent>(uid, out var store))
            _store.UpdateUserInterface(uid, uid, store);

        _alerts.ShowAlert(uid, component.MutationPointAlert, (short)Math.Clamp(Math.Round(component.Hunger.Float() / 10f), 0, 16));
        return true;
    }

    private bool ParasiteComesOut(EntityUid uid, FleshCultistComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        var coordinates = Transform(uid).Coordinates;
        var abommob = Spawn(component.FleshMutationMobId, _transformSystem.GetMapCoordinates(uid));

        if (_mindSystem.TryGetMind(uid, out var mindId, out var mind))
            _mindSystem.TransferTo(mindId, abommob, ghostCheckOverride: true);

        _popup.PopupEntity(Loc.GetString("flesh-pudge-transform-user", ("EntityTransform", uid)), uid, uid, PopupType.LargeCaution);
        _popup.PopupEntity(Loc.GetString("flesh-pudge-transform-others", ("Entity", uid), ("EntityTransform", abommob)), abommob, Filter.PvsExcept(abommob), true, PopupType.LargeCaution);
        _audioSystem.PlayPvs(component.SoundMutation, coordinates, AudioParams.Default.WithVariation(0.025f));

        if (TryComp(uid, out ContainerManagerComponent? container))
        {
            foreach (var cont in container.GetAllContainers().ToArray())
            {
                foreach (var entity in cont.ContainedEntities.Where(entity => !HasComp<BodyPartComponent>(entity) && !HasComp<UnremoveableComponent>(entity)))
                {
                    _containerSystem.Remove(entity, cont, force: true);
                    Transform(entity).Coordinates = coordinates;
                }
            }
        }

        if (TryComp<BloodstreamComponent>(uid, out var bloodstream) && bloodstream.BloodSolution != null)
        {
            var tempSol = new Solution { MaxVolume = 5 };
            tempSol.AddSolution(bloodstream.BloodSolution.Value.Comp.Solution, _prototypeManager);

            if (_puddleSystem.TrySpillAt(uid, tempSol.SplitSolution(50), out var puddleUid) && TryComp<DnaComponent>(uid, out var dna) && dna.DNA != null)
            {
                var comp = EnsureComp<ForensicsComponent>(puddleUid);
                comp.DNAs.Add(dna.DNA);
            }
        }

        QueueDel(uid);
        return true;
    }

    public void UpdateCultist(float frameTime)
    {
        base.Update(frameTime);
        foreach (var cultist in EntityQuery<FleshCultistComponent>())
        {
            cultist.Accumulator += frameTime;
            if (cultist.Accumulator <= 1)
                continue;

            cultist.Accumulator -= 1;
            if (cultist.Hunger <= 40)
            {
                cultist.AccumulatorStarveNotify += 1;
                if (cultist.AccumulatorStarveNotify > 30)
                {
                    cultist.AccumulatorStarveNotify = 0;
                    _popup.PopupEntity(Loc.GetString("flesh-cultist-hungry"), cultist.Owner, cultist.Owner, PopupType.Large);
                }
            }

            if (cultist.Hunger < 0)
                ParasiteComesOut(cultist.Owner, cultist);

            ChangeParasiteHunger(cultist.Owner, cultist.HungerСonsumption, cultist);
        }
    }
}

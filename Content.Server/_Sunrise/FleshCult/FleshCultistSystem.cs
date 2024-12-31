using System.Linq;
using Content.Server._Sunrise.FleshCult.GameRule;
using Content.Server.Actions;
using Content.Server.Atmos.Components;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Chat.Systems;
using Content.Server.Cuffs;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Flash.Components;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Forensics;
using Content.Server.Humanoid;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Server.Store.Systems;
using Content.Server.Sunrise.FleshCult;
using Content.Server.Temperature.Components;
using Content.Server.Traits.Assorted;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared._Sunrise.FleshCult;
using Content.Shared.Alert;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Cuffs.Components;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Electrocution;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Jittering;
using Content.Shared.Mind.Components;
using Content.Shared.Mindshield.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.Speech.EntitySystems;
using Content.Shared.Store.Components;
using Content.Shared.Stunnable;
using Content.Shared.Sunrise.CollectiveMind;
using Content.Shared.Tag;
using Content.Shared.Throwing;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Collections;
using Robust.Shared.Containers;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Sunrise.FleshCult;

public sealed partial class FleshCultistSystem : SharedFleshCultistSystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly ActionsSystem _action = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly PuddleSystem _puddleSystem = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _sharedHuApp = default!;
    [Dependency] private readonly SharedAppearanceSystem _sharedAppearance = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private readonly GunSystem _gunSystem = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly CuffableSystem _cuffable = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedStunSystem _stunSystem = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly FleshCultRuleSystem _fleshCultRule = default!;
    [Dependency] private readonly SharedJitteringSystem _jittering = default!;
    [Dependency] private readonly SharedStutteringSystem _stuttering = default!;
    [Dependency] private readonly ExplosionSystem _explosionSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly ContainerSystem _containerSystem = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FleshCultistComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<FleshCultistComponent, FleshCultistShopActionEvent>(OnShop);
        SubscribeLocalEvent<FleshCultistComponent, FleshCultistInsulatedImmunityMutationEvent>(OnInsulatedImmunityMutation);
        SubscribeLocalEvent<FleshCultistComponent, FleshCultistPressureImmunityMutationEvent>(OnPressureImmunityMutation);
        SubscribeLocalEvent<FleshCultistComponent, FleshCultistFlashImmunityMutationEvent>(OnFlashImmunityMutation);
        SubscribeLocalEvent<FleshCultistComponent, FleshCultistRespiratorImmunityMutationEvent>(OnRespiratorImmunityMutation);
        SubscribeLocalEvent<FleshCultistComponent, FleshCultistColdTempImmunityMutationEvent>(OnColdTempImmunityMutation);
        SubscribeLocalEvent<FleshCultistComponent, FleshCultistDevourActionEvent>(OnDevourAction);
        SubscribeLocalEvent<FleshCultistComponent, FleshCultistDevourDoAfterEvent>(OnDevourDoAfter);
        SubscribeLocalEvent<FleshCultistComponent, IsEquippingAttemptEvent>(OnBeingEquippedAttempt);
        SubscribeLocalEvent<FleshCultistComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<FleshCultistComponent, FleshCultistAbsorbBloodPoolActionEvent>(OnAbsormBloodPoolActionEvent);

        SubscribeLocalEvent<PendingFleshCultistComponent, MapInitEvent>(OnPendingMapInit);

        InitializeAbilities();
    }

    private void OnPendingMapInit(EntityUid uid, PendingFleshCultistComponent component, MapInitEvent args)
    {
        component.NextParalyze = _timing.CurTime + TimeSpan.FromSeconds(1f);
        component.NextScream = _timing.CurTime + TimeSpan.FromSeconds(1f);
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
                        if (HasComp<_Sunrise.FleshCult.FleshHandModComponent>(containerContainedEntity))
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
                    if (TryComp(shoes, out MetaDataComponent? metaData))
                    {
                        if (metaData.EntityPrototype != null)
                        {
                            if (metaData.EntityPrototype.ID == component.SpiderLegsSpawnId)
                            {
                                EntityManager.DeleteEntity(shoes.Value);
                                _movement.RefreshMovementSpeedModifiers(uid);
                                _audioSystem.PlayPvs(component.SoundMutation, uid, component.SoundMutation.Params);
                            }
                        }
                    }
                }

                _inventory.TryGetSlotEntity(uid, "outerClothing", out var outerClothing);
                if (outerClothing != null)
                {
                    if (TryComp(outerClothing, out MetaDataComponent? metaData))
                    {
                        if (metaData.EntityPrototype != null)
                        {
                            if (metaData.EntityPrototype.ID == component.ArmorSpawnId ||
                                metaData.EntityPrototype.ID == component.HeavyArmorSpawnId)
                            {
                                EntityManager.DeleteEntity(outerClothing.Value);
                                _movement.RefreshMovementSpeedModifiers(uid);
                                _audioSystem.PlayPvs(component.SoundMutation, uid, component.SoundMutation.Params);
                            }
                        }
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
        if (!TryComp(shoes, out MetaDataComponent? metaData))
            return;
        if (metaData.EntityPrototype == null)
            return;
        if (metaData.EntityPrototype.ID != component.SpiderLegsSpawnId)
            return;
        if (args.Slot is "outerClothing" && !_tagSystem.HasTag(args.Equipment, "FullBodyOuter"))
            return;
        _popupSystem.PopupEntity(Loc.GetString("flesh-cultist-equiped-outer-clothing-blocked",
            ("Entity", uid)), uid, PopupType.Large);
        args.Cancel();

    }

    private void OnStartup(EntityUid uid, FleshCultistComponent component, ComponentStartup args)
    {
        ChangeParasiteHunger(uid, 0, component);

        _action.AddAction(uid, "FleshCultistShop");
        _action.AddAction(uid, "FleshCultistDevour");
        _action.AddAction(uid, "FleshCultistAbsorbBloodPool");

        var storeComp = EnsureComp<StoreComponent>(uid);

        var collectiveMindComponent = EnsureComp<CollectiveMindComponent>(uid);
        if (!collectiveMindComponent.Minds.Contains("FleshCult"))
            collectiveMindComponent.Minds.Add("FleshCult");

        storeComp.Categories.Add("FleshCultistPassiveSkills");
        storeComp.Categories.Add("FleshCultistActiveSkills");
        storeComp.Categories.Add("FleshCultistWeapon");
        storeComp.Categories.Add("FleshCultistArmor");
        storeComp.CurrencyWhitelist.Add("StolenMutationPoint");
        storeComp.BuySuccessSound = component.BuySuccesSound;
        storeComp.RefundAllowed = false;

        EnsureComp<IgnoreFleshSpiderWebComponent>(uid);

        if (HasComp<HungerComponent>(uid))
            RemComp<HungerComponent>(uid);

        if (HasComp<ThirstComponent>(uid))
            RemComp<ThirstComponent>(uid);

        _tagSystem.AddTag(uid, "Flesh");

        if (TryComp<HumanoidAppearanceComponent>(uid, out var appearance))
        {
            appearance.HideLayersOnEquip.Add(HumanoidVisualLayers.RLeg);
            appearance.HideLayersOnEquip.Add(HumanoidVisualLayers.LLeg);
            appearance.HideLayersOnEquip.Add(HumanoidVisualLayers.RFoot);
            appearance.HideLayersOnEquip.Add(HumanoidVisualLayers.LFoot);
            Dirty(uid, appearance);
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

    private void OnShop(EntityUid uid, FleshCultistComponent component, FleshCultistShopActionEvent args)
    {
        if (!TryComp<StoreComponent>(uid, out var store))
            return;
        _store.ToggleUi(uid, uid, store);
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

    private void OnDevourAction(EntityUid uid, FleshCultistComponent component, FleshCultistDevourActionEvent args)
    {
        if (args.Handled)
            return;

        var target = args.Target;

        if (!TryComp<MobStateComponent>(target, out var targetState))
            return;
        if (!TryComp<BloodstreamComponent>(target, out var bloodstream))
            return;
        var hasAppearance = false;
        {
            switch (targetState.CurrentState)
            {
                case MobState.Dead:
                    if (EntityManager.TryGetComponent(target, out HumanoidAppearanceComponent? humanoidAppearance))
                    {
                        if (!component.SpeciesWhitelist.Contains(humanoidAppearance.Species))
                        {
                            _popupSystem.PopupEntity(
                                Loc.GetString("flesh-cultist-devout-target-not-have-flesh"),
                                uid, uid);
                            return;
                        }

                        if (TryComp<FixturesComponent>(target, out var fixturesComponent))
                        {
                            if (fixturesComponent.Fixtures["fix1"].Density <= 60)
                            {
                                _popupSystem.PopupEntity(
                                    Loc.GetString("flesh-cultist-devout-target-invalid"),
                                    uid, uid);
                                return;
                            }
                        }

                        hasAppearance = true;
                    }
                    else
                    {
                        if (bloodstream.BloodReagent != "Blood")
                        {
                            _popupSystem.PopupEntity(
                                Loc.GetString("flesh-cultist-devout-target-not-have-flesh"),
                                uid, uid);
                            return;
                        }
                        if (bloodstream.BloodMaxVolume < 30)
                        {
                            _popupSystem.PopupEntity(
                                Loc.GetString("flesh-cultist-devout-target-invalid"),
                                uid, uid);
                            return;
                        }
                    }
                    var saturation = MatchSaturation(bloodstream.BloodMaxVolume.Value / 100, hasAppearance);
                    if (component.Hunger + saturation >= component.MaxHunger)
                    {
                        _popupSystem.PopupEntity(
                            Loc.GetString("flesh-cultist-devout-not-hungry"),
                            uid, uid);
                        return;
                    }
                    _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager ,uid, component.DevourTime,
                        new FleshCultistDevourDoAfterEvent(), uid, target: target, used: uid)
                    {
                        BreakOnMove = true,
                    });
                    args.Handled = true;
                    break;

                case MobState.Invalid:
                case MobState.Critical:
                case MobState.Alive:
                default:
                    _popupSystem.PopupEntity(
                        Loc.GetString("flesh-cultist-devout-target-alive"),
                        uid, uid);
                    break;
            }
        }
    }

    private void OnDevourDoAfter(EntityUid uid, FleshCultistComponent component, FleshCultistDevourDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (args.Args.Target == null)
            return;

        if (!TryComp<BloodstreamComponent>(args.Args.Target.Value, out var bloodstream))
            return;

        var hasAppearance = false;

        var xform = Transform(args.Args.Target.Value);
        var coordinates = xform.Coordinates;
        _audioSystem.PlayPvs(component.DevourSound, coordinates, AudioParams.Default.WithVariation(0.025f).WithMaxDistance(5f));
        _popupSystem.PopupEntity(Loc.GetString("flesh-cultist-devour-target",
                ("Entity", uid), ("Target", args.Args.Target)), uid);

        if (bloodstream.BloodSolution != null)
        {
            _bloodstreamSystem.SpillAllSolutions(args.Args.Target.Value, bloodstream);
        }

        if (TryComp<HumanoidAppearanceComponent>(args.Args.Target, out var HuAppComponent))
        {
            if (TryComp(args.Args.Target.Value, out ContainerManagerComponent? container))
            {
                foreach (var cont in container.GetAllContainers().ToArray())
                {
                    foreach (var ent in cont.ContainedEntities.ToArray())
                    {
                        if (HasComp<BodyPartComponent>(ent))
                        {
                            continue;
                        }
                        _containerSystem.Remove(ent, cont, force: true);
                        Transform(ent).Coordinates = coordinates;
                    }
                }
            }

            if (TryComp<BodyComponent>(args.Args.Target, out var bodyComponent))
            {
                var parts = _body.GetBodyChildren(args.Args.Target, bodyComponent).ToArray();

                foreach (var part in parts)
                {
                    if (part.Component.PartType == BodyPartType.Head)
                        continue;

                    if (part.Component.PartType == BodyPartType.Torso)
                    {
                        foreach (var organ in _body.GetPartOrgans(part.Id, part.Component))
                        {
                            //_body.RemoveOrgan(organ.Id);
                            QueueDel(organ.Id);
                        }
                    }
                    else
                    {
                        QueueDel(part.Id);
                    }
                }
            }

            var skeletonSprites = _proto.Index<HumanoidSpeciesBaseSpritesPrototype>("MobSkeletonSprites");
            foreach (var (key, id) in skeletonSprites.Sprites)
            {
                if (key != HumanoidVisualLayers.Head)
                {
                    _sharedHuApp.SetBaseLayerId(args.Args.Target.Value, key, id, humanoid: HuAppComponent);
                }
            }

            if (TryComp<FixturesComponent>(args.Args.Target, out var fixturesComponent))
            {
                _physics.SetDensity(args.Args.Target.Value, "fix1", fixturesComponent.Fixtures["fix1"], 50);
            }

            if (TryComp<AppearanceComponent>(args.Args.Target, out var appComponent))
            {
                _sharedAppearance.SetData(args.Args.Target.Value, DamageVisualizerKeys.Disabled, true, appComponent);
            }

            hasAppearance = true;
        }

        var saturation = MatchSaturation(bloodstream.BloodMaxVolume.Value / 100, hasAppearance);
        var evolutionPoint = MatchEvolutionPoint(bloodstream.BloodMaxVolume.Value / 100, hasAppearance);
        var healPoint = MatchHealPoint(bloodstream.BloodMaxVolume.Value / 100, hasAppearance);

        RemComp<BloodstreamComponent>(args.Args.Target.Value);

        EnsureComp<UnrevivableComponent>(args.Args.Target.Value);

        if (!hasAppearance)
        {
            QueueDel(args.Args.Target.Value);
        }

        if (_solutionContainerSystem.TryGetInjectableSolution(uid, out var injectableSolution, out _))
        {
            var transferSolution = new Solution();
            foreach (var solution in component.HealDevourReagents)
            {
                transferSolution.AddReagent(solution.Reagent, solution.Quantity * healPoint);
            }
            _solutionContainerSystem.TryAddSolution(injectableSolution.Value, transferSolution);
        }

        component.Hunger += saturation;
        _store.TryAddCurrency(new Dictionary<string, FixedPoint2>
            { {component.StolenCurrencyPrototype, evolutionPoint} }, uid);
    }

    private int MatchSaturation(int bloodVolume, bool hasAppearance)
    {
        if (hasAppearance)
        {
            return 100;
        }
        return bloodVolume switch
        {
            >= 300 => 80,
            >= 150 => 60,
            >= 100 => 40,
            _ => 20
        };
    }

    private int MatchEvolutionPoint(int bloodVolume, bool hasAppearance)
    {
        if (hasAppearance)
        {
            return 20;
        }
        return bloodVolume switch
        {
            >= 300 => 15,
            >= 150 => 10,
            >= 100 => 5,
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

        _popupSystem.PopupEntity(Loc.GetString("flesh-pudge-transform-user", ("EntityTransform", uid)),
            uid, uid, PopupType.LargeCaution);
        _popupSystem.PopupEntity(Loc.GetString("flesh-pudge-transform-others",
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

            tempSol.AddSolution(bloodstream.BloodSolution.Value.Comp.Solution, _proto);

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

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var curTime = _timing.CurTime;

        var query = EntityQueryEnumerator<PendingFleshCultistComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.CurrentStage == PendingFleshCultistStage.Final)
                continue;

            comp.Accumulator += frameTime;

            var stageTimer = comp.CurrentStage switch
            {
                PendingFleshCultistStage.First => comp.FirstStageTimer,
                PendingFleshCultistStage.Second => comp.SecondStageTimer,
                _ => 0f
            };

            if (comp.Accumulator >= stageTimer)
            {
                comp.Accumulator = 0f;
                comp.CurrentStage = GetNextStage(comp.CurrentStage);
            }

            switch (comp.CurrentStage)
            {
                case PendingFleshCultistStage.First:
                    if (comp.NextScream <= curTime)
                    {
                        comp.NextScream = curTime + TimeSpan.FromSeconds(comp.ScreamInterval);
                        _chatSystem.TryEmoteWithChat(uid, "Scream");
                    }
                    if (comp.NextStutter <= curTime)
                    {
                        comp.NextStutter = curTime + TimeSpan.FromSeconds(comp.ScreamInterval);
                        _stuttering.DoStutter(uid, TimeSpan.FromSeconds(comp.StutterTime), true);
                    }
                    break;
                case PendingFleshCultistStage.Second:
                    if (comp.NextParalyze <= curTime)
                    {
                        comp.NextParalyze = curTime + TimeSpan.FromSeconds(comp.ParalyzeInterval);
                        _stunSystem.TryParalyze(uid, TimeSpan.FromSeconds(comp.ParalyzeTime), true);
                    }
                    if (comp.NextJitter <= curTime)
                    {
                        comp.NextJitter = curTime + TimeSpan.FromSeconds(comp.JitterInterval);
                        _jittering.DoJitter(uid, TimeSpan.FromSeconds(comp.JitterTime), true);
                    }
                    break;
                case PendingFleshCultistStage.Third:
                {
                    if (!TryComp<MindContainerComponent>(uid, out var targetMindComp))
                        return;

                    if (HasComp<MindShieldComponent>(uid))
                    {
                        _popupSystem.PopupEntity("Активация самоуничтожения импланта защиты разума", uid, PopupType.LargeCaution);
                        _body.GibBody(uid, true);
                        _explosionSystem.QueueExplosion(uid, "Default", 50, 5, 30, canCreateVacuum: false);
                        return;
                    }

                    var fleshCultRule = _fleshCultRule.StartGameRule();
                    _fleshCultRule.MakeCultist(uid, 0, fleshCultRule);

                    comp.CurrentStage = PendingFleshCultistStage.Final;
                    break;
                }
            }
        }

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
                    _popupSystem.PopupEntity(Loc.GetString("flesh-cultist-hungry"),
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

    private PendingFleshCultistStage GetNextStage(PendingFleshCultistStage currentStage)
    {
        return currentStage switch
        {
            PendingFleshCultistStage.First => PendingFleshCultistStage.Second,
            PendingFleshCultistStage.Second => PendingFleshCultistStage.Third,
            PendingFleshCultistStage.Third => PendingFleshCultistStage.Final,
            _ => currentStage,
        };
    }

    private void OnAbsormBloodPoolActionEvent(EntityUid uid, FleshCultistComponent component,
        FleshCultistAbsorbBloodPoolActionEvent args)
    {
        if (args.Handled)
                return;

        var xform = Transform(uid);
        var puddles = new ValueList<(EntityUid Entity, string Solution)>();
        puddles.Clear();
        foreach (var entity in _lookup.GetEntitiesInRange(xform.MapPosition, 1f))
        {
            if (TryComp<PuddleComponent>(entity, out var puddle))
            {
                puddles.Add((entity, puddle.SolutionName));
            }
        }

        if (puddles.Count == 0)
        {
            _popupSystem.PopupEntity(Loc.GetString("flesh-cultist-not-find-puddles"),
                uid, uid, PopupType.Large);
            return;
        }

        var absorbBlood = new Solution();
        foreach (var (puddle, solution) in puddles)
        {
            if (!_solutionContainerSystem.TryGetSolution(puddle, solution, out var puddleSolution))
            {
                continue;
            }
            foreach (var puddleSolutionContent in puddleSolution.Value.Comp.Solution.ToList())
            {
                if (!component.BloodWhitelist.Contains(puddleSolutionContent.Reagent.Prototype))
                    continue;

                var blood = puddleSolution.Value.Comp.Solution.SplitSolutionWithOnly(
                    puddleSolutionContent.Quantity, puddleSolutionContent.Reagent.Prototype);

                absorbBlood.AddSolution(blood, _proto);
            }

            var ev = new SolutionContainerChangedEvent(puddleSolution.Value.Comp.Solution, solution);
            RaiseLocalEvent(puddle, ref ev);
        }

        if (absorbBlood.Volume == 0)
        {
            _popupSystem.PopupEntity(Loc.GetString("flesh-cultist-cant-absorb-puddle"),
                uid, uid, PopupType.Large);
            return;
        }

        _audioSystem.PlayPvs(component.BloodAbsorbSound, uid, component.BloodAbsorbSound.Params);
        _popupSystem.PopupEntity(Loc.GetString("flesh-cultist-absorb-puddle", ("Entity", uid)),
            uid, uid, PopupType.Large);

        var transferSolution = new Solution();
        foreach (var solution in component.HealBloodAbsorbReagents)
        {
            transferSolution.AddReagent(solution.Reagent, solution.Quantity * (absorbBlood.Volume / 10));
        }

        if (_solutionContainerSystem.TryGetInjectableSolution(uid, out var injectableSolution, out var _))
        {
            _solutionContainerSystem.TryAddSolution(injectableSolution.Value, transferSolution);
        }
        absorbBlood.RemoveAllSolution();
        args.Handled = true;
    }
}

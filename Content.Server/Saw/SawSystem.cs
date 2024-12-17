using Content.Server.Nutrition;
using Content.Server.Nutrition.Components;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.Nutrition.AnimalHusbandry;
using Content.Server.Mind;
using Robust.Shared.Prototypes;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Server.Nutrition.EntitySystems;
using Content.Shared.Preferences;
using Content.Server.Humanoid;
using Content.Shared.Dataset;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs;
using Content.Shared.FixedPoint;
using Content.Server.Destructible.Thresholds;
using Content.Shared.Mobs.Systems;
using Content.Server._Sunrise.Mood;
using Content.Shared.Nutrition.Components;


namespace Content.Server.Saw;

public sealed class SawSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly MobThresholdSystem _thresholdSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FoodComponent, BeforeFullyEatenEvent>(OnBeforeFullyEaten);
        SubscribeLocalEvent<SawComponent, ComponentInit>(SawInit);
        SubscribeLocalEvent<SawComponent, BirthEvent>(OnBirth);
    }

    private void OnBeforeFullyEaten(Entity<FoodComponent> food, ref BeforeFullyEatenEvent args)
    {
        if (!TryComp(args.User, out SawComponent? sawComp) ||
            !TryComp(args.User, out ReproductiveComponent? reproductive) ||
            !TryComp(food, out MindContainerComponent? mind) ||
            !HasComp<IdentityComponent>(food))
        return;

        EntityUid? foodMind = _mindSystem.GetMind(food);
        sawComp.EatenMind = foodMind;

        if (_prototypeManager.Index<EntityPrototype>("MobSaw").Components.TryGetComponent("Reproductive", out var defaultReproductive))
            reproductive.Gestating = false;
    }

    private void SawInit(EntityUid ent, SawComponent saw, ComponentInit args)
    {
        if (TryComp(ent, out ReproductiveComponent? reproductive))
            reproductive.Gestating = true;
    }

    private void OnBirth(Entity<SawComponent> saw, ref BirthEvent args)
    {
        if (TryComp(saw, out ReproductiveComponent? reproductive))
            reproductive.Gestating = true;
        EntityUid child = args.Spawns[0];
        EntityUid? eatenMind = Comp<SawComponent>(saw).EatenMind;
        saw.Comp.EatenMind = null;
        if (eatenMind == null)
            return;
        if (!TryComp<HungerComponent>(saw, out var hungerComp))
            return;

        if (TryComp<MobThresholdsComponent>(child, out var thresholds))
        {
            FixedPoint2 thresholdModifier = hungerComp.CurrentHunger * saw.Comp.HungerToThresholdModifier;

            _thresholdSystem.SetMobStateThreshold(child, _thresholdSystem.GetThresholdForState(child, MobState.Critical) + thresholdModifier, MobState.Critical);
            _thresholdSystem.SetMobStateThreshold(child, _thresholdSystem.GetThresholdForState(child, MobState.Dead) + thresholdModifier, MobState.Dead);
            if (TryComp<MoodComponent>(child, out var mood))
                mood.CritThresholdBeforeModify = _thresholdSystem.GetThresholdForState(child, MobState.Critical);
        }

        _mindSystem.TransferTo((EntityUid) eatenMind, child);

        var profile = HumanoidCharacterProfile.RandomWithSpecies("Swine");
        _humanoid.LoadProfile(args.Spawns[0], profile);
        //_metaData.SetEntityName(args.Spawns[0], profile.Name);
        //Logger.Log(LogLevel.Debug, profile.Name);
        //_metaData.SetEntityName(args.Spawns[0], "PRIVET");
        //if (_prototypeManager.TryIndex<RandomHumanoidSettingsPrototype>("EventHumanoid", out var prototype))
        //    _metaData.SetEntityName(args.Spawns[0], prototype.RandomizeName ? profile.Name : name);

    }

}

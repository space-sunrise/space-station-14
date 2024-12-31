using Content.Shared.Alert;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.StatusIcon;
using Content.Shared.Store;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._Sunrise.FleshCult;

[RegisterComponent, NetworkedComponent]
public sealed partial class FleshCultistComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)] public FixedPoint2 Hunger = 100;

    [ViewVariables(VVAccess.ReadWrite), DataField("hungerСonsumption")]
    public FixedPoint2 HungerСonsumption = -0.05; // 200 hunger in 60 minutes

    [ViewVariables(VVAccess.ReadWrite), DataField("maxHunger")]
    public FixedPoint2 MaxHunger = 200;

    [ViewVariables(VVAccess.ReadWrite),
     DataField("bulletAcidSpawnId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string BulletAcidSpawnId = "BulletSplashAcid";

    [ViewVariables(VVAccess.ReadWrite), DataField("speciesWhitelist")]
    public List<string> SpeciesWhitelist = new()
    {
        "Human",
        "Reptilian",
        "Dwarf",
        "Vulpkanin",
        "Felinid",
        "Moth",
        "Swine",
        "Arachnid",
    };

    [DataField("adrenalinReagents")] public Solution AdrenalinReagents = new()
    {
        Contents = { new ReagentQuantity(new ReagentId("Ephedrine", null), 10) }
    };

    [DataField("healDevourReagents")] public Solution HealDevourReagents = new()
    {
        Contents =
        {
            new ReagentQuantity(new ReagentId("Carol", null), 20),
        }
    };

    [DataField("healBloodAbsorbReagents")] public Solution HealBloodAbsorbReagents = new()
    {
        Contents =
        {
            new ReagentQuantity(new ReagentId("Carol", null), 1),
        }
    };

    [DataField("bloodAbsorbSound")]
    public SoundSpecifier BloodAbsorbSound = new SoundPathSpecifier("/Audio/Effects/Fluids/splat.ogg");

    [ViewVariables(VVAccess.ReadWrite), DataField("bloodWhitelist")]
    public List<string> BloodWhitelist = new()
    {
        "Blood",
        "CopperBlood",
        "InsectBlood",
        "AmmoniaBlood",
        "ZombieBlood"
    };

    [DataField("devourTime")] public float DevourTime = 10f;

    [DataField("devourSound")]
    public SoundSpecifier DevourSound = new SoundPathSpecifier("/Audio/_Sunrise/FleshCult/devour_flesh_cultist.ogg");

    [DataField("stolenCurrencyPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<CurrencyPrototype>))]
    public string StolenCurrencyPrototype = "StolenMutationPoint";

    [ViewVariables(VVAccess.ReadWrite),
     DataField("fleshBladeSpawnId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string BladeSpawnId = "FleshBlade";

    [ViewVariables(VVAccess.ReadWrite),
     DataField("fleshFistSpawnId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string FistSpawnId = "FleshFist";

    [ViewVariables(VVAccess.ReadWrite),
     DataField("clawSpawnId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ClawSpawnId = "FleshClaw";

    [ViewVariables(VVAccess.ReadWrite),
     DataField("spikeHandGunSpawnId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string SpikeHandGunSpawnId = "FleshSpikeHandGun";

    [ViewVariables(VVAccess.ReadWrite),
     DataField("armorSpawnId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ArmorSpawnId = "ClothingOuterArmorFlesh";

    [ViewVariables(VVAccess.ReadWrite),
     DataField("heavyArmorSpawnId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string HeavyArmorSpawnId = "ClothingOuterHeavyArmorFlesh";

    [ViewVariables(VVAccess.ReadWrite),
     DataField("spiderLegsSpawnId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string SpiderLegsSpawnId = "ClothingFleshSpiderLegs";

    [ViewVariables(VVAccess.ReadWrite),
     DataField("fleshMutationMobId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string FleshMutationMobId = "MobFleshPudge";

    [ViewVariables(VVAccess.ReadWrite), DataField("soundMutation")]
    public SoundSpecifier SoundMutation = new SoundPathSpecifier("/Audio/_Sunrise/FleshCult/flesh_cultist_mutation.ogg");

    [DataField("fleshHeartId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>)),
     ViewVariables(VVAccess.ReadWrite)]
    public string FleshHeartId = "FleshHeart";

    [ViewVariables(VVAccess.ReadWrite), DataField("soundThrowWorm")]
    public SoundSpecifier? SoundThrowHugger = new SoundPathSpecifier("/Audio/_Sunrise/FleshCult/throw_worm.ogg");

    [ViewVariables(VVAccess.ReadWrite),
     DataField("huggerMobSpawnId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string HuggerMobSpawnId = "MobFleshHugger";

    public SoundSpecifier BuySuccesSound = new SoundPathSpecifier(
        "/Audio/_Sunrise/FleshCult/flesh_cultist_buy_succes.ogg");

    [ViewVariables] public float Accumulator = 0;

    [ViewVariables] public float AccumulatorStarveNotify = 0;

    [DataField("fleshStatusIcon")]
    public ProtoId<FactionIconPrototype> StatusIcon { get; set; } = "FleshFaction";

    [DataField]
    public ProtoId<AlertPrototype> MutationPointAlert = "MutationPoint";
}

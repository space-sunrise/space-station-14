using System.Collections.Generic;
using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._Sunrise.CarpQueen;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedCarpQueenSystem), typeof(CarpQueenAccessSystem))]
[AutoGenerateComponentState]
public sealed partial class CarpQueenComponent : Component
{
    [DataField("actionSummon", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ActionSummon = "ActionCarpQueenSummon";

    [DataField("actionSummonEntity")]
    public EntityUid? ActionSummonEntity;

    [DataField("actionOrderStay", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ActionOrderStay = "ActionCarpQueenOrderStay";

    [DataField("actionOrderStayEntity")]
    public EntityUid? ActionOrderStayEntity;

    [DataField("actionOrderFollow", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ActionOrderFollow = "ActionCarpQueenOrderFollow";

    [DataField("actionOrderFollowEntity")]
    public EntityUid? ActionOrderFollowEntity;

    [DataField("actionOrderKill", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ActionOrderKill = "ActionCarpQueenOrderKill";

    [DataField("actionOrderKillEntity")]
    public EntityUid? ActionOrderKillEntity;

    [DataField("actionOrderLoose", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ActionOrderLoose = "ActionCarpQueenOrderLoose";

    [DataField("actionOrderLooseEntity")]
    public EntityUid? ActionOrderLooseEntity;

    /// <summary>
    /// Current order applied to servants.
    /// </summary>
    [DataField("currentOrders"), AutoNetworkedField]
    public CarpQueenOrderType CurrentOrder = CarpQueenOrderType.Loose;

    /// <summary>
    /// List of spawned servants controlled by the queen.
    /// </summary>
    [DataField("servants")]
    public HashSet<EntityUid> Servants = new();

    /// <summary>
    /// Active eggs spawned by the queen (not yet hatched).
    /// </summary>
    [DataField("eggs")]
    public HashSet<EntityUid> Eggs = new();

    /// <summary>
    /// Pool of carp servant prototype IDs to randomly pick from when summoning.
    /// </summary>
    [DataField("armyMobSpawnOptions")]
    public List<string> ArmyMobSpawnOptions = new()
    {
        "MobCarpServantDungeon",
        "MobCarpServantMagic",
        "MobCarpServantHolo",
        "MobCarpServantRainbow",
        "MobCarpServantDragon"
    };

    /// <summary>
    /// Dataset mapping for order callouts (spoken lines on order change).
    /// </summary>
    [DataField("orderCallouts")]
    public Dictionary<CarpQueenOrderType, string> OrderCallouts = new()
    {
        { CarpQueenOrderType.Stay, "CarpQueenCommandStay" },
        { CarpQueenOrderType.Follow, "CarpQueenCommandFollow" },
        { CarpQueenOrderType.Kill, "CarpQueenCommandKill" },
        { CarpQueenOrderType.Loose, "CarpQueenCommandLoose" }
    };

    /// <summary>
    /// Hunger consumed per summon use.
    /// </summary>
    [DataField("hungerPerSummon")]
    public float HungerPerSummon = 25f;

    /// <summary>
    /// Tracks last observed hunger to grant small healing when eating.
    /// Server-side only; not networked.
    /// </summary>
    public float LastObservedHunger;

    /// <summary>
    /// Maximum total servants + eggs the queen can have at once.
    /// </summary>
    [DataField("maxArmySize")]
    public int MaxArmySize = 5;

    /// <summary>
    /// HP healed per 1 unit of hunger gained (when eating).
    /// </summary>
    [DataField("healPerHunger")]
    public float HealPerHunger = 0.2f;

    /// <summary>
    /// Maximum HP healed per tick from eating.
    /// </summary>
    [DataField("maxHealPerTick")]
    public float MaxHealPerTick = 5f;

    /// <summary>
    /// Spawn chances for different carp types when hatching from egg.
    /// Key: prototype ID, Value: chance (0-100).
    /// If sum is less than 100, remaining chance goes to default (MobCarpServantRainbow).
    /// </summary>
    [DataField("spawnChances")]
    public Dictionary<string, int> SpawnChances = new()
    {
        { "MobCarpServantRainbow", 80 },
        { "MobCarpServantHolo", 10 },
        { "MobCarpServantDungeon", 10 }
    };
}



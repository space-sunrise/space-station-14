using Content.Shared.Roles;
using Content.Shared.Maps;
using Content.Shared.Parallax.Biomes;
using Content.Shared.Whitelist;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.GameTicking.PlayerJoinableMaps;

/// <summary>
/// Describes player access, spawning rules, and optional managed loading for a station on a separate map.
/// </summary>
/// <remarks>
/// When <see cref="Load"/> is absent, an external system owns the map lifecycle. This is used by
/// technical maps such as Central Command. When it is present, the generic server system loads the map.
/// </remarks>
[Prototype]
public sealed partial class PlayerJoinableMapPrototype : IPrototype
{
    /// <summary>
    /// Unique prototype identifier referenced by <see cref='PlayerJoinableMapComponent.Map'/>.
    /// </summary>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Localized name shown for this external map in character and late-join interfaces.
    /// </summary>
    [DataField(required: true)]
    public LocId DisplayName;

    /// <summary>
    /// Selects the typed CVar group used to configure player access to this map.
    /// </summary>
    [DataField]
    public PlayerJoinableMapAccessType Access = PlayerJoinableMapAccessType.Always;

    /// <summary>
    /// Whether the owning loader may create the map while player access is disabled.
    /// </summary>
    /// <remarks>
    /// This is useful for service maps that must exist for gameplay even when their jobs are unavailable.
    /// </remarks>
    [DataField]
    public bool SpawnWhenPlayerAccessDisabled;

    /// <summary>
    /// Optional server-side configuration for maps owned by the generic DLC loader.
    /// </summary>
    [DataField(serverOnly: true)]
    public PlayerJoinableMapLoadConfiguration? Load;

    /// <summary>
    /// Whether generic fallback spawning must ignore stations configured with this map.
    /// </summary>
    [DataField]
    public bool ExcludeFromFallbackSpawn = true;

    /// <summary>
    /// Whether players assigned to this external station may be selected as antagonists.
    /// </summary>
    [DataField]
    public bool CanBeAntag = true;

    /// <summary>
    /// Whether a station using this map configuration may compose an emergency shuttle component.
    /// </summary>
    [DataField]
    public bool EmergencyShuttleEnabled;

    /// <summary>
    /// Spawn-point category required when a player joins after round start.
    /// </summary>
    [DataField]
    public PlayerJoinableMapSpawnPointType LateJoinSpawnPointType = PlayerJoinableMapSpawnPointType.Job;

    /// <summary>
    /// Spawn-point category required when a player is assigned during round start.
    /// </summary>
    [DataField]
    public PlayerJoinableMapSpawnPointType RoundStartSpawnPointType = PlayerJoinableMapSpawnPointType.Job;

    /// <summary>
    /// Display order relative to other player-joinable maps.
    /// </summary>
    [DataField]
    public int Order;

    /// <summary>
    /// Jobs owned by this map and therefore available only through a matching station.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<JobPrototype>> Jobs = [];
}

/// <summary>
/// Configures a game map whose lifecycle is owned by the generic player-joinable map loader.
/// </summary>
[DataDefinition]
public sealed partial class PlayerJoinableMapLoadConfiguration
{
    /// <summary>
    /// The single game map loaded for this player-joinable map.
    /// </summary>
    /// <remarks>
    /// Game-map prototypes are intentionally ignored by clients, so the serialized value cannot use the engine's
    /// automatic <see cref="ProtoId{T}"/> validator. Consumers still receive a typed ID, and the server loader
    /// validates it against <see cref="GameMapPrototype"/> before loading.
    /// </remarks>
    [DataField(required: true)]
    private string _gameMap = default!;

    public ProtoId<GameMapPrototype> GameMap => _gameMap;

    /// <summary>
    /// Selects the environment-specific loading and validation path.
    /// </summary>
    [DataField(required: true)]
    public PlayerJoinableMapEnvironmentType Environment;

    /// <summary>
    /// Components applied to the loaded map entity before map initialization.
    /// </summary>
    [DataField]
    public ComponentRegistry MapComponents = new();

    /// <summary>
    /// Components applied to every loaded grid before map initialization.
    /// </summary>
    [DataField]
    public ComponentRegistry GridComponents = new();

    /// <summary>
    /// Biomes available to a planet environment. Space environments must leave this empty.
    /// </summary>
    [DataField]
    public List<ProtoId<BiomeTemplatePrototype>> Biomes = [];

    /// <summary>
    /// Optional FTL destination configuration for the loaded map.
    /// </summary>
    [DataField]
    public PlayerJoinableMapFtlConfiguration? Ftl;

    /// <summary>
    /// Whether the loader should announce the selected map, biome, and module activation.
    /// </summary>
    [DataField]
    public bool AnnounceOnLoad;
}

/// <summary>
/// Configures FTL discovery and shuttle access for a managed map.
/// </summary>
[DataDefinition]
public sealed partial class PlayerJoinableMapFtlConfiguration
{
    /// <summary>
    /// Whether the destination is enabled after loading.
    /// </summary>
    [DataField]
    public bool Enabled = true;

    /// <summary>
    /// Whether a coordinate disk is required to select the destination.
    /// </summary>
    [DataField]
    public bool RequireCoordinateDisk;

    /// <summary>
    /// Whether FTL arrival is limited to beacons on the destination map.
    /// </summary>
    [DataField]
    public bool BeaconsOnly;

    /// <summary>
    /// Restricts which shuttles may select the destination.
    /// </summary>
    [DataField]
    public EntityWhitelist? ShuttleWhitelist;
}

/// <summary>
/// Selects the environment-specific loading path for a managed player-joinable map.
/// </summary>
public enum PlayerJoinableMapEnvironmentType
{
    /// <summary>
    /// Loads an ordinary space map without biome generation.
    /// </summary>
    Space,

    /// <summary>
    /// Requires one grid and generates a biome-backed planet around it.
    /// </summary>
    Planet,
}

/// <summary>
/// Selects how a join flow validates spawn points on an external station.
/// </summary>
public enum PlayerJoinableMapSpawnPointType
{
    /// <summary>
    /// Does not require a specialized spawn-point category.
    /// </summary>
    Unset,

    /// <summary>
    /// Requires a spawn point matching the selected job.
    /// </summary>
    Job,
}

/// <summary>
/// Selects the typed CVar group used by a player-joinable map.
/// </summary>
public enum PlayerJoinableMapAccessType
{
    /// <summary>
    /// The map is always available to players.
    /// </summary>
    Always,

    /// <summary>
    /// Uses Central Command availability settings.
    /// </summary>
    CentComm,

    /// <summary>
    /// Uses planet prison availability and minimum-player settings.
    /// </summary>
    PlanetPrison,
}

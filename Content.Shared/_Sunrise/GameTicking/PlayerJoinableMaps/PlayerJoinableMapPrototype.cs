using Content.Shared.Roles;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.GameTicking.PlayerJoinableMaps;

/// <summary>
/// Describes player access and spawning rules for a station located on a separately loaded game map.
/// </summary>
/// <remarks>
/// This prototype does not load a map by itself. A server-side owner system must load a
/// <see cref='Content.Shared.Maps.GameMapPrototype'/> and that game map must create a station prototype
/// containing <see cref='PlayerJoinableMapComponent'/> which references this configuration.
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
    /// Selects the configuration source used to decide whether players may join this map.
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
/// Selects the CVar-backed access policy used by a player-joinable map.
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

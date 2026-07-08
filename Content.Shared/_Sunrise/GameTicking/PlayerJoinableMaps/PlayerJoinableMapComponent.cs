namespace Content.Shared._Sunrise.GameTicking.PlayerJoinableMaps;

[RegisterComponent]
public sealed partial class PlayerJoinableMapComponent : Component
{
    /// <summary>
    /// CVar that controls whether ordinary player access to this map is enabled.
    /// </summary>
    [DataField]
    public string? PlayerAccessEnabledCVar;

    /// <summary>
    /// Whether the map should still spawn when ordinary player access is disabled.
    /// </summary>
    [DataField]
    public bool SpawnWhenPlayerAccessDisabled;

    /// <summary>
    /// Whether this station should be excluded from fallback spawning without an explicit job.
    /// </summary>
    [DataField]
    public bool ExcludeFromFallbackSpawn = true;

    /// <summary>
    /// Spawn point type used for latejoin on this map.
    /// </summary>
    [DataField]
    public PlayerJoinableMapSpawnPointType LateJoinSpawnPointType = PlayerJoinableMapSpawnPointType.Job;

    /// <summary>
    /// Spawn point type used for roundstart on this map.
    /// </summary>
    [DataField]
    public PlayerJoinableMapSpawnPointType RoundStartSpawnPointType = PlayerJoinableMapSpawnPointType.Job;
}

public enum PlayerJoinableMapSpawnPointType
{
    Unset,
    Job,
}

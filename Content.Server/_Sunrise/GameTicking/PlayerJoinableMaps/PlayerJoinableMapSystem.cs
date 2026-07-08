using Content.Server.Spawners.Components;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared._Sunrise.GameTicking.PlayerJoinableMaps;
using Content.Shared.Maps;
using Content.Shared.Roles;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.GameTicking.PlayerJoinableMaps;

public sealed class PlayerJoinableMapSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly StationJobsSystem _stationJobs = default!;
    [Dependency] private readonly StationSystem _station = default!;

    /// <summary>
    /// Raises the event that lets map systems activate joinable stations before lobby jobs are built.
    /// </summary>
    public void PrepareLobbyJobs()
    {
        var ev = new PlayerJoinableMapLobbyJobsPreparingEvent();
        RaiseLocalEvent(ev);
    }

    /// <summary>
    /// Returns whether players may currently use this station according to the map access rules, without
    /// applying SpawnWhenPlayerAccessDisabled.
    /// </summary>
    public bool CanUseStationForPlayerAccess(Entity<PlayerJoinableMapComponent?> station)
    {
        if (!Resolve(station, ref station.Comp, false))
            return true;

        return _prototype.TryIndex(station.Comp.Map, out var map) &&
            IsPlayerAccessEnabled(map);
    }

    /// <summary>
    /// Returns whether this station can be selected by fallback spawn logic.
    /// </summary>
    public bool CanFallbackSpawn(Entity<PlayerJoinableMapComponent?> station)
    {
        if (!Resolve(station, ref station.Comp, false))
            return true;

        return _prototype.TryIndex(station.Comp.Map, out var map) &&
            !map.ExcludeFromFallbackSpawn &&
            IsPlayerAccessEnabled(map);
    }

    /// <summary>
    /// Returns whether all player-joinable stations in the game map may be spawned right now, honoring
    /// SpawnWhenPlayerAccessDisabled.
    /// </summary>
    public bool CanSpawnGameMap(GameMapPrototype gameMap)
    {
        foreach (var stationConfig in gameMap.Stations.Values)
        {
            if (!TryResolvePlayerJoinableMap(stationConfig.StationPrototype, out var map, out var hasPlayerJoinableMap))
            {
                if (hasPlayerJoinableMap)
                    return false;

                continue;
            }

            if (!IsPlayerAccessEnabled(map) && !map.SpawnWhenPlayerAccessDisabled)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Returns whether any joinable station in the game map became available because the player count
    /// gate was met.
    /// </summary>
    public bool IsGameMapPlayerCountEnabled(GameMapPrototype gameMap)
    {
        foreach (var stationConfig in gameMap.Stations.Values)
        {
            if (!TryResolvePlayerJoinableMap(stationConfig.StationPrototype, out var map, out _))
                continue;

            if (PlayerJoinableMapAccess.IsPlayerCountEnabled(map, _cfg, _player.PlayerCount))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Gets the lowest configured player-count requirement among the map's joinable stations.
    /// </summary>
    public bool TryGetGameMapAccessMinPlayers(GameMapPrototype gameMap, out int minPlayers)
    {
        minPlayers = int.MaxValue;
        var foundMinPlayers = false;

        foreach (var stationConfig in gameMap.Stations.Values)
        {
            if (!TryResolvePlayerJoinableMap(stationConfig.StationPrototype, out var map, out _))
                continue;

            if (PlayerJoinableMapAccess.IsExplicitlyEnabled(map, _cfg) ||
                !PlayerJoinableMapAccess.TryGetMinPlayers(map, _cfg, out var mapMinPlayers) ||
                mapMinPlayers < 0)
            {
                continue;
            }

            foundMinPlayers = true;
            minPlayers = Math.Min(minPlayers, mapMinPlayers);
        }

        return foundMinPlayers;
    }

    /// <summary>
    /// Returns whether the given job can be joined on the station for the requested join flow.
    /// </summary>
    public bool CanJoinAs(
        Entity<StationJobsComponent?> station,
        ProtoId<JobPrototype> job,
        PlayerJoinKind joinKind)
    {
        if (!Resolve(station, ref station.Comp, false))
            return false;

        if (!CanUseStationForPlayerAccess((station.Owner, null)))
            return false;

        if (!_stationJobs.TryGetJobSlot(station, job, out var slots, station.Comp) || slots == 0)
            return false;

        if (!TryComp<PlayerJoinableMapComponent>(station, out var playerJoinableMap))
            return true;

        if (!_prototype.TryIndex(playerJoinableMap.Map, out var map))
            return false;

        if (!map.Jobs.Contains(job))
            return false;

        return HasMatchingSpawnPoint(station, job, joinKind, map);
    }

    /// <summary>
    /// Finds a station where the job can be joined, while also reporting a station where the job exists
    /// but is blocked.
    /// </summary>
    public bool TryResolveJoinableStationForJob(
        ProtoId<JobPrototype> job,
        PlayerJoinKind joinKind,
        out EntityUid station,
        out EntityUid unavailableStation)
    {
        station = EntityUid.Invalid;
        unavailableStation = EntityUid.Invalid;

        var query = EntityQueryEnumerator<StationJobsComponent>();
        while (query.MoveNext(out var stationUid, out var stationJobs))
        {
            if (!_stationJobs.TryGetJobSlot(stationUid, job, out _, stationJobs))
                continue;

            if (CanJoinAs((stationUid, stationJobs), job, joinKind))
            {
                station = stationUid;
                return true;
            }

            unavailableStation = stationUid;
        }

        return false;
    }

    /// <summary>
    /// Removes jobs from the collection that cannot be joined on the station for the requested join flow.
    /// </summary>
    public void FilterAvailableJobs(
        Entity<StationJobsComponent?> station,
        Dictionary<ProtoId<JobPrototype>, int?> jobs,
        PlayerJoinKind joinKind)
    {
        if (!Resolve(station, ref station.Comp, false))
        {
            jobs.Clear();
            return;
        }

        var jobKeys = new List<ProtoId<JobPrototype>>(jobs.Keys);
        foreach (var job in jobKeys)
        {
            if (!CanJoinAs(station, job, joinKind))
                jobs.Remove(job);
        }
    }

    /// <summary>
    /// Returns whether the job collection contains any job reserved for a player-joinable map.
    /// </summary>
    public bool HasAnyPlayerJoinableJob(IReadOnlyDictionary<ProtoId<JobPrototype>, int?> jobs)
    {
        foreach (var map in _prototype.EnumeratePrototypes<PlayerJoinableMapPrototype>())
        {
            foreach (var job in map.Jobs)
            {
                if (jobs.ContainsKey(job))
                    return true;
            }
        }

        return false;
    }

    private bool HasMatchingSpawnPoint(
        EntityUid station,
        ProtoId<JobPrototype> job,
        PlayerJoinKind joinKind,
        PlayerJoinableMapPrototype playerJoinableMap)
    {
        var spawnPointType = joinKind switch
        {
            PlayerJoinKind.RoundStart => GetSpawnPointType(playerJoinableMap.RoundStartSpawnPointType),
            PlayerJoinKind.LateJoin => GetSpawnPointType(playerJoinableMap.LateJoinSpawnPointType),
            _ => SpawnPointType.Unset,
        };

        if (spawnPointType == SpawnPointType.Unset)
            return true;

        var query = EntityQueryEnumerator<SpawnPointComponent, TransformComponent>();
        while (query.MoveNext(out var spawnUid, out var spawnPoint, out var xform))
        {
            if (_station.GetOwningStation(spawnUid, xform) != station)
                continue;

            if (spawnPoint.SpawnType != spawnPointType)
                continue;

            if (spawnPointType == SpawnPointType.Job && spawnPoint.Job != null && spawnPoint.Job != job)
                continue;

            return true;
        }

        return false;
    }

    private bool IsPlayerAccessEnabled(PlayerJoinableMapPrototype map)
    {
        return PlayerJoinableMapAccess.IsEnabled(map, _cfg, _player.PlayerCount);
    }

    private bool TryResolvePlayerJoinableMap(
        EntProtoId stationPrototype,
        out PlayerJoinableMapPrototype map,
        out bool hasPlayerJoinableMap)
    {
        map = default!;
        hasPlayerJoinableMap = false;

        if (!_prototype.TryIndex<EntityPrototype>(stationPrototype, out var station) ||
            !station.TryGetComponent<PlayerJoinableMapComponent>(out var component, Factory))
        {
            return false;
        }

        hasPlayerJoinableMap = true;
        if (!_prototype.TryIndex(component.Map, out PlayerJoinableMapPrototype? resolvedMap))
            return false;

        map = resolvedMap;
        return true;
    }

    private static SpawnPointType GetSpawnPointType(PlayerJoinableMapSpawnPointType spawnPointType)
    {
        return spawnPointType switch
        {
            PlayerJoinableMapSpawnPointType.Job => SpawnPointType.Job,
            _ => SpawnPointType.Unset,
        };
    }
}

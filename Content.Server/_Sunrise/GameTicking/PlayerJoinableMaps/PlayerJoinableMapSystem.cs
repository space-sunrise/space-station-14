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
    /// Returns whether players spawned on this station may be selected as antagonists.
    /// </summary>
    public bool CanBeAntag(Entity<PlayerJoinableMapComponent?> station)
    {
        if (!Resolve(station, ref station.Comp, false))
            return true;

        return _prototype.TryIndex(station.Comp.Map, out var map) &&
            map.CanBeAntag;
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

            if (!PlayerJoinableMapAccess.IsEnabledByCVar(map, _cfg) ||
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

        Entity<PlayerJoinableMapComponent?> playerJoinableMap = (station.Owner, null);
        var hasPlayerJoinableMap = Resolve(playerJoinableMap, ref playerJoinableMap.Comp, false);
        var playerJoinableMapComponent = playerJoinableMap.Comp;

        if (hasPlayerJoinableMap &&
            playerJoinableMapComponent != null &&
            !CanUseStationForPlayerAccess(playerJoinableMap))
            return false;

        if (!_stationJobs.TryGetJobSlot(station, job, out var slots, station.Comp) || slots == 0)
            return false;

        if (!hasPlayerJoinableMap || playerJoinableMapComponent == null)
            return true;

        if (!_prototype.TryIndex(playerJoinableMapComponent.Map, out var map))
            return false;

        if (!map.Jobs.Contains(job))
            return false;

        var spawnPointMatchCache = GetSpawnPointMatchCache(station, GetSpawnPointType(joinKind, map));
        return HasMatchingSpawnPoint(job, spawnPointMatchCache);
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

        PlayerJoinableMapPrototype? map = null;
        var spawnPointMatchCache = new SpawnPointMatchCache(SpawnPointType.Unset, true, false, null);
        Entity<PlayerJoinableMapComponent?> playerJoinableMap = (station.Owner, null);
        var hasPlayerJoinableMap = Resolve(playerJoinableMap, ref playerJoinableMap.Comp, false);
        var playerJoinableMapComponent = playerJoinableMap.Comp;

        if (hasPlayerJoinableMap && playerJoinableMapComponent != null)
        {
            if (!CanUseStationForPlayerAccess(playerJoinableMap) ||
                !_prototype.TryIndex(playerJoinableMapComponent.Map, out map))
            {
                jobs.Clear();
                return;
            }

            spawnPointMatchCache = GetSpawnPointMatchCache(station, GetSpawnPointType(joinKind, map));
        }

        var jobKeys = new List<ProtoId<JobPrototype>>(jobs.Keys);
        foreach (var job in jobKeys)
        {
            if (!CanJoinAs(station, job, map, spawnPointMatchCache))
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

    private bool CanJoinAs(
        Entity<StationJobsComponent?> station,
        ProtoId<JobPrototype> job,
        PlayerJoinableMapPrototype? playerJoinableMap,
        SpawnPointMatchCache spawnPointMatchCache)
    {
        if (!_stationJobs.TryGetJobSlot(station, job, out var slots, station.Comp) || slots == 0)
            return false;

        if (playerJoinableMap == null)
            return true;

        if (!playerJoinableMap.Jobs.Contains(job))
            return false;

        return HasMatchingSpawnPoint(job, spawnPointMatchCache);
    }

    private SpawnPointMatchCache GetSpawnPointMatchCache(EntityUid station, SpawnPointType spawnPointType)
    {
        if (spawnPointType == SpawnPointType.Unset)
            return new SpawnPointMatchCache(spawnPointType, true, false, null);

        HashSet<ProtoId<JobPrototype>>? jobs = null;
        var query = EntityQueryEnumerator<SpawnPointComponent, TransformComponent>();
        while (query.MoveNext(out var spawnUid, out var spawnPoint, out var xform))
        {
            if (_station.GetOwningStation(spawnUid, xform) != station)
                continue;

            if (spawnPoint.SpawnType != spawnPointType)
                continue;

            if (spawnPointType != SpawnPointType.Job)
                return new SpawnPointMatchCache(spawnPointType, true, false, null);

            if (spawnPoint.Job is not { } spawnPointJob)
                return new SpawnPointMatchCache(spawnPointType, true, true, null);

            jobs ??= [];
            jobs.Add(spawnPointJob);
        }

        return new SpawnPointMatchCache(spawnPointType, jobs != null, false, jobs);
    }

    private static bool HasMatchingSpawnPoint(ProtoId<JobPrototype> job, SpawnPointMatchCache spawnPointMatchCache)
    {
        if (spawnPointMatchCache.SpawnPointType == SpawnPointType.Unset)
            return true;

        if (!spawnPointMatchCache.HasMatchingSpawnPoint)
            return false;

        return spawnPointMatchCache.SpawnPointType != SpawnPointType.Job ||
            spawnPointMatchCache.HasUnrestrictedJobSpawnPoint ||
            spawnPointMatchCache.Jobs?.Contains(job) == true;
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

    private static SpawnPointType GetSpawnPointType(PlayerJoinKind joinKind, PlayerJoinableMapPrototype playerJoinableMap)
    {
        return joinKind switch
        {
            PlayerJoinKind.RoundStart => GetSpawnPointType(playerJoinableMap.RoundStartSpawnPointType),
            PlayerJoinKind.LateJoin => GetSpawnPointType(playerJoinableMap.LateJoinSpawnPointType),
            _ => SpawnPointType.Unset,
        };
    }

    private static SpawnPointType GetSpawnPointType(PlayerJoinableMapSpawnPointType spawnPointType)
    {
        return spawnPointType switch
        {
            PlayerJoinableMapSpawnPointType.Job => SpawnPointType.Job,
            _ => SpawnPointType.Unset,
        };
    }

    private readonly record struct SpawnPointMatchCache(
        SpawnPointType SpawnPointType,
        bool HasMatchingSpawnPoint,
        bool HasUnrestrictedJobSpawnPoint,
        HashSet<ProtoId<JobPrototype>>? Jobs);
}

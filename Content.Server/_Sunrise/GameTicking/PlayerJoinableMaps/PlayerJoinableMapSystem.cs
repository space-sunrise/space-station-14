using Content.Server.Spawners.Components;
using Content.Server.Spawners.EntitySystems;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Server.GameTicking;
using Content.Server._Sunrise.GameTicking.Events;
using Content.Server.Shuttles.Components;
using Content.Shared._Sunrise.GameTicking.PlayerJoinableMaps;
using Content.Shared.GameTicking;
using Content.Shared.Maps;
using Content.Shared.Roles;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.GameTicking.PlayerJoinableMaps;

/// <summary>
/// Coordinates access, job ownership, and spawn-point selection for stations on separately loaded maps.
/// </summary>
/// <remarks>
/// Technical maps such as Central Command may keep a feature-specific owner. Maps with managed loading
/// are created by this system. Both variants use the same round-start, late-join, fallback-spawn, and
/// antagonist-selection flows.
/// </remarks>
public sealed partial class PlayerJoinableMapSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly StationJobsSystem _stationJobs = default!;
    [Dependency] private readonly SpawnPointSystem _spawnPoint = default!;

    private readonly PlayerJoinableMapIndex _mapIndex = new();
    private readonly HashSet<ProtoId<PlayerJoinableMapPrototype>> _playerCountAccessibleMaps = [];
    private readonly HashSet<string> _subscribedAccessCVars = [];
    private int? _lastProcessedPlayerCount;

    public override void Initialize()
    {
        base.Initialize();
        _mapIndex.Rebuild(_prototype);
        RefreshAccessCVarSubscriptions();
        InitializeManagedLoading();
        SubscribeLocalEvent<PlayerJoinableMapComponent, ComponentInit>(OnPlayerJoinableMapInit);
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        SubscribeLocalEvent<RoundLobbyReadyEvent>(OnRoundLobbyReady);
        SubscribeLocalEvent<PlayerJoinedLobbyEvent>(OnPlayerJoinedLobby, before: [typeof(StationJobsSystem)]);
        _player.PlayerStatusChanged += OnPlayerStatusChanged;

        if (_gameTicker.RunLevel == GameRunLevel.PreRoundLobby)
            ResetPlayerCountAccessForLobby();
    }

    public override void Shutdown()
    {
        _player.PlayerStatusChanged -= OnPlayerStatusChanged;
        _playerCountAccessibleMaps.Clear();
        _lastProcessedPlayerCount = null;
        base.Shutdown();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        _mapIndex.Rebuild(_prototype);
        RefreshAccessCVarSubscriptions();

        if (_gameTicker.RunLevel == GameRunLevel.PreRoundLobby)
            UpdatePlayerCountAccess(true);

        LoadAvailableManagedMaps();
        _stationJobs.UpdateJobsAvailable();
    }

    private void OnPlayerJoinableMapInit(Entity<PlayerJoinableMapComponent> ent, ref ComponentInit args)
    {
        if (!_prototype.TryIndex(ent.Comp.Map, out var map) || map.EmergencyShuttleEnabled)
            return;

        RemComp<StationEmergencyShuttleComponent>(ent);
    }

    private void OnPlayerJoinedLobby(PlayerJoinedLobbyEvent args)
    {
        if (UpdatePlayerCountAccess())
            _stationJobs.UpdateJobsAvailable();

        LoadAvailableManagedMaps();
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        if (!UpdatePlayerCountAccess())
            return;

        LoadAvailableManagedMaps();
        _stationJobs.UpdateJobsAvailable();
    }

    private void OnRoundLobbyReady(ref RoundLobbyReadyEvent args)
    {
        ResetPlayerCountAccessForLobby();
        LoadAvailableManagedMaps();
        _stationJobs.UpdateJobsAvailable();
    }

    private void RefreshAccessCVarSubscriptions()
    {
        foreach (var map in _mapIndex.Maps)
        {
            SubscribeAccessCVar(PlayerJoinableMapAccess.GetEnabledCVar(map));
            SubscribeAccessCVar(PlayerJoinableMapAccess.GetMinPlayersCVar(map));
        }
    }

    private void SubscribeAccessCVar<T>(CVarDef<T>? cvar)
        where T : notnull
    {
        if (cvar == null || !_subscribedAccessCVars.Add(cvar.Name))
            return;

        Subs.CVar(_cfg, cvar, _ => OnAccessCVarChanged());
    }

    private void OnAccessCVarChanged()
    {
        if (_gameTicker.RunLevel == GameRunLevel.PreRoundLobby)
            UpdatePlayerCountAccess(true);

        LoadAvailableManagedMaps();
        _stationJobs.UpdateJobsAvailable();
    }

    private void ResetPlayerCountAccessForLobby()
    {
        _playerCountAccessibleMaps.Clear();
        _lastProcessedPlayerCount = null;
        UpdatePlayerCountAccess();
    }

    private bool UpdatePlayerCountAccess(bool force = false)
    {
        if (_gameTicker.RunLevel != GameRunLevel.PreRoundLobby)
            return false;

        var playerCount = _player.PlayerCount;
        if (!force && _lastProcessedPlayerCount == playerCount)
            return false;

        _lastProcessedPlayerCount = playerCount;
        var changed = false;
        foreach (var map in _mapIndex.Maps)
        {
            if (!PlayerJoinableMapAccess.TryGetMinPlayers(map, _cfg, out var minPlayers) ||
                Math.Max(0, minPlayers) == 0 ||
                playerCount < Math.Max(0, minPlayers))
            {
                continue;
            }

            changed |= _playerCountAccessibleMaps.Add(map.ID);
        }

        return changed;
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

        var spawnPointAvailability = _spawnPoint.GetSpawnPointAvailability(station, GetSpawnPointType(joinKind, map));
        return spawnPointAvailability.Matches(job);
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
        var slotUnavailableStation = EntityUid.Invalid;

        var query = EntityQueryEnumerator<StationJobsComponent>();
        while (query.MoveNext(out var stationUid, out var stationJobs))
        {
            if (!_stationJobs.TryGetJobSlot(stationUid, job, out var slots, stationJobs))
                continue;

            if (slots == 0 && slotUnavailableStation == EntityUid.Invalid)
                slotUnavailableStation = stationUid;

            if (CanJoinAs((stationUid, stationJobs), job, joinKind))
            {
                station = stationUid;
                return true;
            }

            if (unavailableStation == EntityUid.Invalid)
                unavailableStation = stationUid;
        }

        if (slotUnavailableStation != EntityUid.Invalid)
            unavailableStation = slotUnavailableStation;

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
        var spawnPointAvailability = SpawnPointAvailability.Unrestricted;
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

            spawnPointAvailability = _spawnPoint.GetSpawnPointAvailability(station, GetSpawnPointType(joinKind, map));
        }

        var jobKeys = new List<ProtoId<JobPrototype>>(jobs.Keys);
        foreach (var job in jobKeys)
        {
            if (!CanJoinAs(station, job, map, spawnPointAvailability))
                jobs.Remove(job);
        }
    }

    /// <summary>
    /// Returns whether the job collection contains any job reserved for a player-joinable map.
    /// </summary>
    public bool HasAnyPlayerJoinableJob(IReadOnlyDictionary<ProtoId<JobPrototype>, int?> jobs)
    {
        foreach (var job in _mapIndex.Jobs)
        {
            if (jobs.ContainsKey(job))
                return true;
        }

        return false;
    }

    private bool CanJoinAs(
        Entity<StationJobsComponent?> station,
        ProtoId<JobPrototype> job,
        PlayerJoinableMapPrototype? playerJoinableMap,
        SpawnPointAvailability spawnPointAvailability)
    {
        if (!_stationJobs.TryGetJobSlot(station, job, out var slots, station.Comp) || slots == 0)
            return false;

        if (playerJoinableMap == null)
            return true;

        if (!playerJoinableMap.Jobs.Contains(job))
            return false;

        return spawnPointAvailability.Matches(job);
    }

    private bool IsPlayerAccessEnabled(PlayerJoinableMapPrototype map)
    {
        if (!PlayerJoinableMapAccess.IsEnabledByCVar(map, _cfg))
            return false;

        if (!PlayerJoinableMapAccess.TryGetMinPlayers(map, _cfg, out var minPlayers) ||
            Math.Max(0, minPlayers) == 0)
        {
            return true;
        }

        return _playerCountAccessibleMaps.Contains(map.ID);
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

    /// <summary>
    /// Resolves the spawn-point category for a station while preserving the caller's vanilla fallback.
    /// </summary>
    public SpawnPointType GetSpawnPointType(
        Entity<PlayerJoinableMapComponent?> station,
        PlayerJoinKind joinKind,
        SpawnPointType fallback)
    {
        if (!Resolve(station, ref station.Comp, false))
            return fallback;

        if (!_prototype.TryIndex(station.Comp.Map, out var map))
            return SpawnPointType.Unset;

        return GetSpawnPointType(joinKind, map);
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

}

using Content.Server.Spawners.Components;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared._Sunrise.GameTicking.PlayerJoinableMaps;
using Content.Shared.Roles;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.GameTicking.PlayerJoinableMaps;

public sealed class PlayerJoinableMapSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly StationJobsSystem _stationJobs = default!;
    [Dependency] private readonly StationSystem _station = default!;

    public bool CanUseStationForPlayerAccess(Entity<PlayerJoinableMapComponent?> station)
    {
        if (!Resolve(station, ref station.Comp, false))
            return true;

        return IsPlayerAccessEnabled(station.Comp);
    }

    public bool CanFallbackSpawn(Entity<PlayerJoinableMapComponent?> station)
    {
        if (!Resolve(station, ref station.Comp, false))
            return true;

        return !station.Comp.ExcludeFromFallbackSpawn && IsPlayerAccessEnabled(station.Comp);
    }

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

        return HasMatchingSpawnPoint(station, job, joinKind, playerJoinableMap);
    }

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
        PlayerJoinableMapComponent playerJoinableMap)
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

    private bool IsPlayerAccessEnabled(PlayerJoinableMapComponent component)
    {
        return component.PlayerAccessEnabledCVar == null ||
               !_cfg.IsCVarRegistered(component.PlayerAccessEnabledCVar) ||
               _cfg.GetCVar<bool>(component.PlayerAccessEnabledCVar);
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

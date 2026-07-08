using Content.Server._Sunrise.Other.StationOnlyDirectSpawn;
using Content.Server.Spawners.Components;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Roles;

namespace Content.Server._Sunrise.StationCentComm;

internal enum CentCommJoinStationResult
{
    NotCentCommJob,
    Resolved,
    Disabled,
    NoStation,
    NoSlots,
    NoSpawnPoint,
}

public sealed partial class StationCentCommSystem
{
    [Dependency] private readonly StationJobsSystem _stationJobs = default!;
    [Dependency] private readonly StationSystem _station = default!;

    internal CentCommJoinStationResult ResolveCentCommJoinStation(string jobId, out EntityUid station)
    {
        station = EntityUid.Invalid;

        if (!IsCentCommJob(jobId))
            return CentCommJoinStationResult.NotCentCommJob;

        if (!IsCentCommEnabled())
            return CentCommJoinStationResult.Disabled;

        var foundStation = false;
        var foundSlot = false;
        var query = EntityQueryEnumerator<StationOnlyDirectSpawnComponent, StationJobsComponent, StationSpawningComponent>();
        while (query.MoveNext(out var stationUid, out _, out var stationJobs, out _))
        {
            foundStation = true;

            if (!_stationJobs.TryGetJobSlot(stationUid, jobId, out var slots, stationJobs) || slots == 0)
            {
                station = stationUid;
                continue;
            }

            foundSlot = true;
            station = stationUid;

            if (HasMatchingJobSpawnPoint(stationUid, jobId))
                return CentCommJoinStationResult.Resolved;
        }

        if (!foundStation)
            return CentCommJoinStationResult.NoStation;

        return foundSlot
            ? CentCommJoinStationResult.NoSpawnPoint
            : CentCommJoinStationResult.NoSlots;
    }

    private bool HasMatchingJobSpawnPoint(EntityUid station, string jobId)
    {
        var query = EntityQueryEnumerator<SpawnPointComponent, TransformComponent>();
        while (query.MoveNext(out var spawnUid, out var spawnPoint, out var xform))
        {
            if (_station.GetOwningStation(spawnUid, xform) != station)
                continue;

            if (spawnPoint.SpawnType != SpawnPointType.Job)
                continue;

            if (spawnPoint.Job != null && spawnPoint.Job != jobId)
                continue;

            return true;
        }

        return false;
    }
}

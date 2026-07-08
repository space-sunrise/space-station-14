using Content.Server._Sunrise.GameTicking.PlayerJoinableMaps;
using Content.Shared.Roles;
using Robust.Shared.Player;

#pragma warning disable IDE0130 // Namespace не соответствует папке из-за partial-портала.
namespace Content.Server.GameTicking;

public sealed partial class GameTicker
{
    [Dependency] private readonly PlayerJoinableMapSystem _playerJoinableMap = default!;

    internal bool TryPreparePlayerJoinableMapJoin(ICommonSession player, string? jobId, ref EntityUid station)
    {
        if (jobId == null)
            return true;

        if (!_prototypeManager.TryIndex<JobPrototype>(jobId, out var job))
            return true;

        if (station != EntityUid.Invalid)
        {
            if (_playerJoinableMap.CanJoinAs((station, null), job.ID, PlayerJoinKind.LateJoin))
                return true;

            NotifyJoinGameJobUnavailable(player, station, job);
            return false;
        }

        if (_playerJoinableMap.TryResolveJoinableStationForJob(job.ID, PlayerJoinKind.LateJoin, out var resolvedStation, out var unavailableStation))
        {
            station = resolvedStation;
            return true;
        }

        if (unavailableStation != EntityUid.Invalid)
            NotifyJoinGameJobUnavailable(player, unavailableStation, job);
        else
            NotifyNoJobsAvailable(player);

        return false;
    }

    partial void ResolveDirectSpawnStationPortal(ICommonSession player, string? jobId, ref EntityUid station, ref bool handled)
    {
        handled = !TryPreparePlayerJoinableMapJoin(player, jobId, ref station);
    }
}

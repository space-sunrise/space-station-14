using Content.Server.GameTicking.Events;
using Content.Server.Station.Components;
using Content.Shared.Roles;
using Robust.Shared.Player;

#pragma warning disable IDE0130 // Namespace не соответствует папке из-за partial-портала
namespace Content.Server.GameTicking;

public sealed partial class GameTicker
{
    internal bool TryHandleJoinGameUnavailable(ICommonSession player, EntityUid station, string jobId)
    {
        if (TryBlockDisabledCentCommJoin(player, jobId))
            return true;

        if (!_prototypeManager.TryIndex<JobPrototype>(jobId, out var job))
            return false;

        if (!job.AlwaysUseSpawner)
            return false;

        if (CanJoinGameAsJob(station, jobId))
            return false;

        NotifyJoinGameJobUnavailable(player, station, job);
        return true;
    }

    partial void BeforeSpawnPlayerJob(
        ICommonSession player,
        EntityUid station,
        JobPrototype job,
        bool lateJoin,
        ref bool jobSlotPreassigned,
        ref bool handled)
    {
        if (TryBlockDisabledCentCommJoin(player, job.ID))
        {
            handled = true;
            return;
        }

        if (!lateJoin || !job.AlwaysUseSpawner)
            return;

        if (_stationJobs.TryAssignJob(station, job, player.UserId))
        {
            jobSlotPreassigned = true;
            return;
        }

        NotifyJoinGameJobUnavailable(player, station, job);
        handled = true;
    }

    private bool CanJoinGameAsJob(EntityUid station, string jobId)
    {
        return TryGetJoinGameJobSlot(station, jobId, out var slots) && slots != 0;
    }

    private bool TryGetJoinGameJobSlot(EntityUid station, string jobId, out int? slots)
    {
        slots = null;

        if (station == EntityUid.Invalid)
            return false;

        if (!TryComp<StationJobsComponent>(station, out var stationJobs))
            return false;

        return _stationJobs.TryGetJobSlot(station, jobId, out slots, stationJobs);
    }

    private void NotifyJoinGameJobUnavailable(ICommonSession player, EntityUid station, JobPrototype jobPrototype)
    {
        if (TryGetJoinGameJobSlot(station, jobPrototype.ID, out var slots) && slots == 0)
        {
            NotifyJoinGameUnavailable(player,
                Loc.GetString("game-ticker-player-job-slots-unavailable",
                    ("job", jobPrototype.LocalizedName)));
            return;
        }

        NotifyNoJobsAvailable(player);
    }

    private void NotifyNoJobsAvailable(ICommonSession player)
    {
        NotifyJoinGameUnavailable(player,
            Loc.GetString("game-ticker-player-no-jobs-available-when-joining"));
    }

    private void NotifyJoinGameUnavailable(ICommonSession player, string message)
    {
        if (!LobbyEnabled)
            JoinAsObserver(player);

        var evNoJobs = new NoJobsAvailableSpawningEvent(player); // Используется правилами, чтобы очистить antag slot.
        RaiseLocalEvent(evNoJobs);

        _chatManager.DispatchServerMessage(player, message);
    }
}

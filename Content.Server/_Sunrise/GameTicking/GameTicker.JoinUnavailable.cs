using Content.Server.GameTicking.Events;
using Content.Shared.Roles;
using Robust.Shared.Player;

#pragma warning disable IDE0130 // Namespace не соответствует папке из-за partial-портала
namespace Content.Server.GameTicking;

public sealed partial class GameTicker
{
    private bool TryGetJoinGameJobSlot(EntityUid station, string jobId, out int? slots)
    {
        slots = null;

        if (station == EntityUid.Invalid)
            return false;

        return _stationJobs.TryGetJobSlot(station, jobId, out slots);
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

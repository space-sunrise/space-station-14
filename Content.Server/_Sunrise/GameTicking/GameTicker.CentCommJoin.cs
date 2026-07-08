using Content.Server._Sunrise.StationCentComm;
using Content.Shared.Roles;
using Robust.Shared.Player;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Server.GameTicking;

public sealed partial class GameTicker
{
    [Dependency] private readonly StationCentCommSystem _sunriseCentComm = default!;

    internal bool TryHandleJoinGameStationResolution(ICommonSession player, string? jobId, ref EntityUid station)
    {
        if (jobId == null)
            return false;

        var result = _sunriseCentComm.ResolveCentCommJoinStation(jobId, out var resolvedStation);
        switch (result)
        {
            case CentCommJoinStationResult.NotCentCommJob:
                return false;
            case CentCommJoinStationResult.Resolved:
                station = resolvedStation;
                return false;
            case CentCommJoinStationResult.Disabled:
                NotifyCentCommDisabled(player);
                return true;
            case CentCommJoinStationResult.NoSlots:
                NotifyJoinGameJobUnavailable(player, resolvedStation, _prototypeManager.Index<JobPrototype>(jobId));
                return true;
            case CentCommJoinStationResult.NoStation:
            case CentCommJoinStationResult.NoSpawnPoint:
                NotifyNoJobsAvailable(player);
                return true;
        }

        return false;
    }

    partial void ResolveDirectSpawnStationPortal(ICommonSession player, string? jobId, ref EntityUid station, ref bool handled)
    {
        handled = TryHandleJoinGameStationResolution(player, jobId, ref station);
    }

    private void NotifyCentCommDisabled(ICommonSession player)
    {
        NotifyJoinGameUnavailable(player,
            Loc.GetString("game-ticker-player-centcomm-disabled"));
    }
}

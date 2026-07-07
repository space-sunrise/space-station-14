using Content.Server._Sunrise.StationCentComm;
using Robust.Shared.Player;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Server.GameTicking;

public sealed partial class GameTicker
{
    [Dependency] private readonly StationCentCommSystem _sunriseCentComm = default!;

    private bool TryBlockDisabledCentCommJoin(ICommonSession player, string jobId)
    {
        if (!_sunriseCentComm.ShouldBlockCentCommJoin(jobId))
            return false;

        NotifyCentCommDisabled(player);
        return true;
    }

    private void NotifyCentCommDisabled(ICommonSession player)
    {
        NotifyJoinGameUnavailable(player,
            Loc.GetString("game-ticker-player-centcomm-disabled"));
    }
}

using Robust.Shared.Player;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Server.GameTicking.Commands;

sealed partial class JoinGameCommand
{
    partial void BeforeJoinGameCommand(
        ICommonSession player,
        EntityUid station,
        string jobId,
        GameTicker ticker,
        ref bool handled)
    {
        handled = ticker.TryHandleJoinGameUnavailable(player, station, jobId);
    }
}

using Content.Server.Administration;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Server.Station.Systems;
using Content.Shared.Administration;
using Content.Shared.Clothing;
using Robust.Server.Console;
using Robust.Server.Player;
using Robust.Server.Toolshed.Commands.Players;
using Robust.Shared.Console;
using Robust.Shared.Player;

namespace Content.Server._Sunrise.CentCom;

public sealed partial class CentComConsoleSystem
{
    [Dependency] private readonly IConsoleHost _consoleHost = default!;
    [Dependency] private readonly StationSpawningSystem _spawning = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly IServerConsoleHost _serverConsole = default!;
    [Dependency] private readonly IPlayerLocator _locator = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private void InitializeCommands()
    {
        _consoleHost.RegisterCommand("spawnas",
            "",
            "spawnas <player> <outfit ID>",
            SpawnAsCommand);
    }

    [AdminCommand(AdminFlags.Debug)]
    private async void SpawnAsCommand(IConsoleShell shell, string argstr, string[] args)
    {
        if (args.Length != 2)
            return;

        if (!_player.TryGetSessionByUsername(args[0], out var session))
            return;

        if (session.AttachedEntity == null)
            return;

        var uid = session.AttachedEntity.Value;

        if (!TryComp<ActorComponent>(uid, out var targetActor))
            return;

        if (!_transform.TryGetMapOrGridCoordinates(uid, out var coords))
        {
            return;
        }

        var stationUid = _station.GetOwningStation(uid);

        var profile = _ticker.GetPlayerProfile(targetActor.PlayerSession);
        var mobUid = _spawning.SpawnPlayerMob(coords.Value, null, profile, stationUid);
        var targetMind = _mindSystem.GetMind(uid);

        if (targetMind != null)
        {
            _mindSystem.TransferTo(targetMind.Value, mobUid, true);
        }

        _serverConsole.ExecuteCommand($"setoutfit {mobUid} {args[1]}");
    }
}

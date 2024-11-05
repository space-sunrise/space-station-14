using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Server.Station.Systems;
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

    private void InitializeCommands()
    {
        _consoleHost.RegisterCommand("spawnascentcomoperator", SpawnAsCentcomOperatorCallback);
    }

    private void SpawnAsCentcomOperatorCallback(IConsoleShell shell, string argstr, string[] args)
    {
        if (args.Length != 1)
            return;

        if (!NetEntity.TryParse(args[0], out var uidNet) || !TryGetEntity(uidNet, out var uid))
        {
            return;
        }

        if (!TryComp<ActorComponent>(uid, out var targetActor))
            return;

        if (!_transform.TryGetMapOrGridCoordinates(uid.Value, out var coords))
        {
            return;
        }

        var stationUid = _station.GetOwningStation(uid);

        var profile = _ticker.GetPlayerProfile(targetActor.PlayerSession);
        var mobUid = _spawning.SpawnPlayerMob(coords.Value, null, profile, stationUid);
        var targetMind = _mindSystem.GetMind(uid.Value);

        if (targetMind != null)
        {
            _mindSystem.TransferTo(targetMind.Value, mobUid, true);
        }
        // ты тут закончил
    }
}

using Content.Server.Administration;
using Content.Server._Sunrise.Shuttles.Systems;
using Content.Server.Station.Systems;
using Content.Shared.Administration;
using JetBrains.Annotations;
using Robust.Shared.Console;

namespace Content.Server._Sunrise.Shuttles.Commands;

[UsedImplicitly]
[AdminCommand(AdminFlags.Fun)]
public sealed class SpawnArrivalsShuttleCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entities = default!;

    public string Command => "spawnarrivalsshuttle";
    public string Description => "sends the executing player to a arrivals shuttle";
    public string Help => "spawnarrivalsshuttle";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player?.AttachedEntity is not { } player)
        {
            shell.WriteError("no attached entity");
            return;
        }

        var stationSystem = _entities.System<StationSystem>();
        if (stationSystem.GetOwningStation(player) is not { } station)
        {
            shell.WriteError("be on a station");
            return;
        }

        if (!_entities.System<SunriseArrivalsSystem>().TrySpawnForPlayer(station, player))
        {
            shell.WriteError("unable to spawn sunrise arrivals shuttle");
            return;
        }

        shell.WriteLine("ok");
    }
}

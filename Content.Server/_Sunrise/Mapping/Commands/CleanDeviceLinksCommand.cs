using Content.Server.Administration;
using Content.Server.DeviceLinking.Systems;
using Content.Shared.Administration;
using Robust.Server.GameObjects;
using Robust.Shared.Console;
using Robust.Shared.Map;

namespace Content.Server._Sunrise.Mapping.Commands;

[AdminCommand(AdminFlags.Mapping)]
public sealed class CleanDeviceLinksCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _ent = default!;

    public string Command => "cleandevicelinks";

    public string Description => "Removes invalid saved device link references from a map.";

    public string Help => $"Usage: {Command} [mapId]";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        MapId mapId;
        switch (args.Length)
        {
            case 0:
                if (shell.Player?.AttachedEntity is not { Valid: true } playerEntity)
                {
                    shell.WriteError("Only an attached player can run this command without a map id.");
                    return;
                }

                mapId = _ent.GetComponent<TransformComponent>(playerEntity).MapID;
                break;
            case 1:
                if (!int.TryParse(args[0], out var intMapId))
                {
                    shell.WriteError($"{args[0]} is not a valid map id.");
                    return;
                }

                mapId = new MapId(intMapId);
                break;
            default:
                shell.WriteLine(Help);
                return;
        }

        if (mapId == MapId.Nullspace || !_ent.System<MapSystem>().MapExists(mapId))
        {
            shell.WriteError($"Map {mapId} does not exist.");
            return;
        }

        var result = _ent.System<DeviceLinkSystem>().CleanupLinksForMapSave(mapId);
        shell.WriteLine(
            $"Cleaned device links on map {mapId}: removed {result.RemovedSinkEntries} sink references and {result.RemovedLinkPairs} invalid link pairs across {result.AffectedSources} source entities.");
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length == 1
            ? CompletionResult.FromHintOptions(CompletionHelper.MapIds(_ent), "mapId")
            : CompletionResult.Empty;
    }
}

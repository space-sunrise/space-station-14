using Content.Server.Administration;
using Content.Server.DeviceLinking.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Map;

namespace Content.Server._Sunrise.Mapping.Commands;

/// <summary>
/// Removes invalid saved device-link references from a map before it is exported or fixed manually.
/// </summary>
[AdminCommand(AdminFlags.Mapping)]
public sealed class CleanDeviceLinksCommand : LocalizedEntityCommands
{
    [Dependency] private readonly DeviceLinkSystem _deviceLink = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;

    public override string Command => "cleandevicelinks";

    /// <summary>
    /// Cleans invalid saved device-link references for the requested map.
    /// </summary>
    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        MapId mapId;
        switch (args.Length)
        {
            case 0:
                var player = shell.Player;
                if (player == null)
                {
                    shell.WriteError(Loc.GetString("shell-only-players-can-run-this-command"));
                    return;
                }

                if (player.AttachedEntity is not { Valid: true } playerEntity)
                {
                    shell.WriteError(Loc.GetString("shell-must-be-attached-to-entity"));
                    return;
                }

                mapId = EntityManager.GetComponent<TransformComponent>(playerEntity).MapID;
                break;
            case 1:
                if (!int.TryParse(args[0], out var intMapId))
                {
                    shell.WriteError(Loc.GetString("cmd-parse-failure-mapid", ("arg", args[0])));
                    return;
                }

                mapId = new MapId(intMapId);
                break;
            default:
                shell.WriteLine(Help);
                return;
        }

        if (mapId == MapId.Nullspace || !_map.MapExists(mapId))
        {
            shell.WriteError(Loc.GetString("cmd-cleandevicelinks-map-missing", ("mapId", mapId)));
            return;
        }

        var result = _deviceLink.CleanupLinksForMapSave(mapId);
        shell.WriteLine(Loc.GetString(
            "cmd-cleandevicelinks-cleaned",
            ("mapId", mapId),
            ("removedSinkEntries", result.RemovedSinkEntries),
            ("removedLinkPairs", result.RemovedLinkPairs),
            ("affectedSources", result.AffectedSources)));
    }

    /// <summary>
    /// Provides map-id completion for the command.
    /// </summary>
    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length == 1
            ? CompletionResult.FromHintOptions(
                CompletionHelper.MapIds(EntityManager),
                Loc.GetString("cmd-cleandevicelinks-hint-map"))
            : CompletionResult.Empty;
    }
}

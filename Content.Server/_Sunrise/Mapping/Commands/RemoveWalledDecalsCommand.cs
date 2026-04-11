#pragma warning disable IDE0130

using Content.Server.Administration;
using Content.Server.Decals;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Map.Components;

namespace Content.Server._Sunrise.Mapping.Commands;

/// <summary>
/// Removes decals that overlap wall tiles on the selected grid.
/// </summary>
[AdminCommand(AdminFlags.Mapping)]
public sealed class RemoveWalledDecalsCommand : LocalizedEntityCommands
{
    /// <summary>
    /// Gets the console verb used to remove walled decals.
    /// </summary>
    public override string Command => "removewalleddecals";

    /// <summary>
    /// Removes walled decals from the player grid or from the grid specified in the command arguments.
    /// </summary>
    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        EntityUid? gridId;

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

                gridId = EntityManager.GetComponent<TransformComponent>(playerEntity).GridUid;
                break;
            case 1:
                if (!NetEntity.TryParse(args[0], out var idNet))
                {
                    shell.WriteError(Loc.GetString("cmd-parse-failure-uid", ("arg", args[0])));
                    return;
                }

                if (!EntityManager.TryGetEntity(idNet, out var id))
                {
                    shell.WriteError(Loc.GetString("cmd-parse-failure-entity-exist", ("arg", args[0])));
                    return;
                }

                gridId = id;
                break;
            default:
                shell.WriteLine(Help);
                return;
        }

        if (!EntityManager.EntityExists(gridId))
        {
            shell.WriteError(Loc.GetString("cmd-removewalleddecals-missing-grid-entity", ("grid", "null")));
            return;
        }

        if (!EntityManager.TryGetComponent(gridId, out MapGridComponent? grid))
        {
            shell.WriteError(Loc.GetString("cmd-removewalleddecals-no-grid"));
            return;
        }

        var removed = EntityManager.System<WalledDecalRemovalSystem>().RemoveWalledDecals(gridId.Value, grid);
        shell.WriteLine(Loc.GetString("cmd-removewalleddecals-removed", ("count", removed)));
    }
}

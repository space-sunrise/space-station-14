using Content.Server._Sunrise.Decals;
using Content.Server.Administration;
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
        if (!TryParseGridArg(shell, args, out var gridId))
            return;

        if (!CanRemoveWalledDecals(shell, gridId, out var grid))
            return;

        DoRemoveWalledDecals(shell, grid);
    }

    private bool TryParseGridArg(IConsoleShell shell, string[] args, out EntityUid? gridId)
    {
        gridId = null;

        switch (args.Length)
        {
            case 0:
                var player = shell.Player;
                if (player == null)
                {
                    shell.WriteError(Loc.GetString("shell-only-players-can-run-this-command"));
                    return false;
                }

                if (player.AttachedEntity is not { Valid: true } playerEntity)
                {
                    shell.WriteError(Loc.GetString("shell-must-be-attached-to-entity"));
                    return false;
                }

                gridId = EntityManager.GetComponent<TransformComponent>(playerEntity).GridUid;
                return true;
            case 1:
                if (!NetEntity.TryParse(args[0], out var idNet))
                {
                    shell.WriteError(Loc.GetString("cmd-parse-failure-uid", ("arg", args[0])));
                    return false;
                }

                if (!EntityManager.TryGetEntity(idNet, out var id))
                {
                    shell.WriteError(Loc.GetString("cmd-parse-failure-entity-exist", ("arg", args[0])));
                    return false;
                }

                gridId = id;
                return true;
            default:
                shell.WriteLine(Help);
                return false;
        }
    }

    private bool CanRemoveWalledDecals(
        IConsoleShell shell,
        EntityUid? gridId,
        out Entity<MapGridComponent> grid)
    {
        grid = default;

        if (!EntityManager.EntityExists(gridId))
        {
            shell.WriteError(Loc.GetString("cmd-removewalleddecals-missing-grid-entity"));
            return false;
        }

        if (!EntityManager.TryGetComponent(gridId, out MapGridComponent? gridComponent))
        {
            shell.WriteError(Loc.GetString("cmd-removewalleddecals-no-grid", ("grid", gridId?.ToString() ?? "null")));
            return false;
        }

        grid = (gridId!.Value, gridComponent);
        return true;
    }

    private void DoRemoveWalledDecals(IConsoleShell shell, Entity<MapGridComponent> grid)
    {
        var removed = EntityManager.System<WalledDecalRemovalSystem>().RemoveWalledDecals(grid);
        shell.WriteLine(Loc.GetString("cmd-removewalleddecals-removed", ("count", removed)));
    }
}

using Content.Server.Administration;
using Content.Shared._Sunrise.Misc.Events;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._Sunrise.Chat.Commands;

/// <summary>
/// Console command that toggles the mapping transparency overlay for mapper admins.
/// </summary>
[AdminCommand(AdminFlags.Admin)]
public sealed class ShowMappingTransparencyCommand : LocalizedEntityCommands
{
    /// <summary>
    /// Gets the console verb used to toggle the overlay.
    /// </summary>
    public override string Command => "showmappingtransparency";

    /// <summary>
    /// Toggles the mapping transparency overlay and reports the resulting state to the caller.
    /// </summary>
    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player == null)
        {
            shell.WriteError(LocalizationManager.GetString($"cmd-{Command}-denied"));
            return;
        }

        var ev = new ToggleMappingTransparencyEvent();
        EntityManager.EntityNetManager.SendSystemNetworkMessage(ev, shell.Player.Channel);

        shell.WriteLine(LocalizationManager.GetString($"cmd-{Command}-status"));
    }
}

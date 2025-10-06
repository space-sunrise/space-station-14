using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Shared.Database;
using Robust.Shared.Console;
using Robust.Shared.Toolshed;

namespace Content.Server.Administration.AdminCommandLogger;

public sealed class AdminCommandLoggerSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly IConsoleHost _consoleHost = default!;
    [Dependency] private readonly ToolshedManager _toolshed = default!;

    public override void Initialize()
    {
        _consoleHost.AnyCommandExecuted += OnCommandExecuted;
    }

    private void OnCommandExecuted(IConsoleShell shell, string name, string argStr, string[] args)
    {
        if (shell.Player is not { } player)
            return;

        var adminOnly = false;

        if (_toolshed.DefaultEnvironment.TryGetCommand(name, out var tsCmd))
        {
            var sub = tsCmd.HasSubCommands && args.Length > 0 ? args[0] : null;
            var commandSpec = new CommandSpec(tsCmd, sub);

            adminOnly = _adminManager.TryGetCommandFlags(commandSpec, out var flags) && flags != null;
        }
        else if (_consoleHost.AvailableCommands.TryGetValue(name, out var consoleCommand))
            adminOnly = Attribute.IsDefined(consoleCommand.GetType(), typeof(AdminCommandAttribute));

        if (!adminOnly)
            return;

        _adminLog.Add(
            LogType.AdminCommands,
            LogImpact.High,
            $"Administrator {player:player} executed command <{name}> with args: [{string.Join(", ", args)}]");
    }
}

using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._Scp.DeviceLinking.Commands;

/// <summary>
///     Переключает отображение связку подключенных устройств.
/// </summary>
[AdminCommand(AdminFlags.Admin)]
public sealed class ShowDeviceLinkCommand : LocalizedEntityCommands
{
    [Dependency] private readonly DeviceLinkingVisualizationSystem _deviceLinking = default!;

    public override string Command => "showdevicelink";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var session = shell.Player;
        if (session == null)
            return;

        _deviceLinking.ToggleDebugView(session);
    }
}

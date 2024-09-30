using Content.Shared._Sunrise.Proton;
using Robust.Shared.Console;
using Robust.Shared.Network;

namespace Content.Client._Sunrise.Proton;

public sealed class ScreenshotCommand : LocalizedCommands
{
    [Dependency] private readonly IClientNetManager _netManager = default!;

    public override string Command => "screenshot";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 1)
        {
            shell.WriteLine($"You need to provide a player to screenshot. Try running 'screenshot (playername)' as an example");
            return;
        }

        var name = args[0];
        var message = new ProtonRequestScreenshotServer()
        {
            Target = name,
        };
        _netManager.ClientSendMessage(message);
    }
}

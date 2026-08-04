using Content.Server.Chat.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Enums;

namespace Content.Server._Sunrise.Chat.Commands;

[AnyCommand]
internal sealed class CollectiveMindCommand : IConsoleCommand
{
    [Dependency] private readonly IEntitySystemManager _systems = default!;

    public string Command => "cmsay";
    public string Description => "Send chat messages to the collective mind.";
    public string Help => "cmsay <text>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } player)
        {
            shell.WriteError("This command cannot be run from the server.");
            return;
        }

        if (player.Status != SessionStatus.InGame)
            return;

        if (player.AttachedEntity is not { } playerEntity)
        {
            shell.WriteError("You don't have an entity!");
            return;
        }

        var message = string.Join(" ", args).Trim();
        _systems.GetEntitySystem<ChatSystem>().TrySendCollectiveMindMessage(
            playerEntity,
            message,
            shell: shell,
            player: player);
    }
}

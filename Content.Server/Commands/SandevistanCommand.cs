using Content.Server.Sunrise.Sandevistan;
using Robust.Shared.Console;

namespace Content.Server.Commands
{
    [AnyCommand]  // Важно добавить!
    public sealed class SandevistanCommand : IConsoleCommand
    {
        public string Command => "sandevistan";
        public string Description => "Активирует Сандевистан";
        public string Help => "sandevistan";

        private readonly SandevistanSystem _system;

        public SandevistanCommand(SandevistanSystem system)
        {
            _system = system;
        }

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (shell.Player?.AttachedEntity is not { } player)
            {
                shell.WriteError("Вы не контролируете существо!");
                return;
            }

            _system.ActivateSandevistan(player);
        }
    }
}

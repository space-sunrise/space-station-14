using System.Linq;
using Content.Server.Administration;
using Content.Server.GameTicking.Presets;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Commands
{
    [AdminCommand(AdminFlags.Round)]
    sealed class ForcePresetCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _e = default!;

        public string Command => "forcepreset";
        public string Description => "Forces a specific game preset to start for the current lobby.";
        public string Help => $"Usage: {Command} <preset>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var ticker = _e.System<GameTicker>();
            if (ticker.RunLevel != GameRunLevel.PreRoundLobby)
            {
                shell.WriteLine("This can only be executed while the game is in the pre-round lobby.");
                return;
            }

            if (args.Length != 1)
            {
                shell.WriteLine("Need exactly one argument.");
                return;
            }

            var name = args[0];
            if (!ticker.TryFindGamePreset(name, out var type))
            {
                shell.WriteLine($"No preset exists with name {name}.");
                return;
            }

            // Sunrise-Start
            if (type.Hide)
            {
                shell.WriteError(Loc.GetString("set-game-preset-preset-error", ("preset", args[0])));
                return;
            }
            // Sunrise-End

            ticker.SetGamePreset(type, true);
            shell.WriteLine($"Forced the game to start with preset {name}.");
            ticker.UpdateInfoText();
        }

        public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            if (args.Length == 1)
            {
                var gamePresets = IoCManager.Resolve<IPrototypeManager>().EnumeratePrototypes<GamePresetPrototype>()
                    .OrderBy(p => p.ID);

                // Sunrise-Start
                var options = new List<string>();
                foreach (var preset in gamePresets)
                {
                    if (preset.Hide)
                        continue;

                    options.Add(preset.ID);
                }
                // Sunrise-End

                return CompletionResult.FromHintOptions(options, "<preset>");
            }

            return CompletionResult.Empty;
        }
    }
}

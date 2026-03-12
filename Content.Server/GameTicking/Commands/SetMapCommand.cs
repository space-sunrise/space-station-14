using System.Linq;
using Content.Server.Administration;
using Content.Server.Maps;
using Content.Shared.Administration;
using Content.Shared.Maps;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Commands
{
    [AdminCommand(AdminFlags.Round)]
    sealed class SetMapCommand : IConsoleCommand
    {
        public string Command => "setmap";
        public string Description => Loc.GetString("set-map-command-description");
        public string Help => Loc.GetString("set-map-command-help");

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1)
            {
                shell.WriteLine(Loc.GetString("set-map-command-need-one-argument"));
                return;
            }

            var gameMap = IoCManager.Resolve<IGameMapManager>();
            var name = args[0];

            gameMap.SelectMap(name);
            shell.WriteLine(Loc.GetString("set-map-command-success", ("map", name)));
        }

        public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            if (args.Length == 1)
            {
                var options = IoCManager.Resolve<IPrototypeManager>()
                    .EnumeratePrototypes<GameMapPrototype>()
                    .Select(p => new CompletionOption(p.ID, p.MapName))
                    .OrderBy(p => p.Value);

                return CompletionResult.FromHintOptions(options, Loc.GetString("set-map-command-arg-map"));
            }

            return CompletionResult.Empty;
        }
    }
}

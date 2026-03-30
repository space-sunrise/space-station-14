using System.Linq;
using System.Threading.Tasks;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._Sunrise.MapperSync.Commands;

[AdminCommand(AdminFlags.Server)]
public sealed class PullMapCommand : IConsoleCommand
{
    public string Command => "pullmap";
    public string Description => "Downloads a map from the configured remote mapper server.";
    public string Help => "Usage: pullmap <map_path> (e.g., pullmap /Maps/my_map.yml)\nYou can also just type the filename if it's unique.";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteLine(Help);
            return;
        }

        var mapPath = args[0];
        shell.WriteLine($"Starting download of {mapPath} from remote server... Please wait.");

        var sys = IoCManager.Resolve<MapperSyncManager>();

        // Start async task so we don't block the main thread.
        Task.Run(async () =>
        {
            try
            {
                var (success, error) = await sys.DownloadMapAsync(mapPath);

                if (success)
                    shell.WriteLine($"Successfully downloaded map: {mapPath}");
                else
                    shell.WriteError($"Failed to download map {mapPath}. Error: {error}");
            }
            catch (Exception ex)
            {
                shell.WriteError($"Exception during map pull: {ex.Message}");
            }
        });
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var sys = IoCManager.Resolve<MapperSyncManager>();
            var maps = sys.GetRemoteMaps();
            if (maps.Count == 0)
            {
                return CompletionResult.FromHint("Requesting remote cache... Type the path manually or try again in a moment.");
            }

            var opts = maps.Where(m => m.Contains(args[0], StringComparison.OrdinalIgnoreCase))
                           .Select(m => new CompletionOption(m));
            return CompletionResult.FromHintOptions(opts, "Path to map (e.g., /Maps/test_map.yml)");
        }

        return CompletionResult.Empty;
    }
}

using System.Threading.Tasks;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Configuration;
using Content.Shared._Sunrise.SunriseCCVars;

namespace Content.Server._Sunrise.MapperSync.Commands;

[AdminCommand(AdminFlags.Server)]
public sealed class MapperSyncStatusCommand : IConsoleCommand
{
    public string Command => "mappersync_status";
    public string Description => "Shows the status of the Mapper Sync system.";
    public string Help => "Usage: mappersync_status";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var sys = IoCManager.Resolve<MapperSyncManager>();
        var cfg = IoCManager.Resolve<IConfigurationManager>();

        shell.WriteLine($"Mapper Sync Status:");
        shell.WriteLine($"- Server URL: {cfg.GetCVar(SunriseCCVars.MapperSyncServerUrl)}");
        shell.WriteLine($"- Expose Maps: {cfg.GetCVar(SunriseCCVars.MapperSyncExposeMaps)}");
        shell.WriteLine($"- Is Fetching: {sys.IsFetching}");
        shell.WriteLine($"- Last Fetch: {(sys.LastFetchTime == DateTime.MinValue ? "Never" : sys.LastFetchTime.ToLocalTime().ToString())}");
        shell.WriteLine($"- Cached Maps: {sys.CachedRemoteMaps.Count}");
    }
}

[AdminCommand(AdminFlags.Server)]
public sealed class MapperSyncFetchCommand : IConsoleCommand
{
    public string Command => "mappersync_fetch";
    public string Description => "Manually triggers a fetch of the remote map list.";
    public string Help => "Usage: mappersync_fetch";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var sys = IoCManager.Resolve<MapperSyncManager>();

        shell.WriteLine("Starting manual fetch...");

        Task.Run(async () =>
        {
            var success = await sys.FetchMapsAsync();
            if (success)
                shell.WriteLine($"Fetch successful! Found {sys.CachedRemoteMaps.Count} maps.");
            else
                shell.WriteError("Fetch failed. Check server logs for details.");
        });
    }
}

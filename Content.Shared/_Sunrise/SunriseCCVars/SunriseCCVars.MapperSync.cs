using Robust.Shared.Configuration;

namespace Content.Shared._Sunrise.SunriseCCVars;

public sealed partial class SunriseCCVars
{
    /// <summary>
    /// Should this server expose its maps directory via the internal HTTP API?
    /// This should ONLY be enabled on the Mapper Server.
    /// </summary>
    public static readonly CVarDef<bool> MapperSyncExposeMaps =
        CVarDef.Create("mapper_sync.expose_maps", false, CVar.SERVERONLY);

    /// <summary>
    /// The URL of the Mapper Server (e.g. `http://mappers.station.com:1212`).
    /// </summary>
    public static readonly CVarDef<string> MapperSyncServerUrl =
        CVarDef.Create("mapper_sync.server_url", "", CVar.SERVERONLY);

    /// <summary>
    /// The secret token to authorize map download/list requests between main server and mapper server.
    /// </summary>
    public static readonly CVarDef<string> MapperSyncApiToken =
        CVarDef.Create("mapper_sync.api_token", "", CVar.SERVERONLY | CVar.CONFIDENTIAL);
}

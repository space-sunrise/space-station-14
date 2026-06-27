using Robust.Shared.Configuration;

namespace Content.Shared._Sunrise.SunriseCCVars;

public sealed partial class SunriseCCVars
{
    /// <summary>
    /// Должен ли этот сервер отдавать директорию карт через внутренний HTTP API?
    /// Включать только на Mapper Server.
    /// </summary>
    public static readonly CVarDef<bool> MapperSyncExposeMaps =
        CVarDef.Create("mapper_sync.expose_maps", false, CVar.SERVERONLY);

    /// <summary>
    /// URL Mapper Server, например `http://mappers.station.com:1212`.
    /// </summary>
    public static readonly CVarDef<string> MapperSyncServerUrl =
        CVarDef.Create("mapper_sync.server_url", "", CVar.SERVERONLY);

    /// <summary>
    /// Секретный токен для авторизации запросов скачивания и списка карт между основным сервером и mapper server.
    /// </summary>
    public static readonly CVarDef<string> MapperSyncApiToken =
        CVarDef.Create("mapper_sync.api_token", "", CVar.SERVERONLY | CVar.CONFIDENTIAL);
}

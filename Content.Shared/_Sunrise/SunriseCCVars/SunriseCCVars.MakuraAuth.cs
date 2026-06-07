using Robust.Shared.Configuration;

namespace Content.Shared._Sunrise.SunriseCCVars;

public sealed partial class SunriseCCVars
{
    public static readonly CVarDef<bool> MakuraAuthEnabled =
        CVarDef.Create("makura_auth.enabled", false, CVar.SERVERONLY);

    public static readonly CVarDef<string> MakuraAuthApiUrl =
        CVarDef.Create("makura_auth.api_url", "", CVar.SERVERONLY);

    public static readonly CVarDef<string> MakuraAuthApiToken =
        CVarDef.Create("makura_auth.api_token", "", CVar.SERVERONLY);

    public static readonly CVarDef<string> MakuraAuthInternalApiUrl =
        CVarDef.Create("makura_auth.internal_api_url", "", CVar.SERVERONLY | CVar.CONFIDENTIAL);

    public static readonly CVarDef<string> MakuraAuthInternalApiKey =
        CVarDef.Create("makura_auth.internal_api_key", "", CVar.SERVERONLY | CVar.CONFIDENTIAL);

    public static readonly CVarDef<int> MakuraAuthRegistrationTtlSeconds =
        CVarDef.Create("makura_auth.registration_ttl", 900, CVar.SERVERONLY);

    public static readonly CVarDef<string> MakuraAuthProjectName =
        CVarDef.Create("makura_auth.project_name", "stellar-stories", CVar.SERVERONLY);
}

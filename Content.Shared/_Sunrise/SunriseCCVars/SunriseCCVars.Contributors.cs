using Robust.Shared.Configuration;

namespace Content.Shared._Sunrise.SunriseCCVars;

[CVarDefs]
public sealed partial class SunriseCCVars
{
    public static readonly CVarDef<bool> ContributorsEnable =
        CVarDef.Create("contributors.enable", false, CVar.SERVERONLY);

    public static readonly CVarDef<string> ContributorsApiUrl =
        CVarDef.Create("contributors.api_url", string.Empty, CVar.SERVERONLY);

    public static readonly CVarDef<string> ContributorsProjectName =
        CVarDef.Create("contributors.project_name", string.Empty, CVar.SERVERONLY);

    public static readonly CVarDef<string> ContributorsApiToken =
        CVarDef.Create("contributors.api_token", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);
}

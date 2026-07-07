using Robust.Shared.Configuration;

namespace Content.Shared._Sunrise.SunriseCCVars;

public sealed partial class SunriseCCVars
{
    public static readonly CVarDef<bool> CentCommEnabled =
        CVarDef.Create("centcomm.enabled", true, CVar.SERVERONLY | CVar.ARCHIVE);
}

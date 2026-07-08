using Robust.Shared.Configuration;

namespace Content.Shared._Sunrise.SunriseCCVars;

public sealed partial class SunriseCCVars
{
    public static readonly CVarDef<bool> CentCommEnabled =
        CVarDef.Create("centcomm.enabled", true, CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE);

    public static readonly CVarDef<bool> PlanetPrisonEnabled =
        CVarDef.Create("planet_prison.enable", true, CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE);

    public static readonly CVarDef<int> MinPlayersPlanetPrison =
        CVarDef.Create("planet_prison.min_players", -1, CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE);
}

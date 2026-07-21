using Robust.Shared.Configuration;

namespace Content.Shared._Sunrise.SunriseCCVars;

public sealed partial class SunriseCCVars
{
    public static readonly CVarDef<bool> PlayerJoinableMapCentCommEnabled =
        CVarDef.Create(
            "player_joinable_maps.centcomm.enabled",
            true,
            CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE);

    public static readonly CVarDef<int> PlayerJoinableMapCentCommMinPlayers =
        CVarDef.Create(
            "player_joinable_maps.centcomm.min_players",
            0,
            CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE);

    public static readonly CVarDef<bool> PlayerJoinableMapPlanetPrisonEnabled =
        CVarDef.Create(
            "planet_prison.enable",
            true,
            CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE);

    public static readonly CVarDef<int> PlayerJoinableMapPlanetPrisonMinPlayers =
        CVarDef.Create(
            "planet_prison.min_players",
            0,
            CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE);
}

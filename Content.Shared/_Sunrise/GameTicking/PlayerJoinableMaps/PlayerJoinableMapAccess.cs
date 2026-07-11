using Robust.Shared.Configuration;
using SunriseCVars = Content.Shared._Sunrise.SunriseCCVars.SunriseCCVars;

namespace Content.Shared._Sunrise.GameTicking.PlayerJoinableMaps;

public static class PlayerJoinableMapAccess
{
    public static bool IsEnabled(
        PlayerJoinableMapPrototype map,
        IConfigurationManager cfg,
        int playerCount)
    {
        if (!IsEnabledByCVar(map, cfg))
            return false;

        if (!TryGetMinPlayers(map, cfg, out var minPlayers))
            return true;

        return playerCount >= minPlayers;
    }

    public static bool IsEnabledByCVar(PlayerJoinableMapPrototype map, IConfigurationManager cfg)
    {
        var cvar = GetEnabledCVar(map);
        return cvar == null || cfg.GetCVar(cvar);
    }

    public static bool IsPlayerCountEnabled(
        PlayerJoinableMapPrototype map,
        IConfigurationManager cfg,
        int playerCount)
    {
        if (!IsEnabledByCVar(map, cfg))
            return false;

        if (!TryGetMinPlayers(map, cfg, out var minPlayers) || minPlayers < 0)
            return false;

        return playerCount >= minPlayers;
    }

    public static bool TryGetMinPlayers(
        PlayerJoinableMapPrototype map,
        IConfigurationManager cfg,
        out int minPlayers)
    {
        minPlayers = default;
        var cvar = GetMinPlayersCVar(map);
        if (cvar == null)
            return false;

        minPlayers = cfg.GetCVar(cvar);
        return true;
    }

    public static CVarDef<bool>? GetEnabledCVar(PlayerJoinableMapPrototype map)
    {
        return map.Access switch
        {
            PlayerJoinableMapAccessType.CentComm => SunriseCVars.CentCommEnabled,
            PlayerJoinableMapAccessType.PlanetPrison => SunriseCVars.PlanetPrisonEnabled,
            _ => null,
        };
    }

    public static CVarDef<int>? GetMinPlayersCVar(PlayerJoinableMapPrototype map)
    {
        return map.Access switch
        {
            PlayerJoinableMapAccessType.PlanetPrison => SunriseCVars.MinPlayersPlanetPrison,
            _ => null,
        };
    }

    public static bool IsAutoGated(
        PlayerJoinableMapPrototype map,
        IConfigurationManager cfg,
        int playerCount)
    {
        if (!IsEnabledByCVar(map, cfg))
            return false;

        if (!TryGetMinPlayers(map, cfg, out var minPlayers) || minPlayers < 0)
            return false;

        return playerCount < minPlayers;
    }
}

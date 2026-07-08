using Robust.Shared.Configuration;

namespace Content.Shared._Sunrise.GameTicking.PlayerJoinableMaps;

public static class PlayerJoinableMapAccess
{
    public static bool IsEnabled(
        PlayerJoinableMapPrototype map,
        IConfigurationManager cfg,
        int playerCount)
    {
        if (IsExplicitlyEnabled(map, cfg))
            return true;

        if (!TryGetMinPlayers(map, cfg, out var minPlayers))
            return map.PlayerAccessEnabledCVar == null;

        if (minPlayers < 0)
            return false;

        return playerCount >= minPlayers;
    }

    public static bool IsExplicitlyEnabled(PlayerJoinableMapPrototype map, IConfigurationManager cfg)
    {
        return map.PlayerAccessEnabledCVar != null &&
            cfg.IsCVarRegistered(map.PlayerAccessEnabledCVar) &&
            cfg.GetCVar<bool>(map.PlayerAccessEnabledCVar);
    }

    public static bool IsPlayerCountEnabled(
        PlayerJoinableMapPrototype map,
        IConfigurationManager cfg,
        int playerCount)
    {
        if (IsExplicitlyEnabled(map, cfg))
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
        if (map.PlayerAccessMinPlayersCVar == null ||
            !cfg.IsCVarRegistered(map.PlayerAccessMinPlayersCVar))
        {
            return false;
        }

        minPlayers = cfg.GetCVar<int>(map.PlayerAccessMinPlayersCVar);
        return true;
    }

    public static bool IsAutoGated(
        PlayerJoinableMapPrototype map,
        IConfigurationManager cfg,
        int playerCount)
    {
        if (!TryGetMinPlayers(map, cfg, out _))
            return false;

        if (IsExplicitlyEnabled(map, cfg))
            return false;

        return !IsEnabled(map, cfg, playerCount);
    }
}

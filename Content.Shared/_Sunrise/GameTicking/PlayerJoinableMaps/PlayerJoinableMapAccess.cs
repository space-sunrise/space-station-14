using Robust.Shared.Configuration;
using SunriseCVars = Content.Shared._Sunrise.SunriseCCVars.SunriseCCVars;

namespace Content.Shared._Sunrise.GameTicking.PlayerJoinableMaps;

public static class PlayerJoinableMapAccess
{
    /// <summary>
    /// Evaluates the complete access state for a player-joinable map.
    /// </summary>
    public static PlayerJoinableMapAccessResult GetAccess(
        PlayerJoinableMapPrototype map,
        IConfigurationManager cfg,
        int playerCount)
    {
        var enabled = IsEnabledByCVar(map, cfg);
        var minPlayers = TryGetMinPlayers(map, cfg, out var configuredMinPlayers)
            ? Math.Max(0, configuredMinPlayers)
            : 0;

        return Evaluate(enabled, minPlayers, playerCount);
    }

    /// <summary>
    /// Evaluates map access from already resolved configuration values.
    /// </summary>
    public static PlayerJoinableMapAccessResult Evaluate(bool enabled, int minPlayers, int playerCount)
    {
        minPlayers = Math.Max(0, minPlayers);
        var hasPlayerThreshold = minPlayers > 0;
        var playerThresholdReached = !hasPlayerThreshold || playerCount >= minPlayers;

        var reason = PlayerJoinableMapUnavailableReason.None;
        if (!enabled)
            reason = PlayerJoinableMapUnavailableReason.Disabled;
        else if (!playerThresholdReached)
            reason = PlayerJoinableMapUnavailableReason.PlayerThreshold;

        return new PlayerJoinableMapAccessResult(
            enabled,
            hasPlayerThreshold,
            playerThresholdReached,
            enabled && playerThresholdReached,
            minPlayers,
            reason);
    }

    /// <summary>
    /// Returns whether a map is currently available.
    /// </summary>
    public static bool IsEnabled(
        PlayerJoinableMapPrototype map,
        IConfigurationManager cfg,
        int playerCount)
    {
        return GetAccess(map, cfg, playerCount).IsAvailable;
    }

    /// <summary>
    /// Returns whether the map's base bool CVar permits access.
    /// </summary>
    public static bool IsEnabledByCVar(PlayerJoinableMapPrototype map, IConfigurationManager cfg)
    {
        var cvar = GetEnabledCVar(map);
        return cvar == null || cfg.GetCVar(cvar);
    }

    /// <summary>
    /// Returns whether a configured positive player threshold currently enables the map.
    /// </summary>
    public static bool IsPlayerCountEnabled(
        PlayerJoinableMapPrototype map,
        IConfigurationManager cfg,
        int playerCount)
    {
        var access = GetAccess(map, cfg, playerCount);
        return access.Enabled && access.HasPlayerThreshold && access.PlayerThresholdReached;
    }

    /// <summary>
    /// Gets the raw configured minimum player count when the map access type defines one.
    /// </summary>
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

    /// <summary>
    /// Gets the bool CVar associated with the map access type.
    /// </summary>
    public static CVarDef<bool>? GetEnabledCVar(PlayerJoinableMapPrototype map)
    {
        return map.Access switch
        {
            PlayerJoinableMapAccessType.CentComm => SunriseCVars.CentCommEnabled,
            PlayerJoinableMapAccessType.PlanetPrison => SunriseCVars.PlanetPrisonEnabled,
            _ => null,
        };
    }

    /// <summary>
    /// Gets the minimum-player CVar associated with the map access type.
    /// </summary>
    public static CVarDef<int>? GetMinPlayersCVar(PlayerJoinableMapPrototype map)
    {
        return map.Access switch
        {
            PlayerJoinableMapAccessType.PlanetPrison => SunriseCVars.MinPlayersPlanetPrison,
            _ => null,
        };
    }

    /// <summary>
    /// Returns whether a map is temporarily blocked by a configured positive player threshold.
    /// </summary>
    public static bool IsAutoGated(
        PlayerJoinableMapPrototype map,
        IConfigurationManager cfg,
        int playerCount)
    {
        var access = GetAccess(map, cfg, playerCount);
        return access.Enabled && access.HasPlayerThreshold && !access.PlayerThresholdReached;
    }
}

/// <summary>
/// Complete access decision shared by the client and server.
/// </summary>
public readonly record struct PlayerJoinableMapAccessResult(
    bool Enabled,
    bool HasPlayerThreshold,
    bool PlayerThresholdReached,
    bool IsAvailable,
    int MinPlayers,
    PlayerJoinableMapUnavailableReason UnavailableReason);

/// <summary>
/// Describes why a player-joinable map is currently unavailable.
/// </summary>
public enum PlayerJoinableMapUnavailableReason
{
    None,
    Disabled,
    PlayerThreshold,
}

namespace Content.Server._Sunrise.GameTicking.PlayerJoinableMaps;

/// <summary>
/// Identifies the spawning flow for which an external station and its spawn points are validated.
/// </summary>
public enum PlayerJoinKind : byte
{
    /// <summary>
    /// Initial assignment while the round is starting.
    /// </summary>
    RoundStart,

    /// <summary>
    /// Direct job selection after the round has started.
    /// </summary>
    LateJoin,

    /// <summary>
    /// Generic fallback spawning when no explicit job station was selected.
    /// </summary>
    Fallback,
}

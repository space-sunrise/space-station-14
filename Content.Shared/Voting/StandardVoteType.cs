namespace Content.Shared.Voting;

/// <summary>
/// Standard vote types that players can initiate themselves from the escape menu.
/// </summary>
public enum StandardVoteType : byte
{
    /// <summary>
    /// Vote to restart the round.
    /// </summary>
    Restart,

    /// <summary>
    /// Vote to change the game preset for next round.
    /// </summary>
    Preset,

    /// <summary>
    /// Vote to change the map for the next round.
    /// </summary>
    Map,

    /// <summary>
    /// Vote to kick a player.
    /// </summary>
    Votekick,

    // Sunrise-Start
    /// <summary>
    /// Vote to end the current Planet Prison run (per specific prison map).
    /// </summary>
    PrisonEnd,

    /// <summary>
    /// Vote to exclude a player from the current Planet Prison map.
    /// </summary>
    PrisonExclude
    // Sunrise-End
}

/// <summary>
/// Reasons available to initiate a votekick.
/// </summary>
public enum VotekickReasonType : byte
{
    Raiding,
    Cheating,
    Spam
}

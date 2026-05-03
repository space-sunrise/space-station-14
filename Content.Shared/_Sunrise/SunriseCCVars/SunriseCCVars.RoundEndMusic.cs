using Robust.Shared.Configuration;

namespace Content.Shared._Sunrise.SunriseCCVars;

public sealed partial class SunriseCCVars
{
    /// <summary>
    ///     Round-end music on the summary screen.
    /// </summary>
    public static readonly CVarDef<bool> RoundEndMusicEnabled =
        CVarDef.Create("music.round_end_music_enabled", true, CVar.ARCHIVE | CVar.CLIENTONLY);

    /// <summary>
    ///     Prototype with weighted round-end music options for the scoreboard.
    ///     If empty, scoreboard music falls back to a random track from the lobby collection.
    /// </summary>
    public static readonly CVarDef<string> RoundEndMusicPool =
        CVarDef.Create("music.round_end_music_pool", string.Empty, CVar.SERVERONLY);
}

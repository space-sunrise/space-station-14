using Robust.Shared.Configuration;

namespace Content.Shared._Sunrise.SunriseCCVars;

public sealed partial class SunriseCCVars
{
    /// <summary>
    /// Maximum length of directional emotes.
    /// </summary>
    public static readonly CVarDef<int> DirectionalEmoteMaxLength =
        CVarDef.Create("directional_emote.max_length", 8000, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// Max distance for directional emotes between entities.
    /// </summary>
    public static readonly CVarDef<float> DirectionalEmoteMaxDistance =
        CVarDef.Create("directional_emote.max_distance", 2.3f, CVar.SERVER | CVar.REPLICATED);
}

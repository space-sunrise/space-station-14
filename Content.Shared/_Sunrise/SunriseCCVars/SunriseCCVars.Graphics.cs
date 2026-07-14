using Robust.Shared.Configuration;

namespace Content.Shared._Sunrise.SunriseCCVars;

public sealed partial class SunriseCCVars
{
    /// <summary>
    /// Enables the bloom effect rendered around compatible light fixtures.
    /// </summary>
    public static readonly CVarDef<bool> LightBloomEnabled =
        CVarDef.Create("sunrise.light_bloom_enabled", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Enables additional visibility filtering for bloom lights.
    /// </summary>
    public static readonly CVarDef<bool> LightBloomVisibilityFiltering =
        CVarDef.Create("sunrise.light_bloom_visibility_filtering", false, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Controls the bloom effect intensity.
    /// </summary>
    public static readonly CVarDef<float> LightBloomStrength =
        CVarDef.Create("sunrise.light_bloom_strength", 1f, CVar.CLIENTONLY | CVar.ARCHIVE);
}

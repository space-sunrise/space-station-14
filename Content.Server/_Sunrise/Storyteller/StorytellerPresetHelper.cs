namespace Content.Server._Sunrise.Storyteller;

/// <summary>
/// Static helper class containing Sunrise custom presets configuration and overrides.
/// Keeps upstream official files clean by isolating custom storyteller preset logic.
/// </summary>
public static class StorytellerPresetHelper
{
    /// <summary>
    /// The unique identifier of the Storyteller preset.
    /// </summary>
    public const string StorytellerPresetId = "Storyteller";

    /// <summary>
    /// Integrates the Storyteller preset dynamically into the available preset pools.
    /// </summary>
    public static void AdjustPresetPool(Dictionary<string, int[] > presets, Robust.Shared.Configuration.IConfigurationManager cfg)
    {
        if (cfg.GetCVar(Content.Shared._Sunrise.SunriseCCVars.SunriseCCVars.StorytellerEnabled) && !presets.ContainsKey(StorytellerPresetId))
        {
            presets.Add(StorytellerPresetId, new[] { 0, 200 });
        }
    }

    /// <summary>
    /// Checks whether the specified preset should bypass round-exclusion limits.
    /// </summary>
    public static bool ShouldBypassExclusion(string presetId)
    {
        return presetId == StorytellerPresetId;
    }
}

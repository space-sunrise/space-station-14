using Robust.Shared.Configuration;

namespace Content.Shared._Sunrise.SunriseCCVars;

public sealed partial class SunriseCCVars
{
    /// <summary>
    /// Whether the dynamic storyteller system is enabled.
    /// </summary>
    public static readonly CVarDef<bool> StorytellerEnabled =
        CVarDef.Create("storyteller.enabled", true, CVar.SERVER | CVar.ARCHIVE);

    /// <summary>
    /// How often the storyteller system evaluates the station state (in seconds).
    /// </summary>
    public static readonly CVarDef<float> StorytellerCheckInterval =
        CVarDef.Create("storyteller.check_interval", 30f, CVar.SERVER | CVar.ARCHIVE);

    /// <summary>
    /// Whether the storyteller should optionally connect to the AI API.
    /// </summary>
    public static readonly CVarDef<bool> StorytellerAiEnabled =
        CVarDef.Create("storyteller.ai_enabled", false, CVar.SERVER | CVar.ARCHIVE);

    /// <summary>
    /// The API URL to post round stats and receive storyteller event recommendations.
    /// </summary>
    public static readonly CVarDef<string> StorytellerAiUrl =
        CVarDef.Create("storyteller.ai_url", "", CVar.SERVERONLY | CVar.ARCHIVE);

    /// <summary>
    /// Whether structured JSON logging to the sawmill (Loki integration) is enabled.
    /// </summary>
    public static readonly CVarDef<bool> StorytellerTelemetryEnabled =
        CVarDef.Create("storyteller.telemetry_enabled", true, CVar.SERVERONLY | CVar.ARCHIVE);
}

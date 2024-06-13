﻿using Robust.Shared.Configuration;

namespace Content.Shared._Sunrise.SunriseCCVars;

[CVarDefs]
public sealed class SunriseCCVars
{
    /**
     * TTS (Text-To-Speech)
     */

    /// <summary>
    /// URL of the TTS server API.
    /// </summary>
    public static readonly CVarDef<bool> TTSEnabled =
        CVarDef.Create("tts.enabled", false, CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE);

    /// <summary>
    /// URL of the TTS server API.
    /// </summary>
    public static readonly CVarDef<string> TTSApiUrl =
        CVarDef.Create("tts.api_url", "", CVar.SERVERONLY | CVar.ARCHIVE);

    /// <summary>
    /// Auth token of the TTS server API.
    /// </summary>
    public static readonly CVarDef<string> TTSApiToken =
        CVarDef.Create("tts.api_token", "", CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    /// Amount of seconds before timeout for API
    /// </summary>
    public static readonly CVarDef<int> TTSApiTimeout =
        CVarDef.Create("tts.api_timeout", 5, CVar.SERVERONLY | CVar.ARCHIVE);

    /// <summary>
    /// Option to disable TTS events for client
    /// </summary>
    public static readonly CVarDef<bool> TTSClientEnabled =
        CVarDef.Create("tts.client_enabled", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Default volume setting of TTS sound
    /// </summary>
    public static readonly CVarDef<float> TTSVolume =
        CVarDef.Create("tts.volume", 0.50f, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<float> TTSRadioVolume =
        CVarDef.Create("tts.radio_volume", 0.50f, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<float> TTSAnnounceVolume =
        CVarDef.Create("tts.announce_volume", 0.50f, CVar.CLIENTONLY | CVar.ARCHIVE);

    /**
     * Ban Webhook
     */

    public static readonly CVarDef<string> DiscordBanWebhook =
        CVarDef.Create("discord.ban_webhook", string.Empty, CVar.SERVERONLY);

    /*
     * Discord Auth
     */

    public static readonly CVarDef<bool> DiscordAuthEnabled =
        CVarDef.Create("discord_auth.enabled", false, CVar.SERVERONLY);

    public static readonly CVarDef<string> DiscordAuthApiUrl =
        CVarDef.Create("discord_auth.api_url", "", CVar.SERVERONLY);

    public static readonly CVarDef<bool> DiscordAuthCheckMember =
        CVarDef.Create("discord_auth.check_member", false, CVar.SERVERONLY);

    public static readonly CVarDef<string> DiscordAuthApiKey =
        CVarDef.Create("discord_auth.api_key", "", CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /*
     * GodMode RoundEnd
     */

    public static readonly CVarDef<bool> GodModeRoundEnd =
        CVarDef.Create("game.godmode_end", true, CVar.SERVERONLY);

    /*
     * Peaceful Round End
     */

    /// <summary>
    /// Making everyone a pacifist at the end of a round.
    /// </summary>
    public static readonly CVarDef<bool> PeacefulRoundEnd =
        CVarDef.Create("game.peaceful_end", true, CVar.SERVERONLY);

    /*
     * Servers Hub
     */

    /// <summary>
    /// Список серверов отображаемых в хабе. Разделяются через запятую.
    /// </summary>
    public static readonly CVarDef<string> ServersHubList =
        CVarDef.Create("servers_hub.urls", "", CVar.SERVERONLY);

    /**
     * Transit hub
     */

    /// <summary>
    /// До сколько часов общего наиграного времени игроки будут появляться на станции даже в позднем присоединеии.
    /// </summary>
    public static readonly CVarDef<int> ArrivalsMinHours =
        CVarDef.Create("transithub.arrivals_min_hours", 10, CVar.SERVER | CVar.ARCHIVE);
}

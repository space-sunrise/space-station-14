using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Shared._Sunrise.SunriseCCVars;

public sealed partial class SunriseCCVars : CVars
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
        CVarDef.Create("tts.api_timeout", 10, CVar.SERVERONLY | CVar.ARCHIVE);

    /// <summary>
    /// Option to disable TTS events for client
    /// </summary>
    public static readonly CVarDef<bool> TTSClientEnabled =
        CVarDef.Create("tts.client_enabled", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<bool> TTSRadioGhostEnabled =
        CVarDef.Create("tts.radio_ghost_enabled", false, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Option to disable TTS queue in radio for client
    /// </summary>
    public static readonly CVarDef<bool> TTSClientQueueEnabled =
        CVarDef.Create("tts.queue_enabled", false, CVar.CLIENTONLY | CVar.ARCHIVE);

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
     * Service Authorization
     */

    public static readonly CVarDef<bool> ServiceAuthEnabled =
        CVarDef.Create("service_auth.enabled", false, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<string> ServiceAuthApiUrl =
        CVarDef.Create("service_auth.api_url", "", CVar.SERVERONLY);

    public static readonly CVarDef<string> ServiceAuthApiToken =
        CVarDef.Create("service_auth.api_token", "", CVar.SERVERONLY | CVar.CONFIDENTIAL);

    public static readonly CVarDef<bool> ServiceAuthCheckTelegramMember =
        CVarDef.Create("service_auth.check_telegram_member", false, CVar.SERVERONLY);

    public static readonly CVarDef<bool> ServiceAuthCheckDiscordMember =
        CVarDef.Create("service_auth.check_discord_member", false, CVar.SERVERONLY);

    public static readonly CVarDef<string> ServiceAuthProjectName =
        CVarDef.Create("service_auth.project_name", string.Empty, CVar.SERVERONLY);

    /*
     * GodMode RoundEnd
     */

    public static readonly CVarDef<bool> GodModeRoundEnd =
        CVarDef.Create("game.godmode_end", false, CVar.SERVERONLY);

    /*
     * Queue
     */

    public static readonly CVarDef<bool>
        QueueEnabled = CVarDef.Create("queue.enabled", false, CVar.SERVERONLY);

    /*
     *  Sponsor API
     */

    public static readonly CVarDef<string> SponsorApiUrl =
        CVarDef.Create("sponsor.api_url", "", CVar.SERVERONLY);

    public static readonly CVarDef<string> SponsorApiToken =
        CVarDef.Create("sponsor.api_token", "", CVar.SERVERONLY | CVar.CONFIDENTIAL);

    public static readonly CVarDef<string> SponsorGhostTheme =
        CVarDef.Create("sponsor.ghost_theme", "", CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<string> SponsorPet =
        CVarDef.Create("sponsor.pet", "", CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<string> SponsorProjectName =
        CVarDef.Create("sponsor.project_name", string.Empty, CVar.SERVERONLY);

    /*
     *  Greetings
     */

    public static readonly CVarDef<bool> GreetingsEnable =
        CVarDef.Create("greetings.enable", true);

    public static readonly CVarDef<string> GreetingsMessage =
        CVarDef.Create("greetings.message", "Привет");

    public static readonly CVarDef<string> GreetingsAuthor =
        CVarDef.Create("greetings.author", "Сервер");

    /*
     * New Life
     */

    public static readonly CVarDef<bool> NewLifeEnable =
        CVarDef.Create("newlife.enable", true, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<bool> NewLifeSponsorOnly =
        CVarDef.Create("newlife.sponsor_only", false, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<int> NewLifeTimeout =
        CVarDef.Create("newlife.timeout", 5, CVar.SERVERONLY);

    /*
     * Servers Hub
     */

    public static readonly CVarDef<bool> ServersHubEnable =
        CVarDef.Create("servers_hub.enable", false, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// Список серверов отображаемых в хабе. Разделяются через запятую.
    /// </summary>
    public static readonly CVarDef<string> ServersHubList =
        CVarDef.Create("servers_hub.urls", "", CVar.SERVERONLY);

    /// <summary>
    /// Простое название сервера для отображения в хабе.
    /// </summary>

    public static readonly CVarDef<string> ServersHubShortName =
        CVarDef.Create("servers_hub.short_name", "SS14 SERVER", CVar.SERVERONLY);

    /**
     * Tape Player
     */

    /// <summary>
    /// Параметр отключения школьников с колонками у клиента.
    /// </summary>
    public static readonly CVarDef<bool> TapePlayerClientEnabled =
        CVarDef.Create("tape_player.client_enabled", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    /*
     * INFOLINKS
     */

    /// <summary>
    /// Link to boosty to show in the launcher.
    /// </summary>
    public static readonly CVarDef<string> InfoLinksDonate =
        CVarDef.Create("infolinks.donate", "", CVar.SERVER | CVar.REPLICATED);

    /**
     * Lobby
     */

    public static readonly CVarDef<string> LobbyBackgroundType =
        CVarDef.Create("lobby.background", "Random", CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<string> LobbyArt =
        CVarDef.Create("lobby.art", "Random", CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<string> LobbyAnimation =
        CVarDef.Create("lobby.animation", "Random", CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<string> LobbyParallax =
        CVarDef.Create("lobby.parallax", "Random", CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<float> LobbyOpacity =
        CVarDef.Create("lobby.lobby_opacity", 0.90f, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<string> ServerName =
        CVarDef.Create("lobby.server_name", "Sunrise Station", CVar.SERVER | CVar.REPLICATED);

    /*
     * Planet Prison
     */

    public static readonly CVarDef<bool> MinPlayersEnable =
        CVarDef.Create("planet_prison.enable", false, CVar.SERVERONLY);

    public static readonly CVarDef<int> MinPlayersPlanetPrison =
        CVarDef.Create("planet_prison.min_players", 0, CVar.SERVERONLY);

    /*
     * MaxLoadedChunks
     */

    public static readonly CVarDef<int> MaxLoadedChunks =
        CVarDef.Create("chunk.max", 100, CVar.ARCHIVE | CVar.REPLICATED | CVar.SERVER);

    /**
     * Roadmap
     */

    public static readonly CVarDef<string> RoadmapId =
        CVarDef.Create("roadmap.id", "SunriseRoadmap");

    /**
     * Lobby Changelog
     */

    public static readonly CVarDef<string> LobbyChangelogsList =
        CVarDef.Create("lobby_changelog.list", "ChangelogSunrise.yml,Changelog.yml", CVar.SERVER | CVar.REPLICATED);

    /*
     * Cryoteleport
     */

    public static readonly CVarDef<bool> CryoTeleportEnable =
        CVarDef.Create("cryo_teleport.enable", true, CVar.SERVERONLY);

    public static readonly CVarDef<int> CryoTeleportTransferDelay =
        CVarDef.Create("cryo_teleport.transfer_delay", 5, CVar.SERVERONLY);

    /*
     * Damage
     */

    public static readonly CVarDef<float> DamageVariance =
        CVarDef.Create("damage.variance", 0.15f, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<float> DamageModifier =
        CVarDef.Create("damage.damage_modifier", 1f, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<float> HealModifier =
        CVarDef.Create("damage.heal_modifier", 1.2f, CVar.SERVER | CVar.REPLICATED);

    /*
     * NPCs
     */

    public static readonly CVarDef<bool> NPCDisableWithoutPlayers = CVarDef.Create("npc.disable_without_players", true);

    public static readonly CVarDef<float> NPCDisableDistance = CVarDef.Create("npc.disable_distance", 20f);

    /*
     * Vote
     */

    public static readonly CVarDef<bool> ShowRestartVotes = CVarDef.Create("vote.show_restart_votes", true);

    public static readonly CVarDef<bool> ShowPresetVotes = CVarDef.Create("vote.show_preset_votes", true);

    public static readonly CVarDef<bool> ShowMapVotes = CVarDef.Create("vote.show_map_votes", true);

    public static readonly CVarDef<bool> RunMapVoteAfterRestart =
        CVarDef.Create("vote.run_map_vote_after_restart", false);

    public static readonly CVarDef<bool> RunPresetVoteAfterRestart =
        CVarDef.Create("vote.run_preset_vote_after_restart", false);

    public static readonly CVarDef<int> VotingsDelay = CVarDef.Create("vote.votings_delay", 60);

    public static readonly CVarDef<bool> VoteMusicDisable =
        CVarDef.Create("vote.music_disable", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<bool> VoteDisableOOC =
        CVarDef.Create("vote.disable_ooc", false, CVar.SERVERONLY);

    public static readonly CVarDef<bool> VotePause =
        CVarDef.Create("vote.pause", true, CVar.SERVERONLY);

    public static readonly CVarDef<bool> ExcludeMaps = CVarDef.Create("vote.exclude_maps", false, CVar.SERVERONLY);

    public static readonly CVarDef<bool> ExcludePresets =
        CVarDef.Create("vote.exclude_presets", true, CVar.SERVERONLY);

    /*
     * Preset
     */

    public static readonly CVarDef<bool> ResetPresetAfterRestart =
        CVarDef.Create("game.reset_preset_after_restart", false);

    public static readonly CVarDef<string> GamePresetPool =
        CVarDef.Create("game.preset_pool", "DefaultHighPopPresetPool", CVar.SERVERONLY);

    /*
     * Ban links.
     */

    public static readonly CVarDef<string> BanDiscordLink =
        CVarDef.Create("cl.discord_link", "", CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE);

    public static readonly CVarDef<string> BanTelegramLink =
        CVarDef.Create("cl.telegram_link", "", CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE);

    /*
     * Mood
     */

    public static readonly CVarDef<bool> MoodEnabled =
        CVarDef.Create("mood.enabled", true, CVar.SERVER);

    public static readonly CVarDef<bool> MoodIncreasesSpeed =
        CVarDef.Create("mood.increases_speed", true, CVar.SERVER);

    public static readonly CVarDef<bool> MoodDecreasesSpeed =
        CVarDef.Create("mood.decreases_speed", true, CVar.SERVER);

    /**
     * Jump
     */

    public static readonly CVarDef<bool> JumpEnable =
        CVarDef.Create("jump.enable", true, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<float> JumpDeadChance =
        CVarDef.Create("jump.dead_chance", 0.001f, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<float> JumpCooldown =
        CVarDef.Create("jump.cooldown", 0.600f, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<bool> JumpSoundDisable =
        CVarDef.Create("jump.sound_disable", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<bool> BunnyHopEnable =
        CVarDef.Create("bunny_hop.enable", true, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<float> BunnyHopSpeedUpPerJump =
        CVarDef.Create("bunny_hop.speed_up_per_jump", 0.005f, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<float> BunnyHopSpeedLimit =
        CVarDef.Create("bunny_hop.speed_limit", 2.0f, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<float> BunnyHopMinSpeedThreshold =
        CVarDef.Create("bunny_hop.min_speed_threshold", 4.0f, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<float> BunnyHopSpeedBoostWindow =
        CVarDef.Create("bunny_hop.speed_boost_window", 0.610f, CVar.SERVER | CVar.REPLICATED);

    /**
     * Flip
     */

    public static readonly CVarDef<float> FlipDeadChance =
        CVarDef.Create("flip.dead_chance", 0.001f, CVar.SERVER | CVar.REPLICATED);

    /**
     * Slip
     */

    public static readonly CVarDef<float> SlipDeadChance =
        CVarDef.Create("slip.dead_chance", 0.001f, CVar.SERVER | CVar.REPLICATED);

    /**
     * Fall
     */

    public static readonly CVarDef<float> FallDeadChance =
        CVarDef.Create("fall.dead_chance", 0.01f, CVar.SERVER | CVar.REPLICATED);

    /**
     * VigersRay
     */

    public static readonly CVarDef<bool> VigersRayJoinNotifyEveryone =
        CVarDef.Create("vigers_ray.join_notify_everyone", false, CVar.SERVERONLY);

    public static readonly CVarDef<bool> VigersRayJoinSoundEveryone =
        CVarDef.Create("vigers_ray.join_sound_everyone", false, CVar.SERVERONLY);

    public static readonly CVarDef<bool> VigersRayJoinShockEveryone =
        CVarDef.Create("vigers_ray.join_shock_everyone", false, CVar.SERVERONLY);

    public static readonly CVarDef<string> VigersRayVictims =
        CVarDef.Create("vigers_ray.victims", "", CVar.SERVERONLY);

    /// <summary>
    ///     Flavor Profile
    /// </summary>
    public static readonly CVarDef<bool> FlavorTextSponsorOnly =
        CVarDef.Create("flavor_text.sponsor_only", true, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<int> FlavorTextBaseLength =
        CVarDef.Create("flavor_text.length", 512, CVar.SERVER | CVar.REPLICATED);

    /*
     * Damage Overlay
     */

    public static readonly CVarDef<bool> DamageOverlayEnable =
        CVarDef.Create("damage_overlay.enable", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<bool> DamageOverlaySelf =
        CVarDef.Create("damage_overlay.self", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<bool> DamageOverlayStructures =
        CVarDef.Create("damage_overlay.structures", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    /*
     * Radio chat icons
     */

    public static readonly CVarDef<bool> ChatIconsEnable =
        CVarDef.Create("chat_icon.enable", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    /*
     * Pointing chat visuals
     */

    public static readonly CVarDef<bool> ChatPointingVisuals =
        CVarDef.Create("chat_icon_pointing.enable", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    /*
     * Mute new ghost role sound
     */

    public static readonly CVarDef<bool> MuteGhostRoleNotification =
        CVarDef.Create("ghost.mute_role_notification", false, CVar.CLIENTONLY | CVar.ARCHIVE);

    /*
     * Heartbeat sound
     */

    public static readonly CVarDef<bool> PlayHeartBeatSound =
        CVarDef.Create("heartbeat.play_sound", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    /**
     * Transit hub
     */

    /// <summary>
    /// До сколько часов общего наиграного времени игроки будут появляться на станции даже в позднем присоединеии.
    /// </summary>
    public static readonly CVarDef<int> ArrivalsMinHours =
        CVarDef.Create("transithub.arrivals_min_hours", 0, CVar.SERVER | CVar.ARCHIVE);

    public static readonly CVarDef<bool> ArrivalsRoundStartSpawn =
        CVarDef.Create("transithub.arrivals_round_start_spawn", false, CVar.SERVER | CVar.ARCHIVE);

    /*
     * Random items-artifacts
     */

    /// <summary>
    /// Включены ли артефакты-предметы? Переключение этого в моменты игры динамически включает и выключает фичу
    /// </summary>
    public static readonly CVarDef<bool> EnableRandomArtifacts =
        CVarDef.Create("random_artifacts.enable", false, CVar.SERVER | CVar.ARCHIVE);

    /// <summary>
    /// Соотношение артефактов-предметов к обычным предметам.
    /// </summary>
    public static readonly CVarDef<float> ItemToArtifactRatio =
        CVarDef.Create("random_artifacts.ratio", 0.55f, CVar.SERVER | CVar.ARCHIVE);

    /*
     * AntiSpam params
     */
    public static readonly CVarDef<bool> AntiSpamEnable =
        CVarDef.Create("anti_spam.enable", false, CVar.SERVER | CVar.ARCHIVE);
    public static readonly CVarDef<int> AntiSpamCounterShort =
        CVarDef.Create("anti_spam.counter_short", 1, CVar.SERVER | CVar.ARCHIVE);
    public static readonly CVarDef<int> AntiSpamCounterLong =
        CVarDef.Create("anti_spam.counter_long", 2, CVar.SERVER | CVar.ARCHIVE);
    public static readonly CVarDef<float> AntiSpamMuteDuration =
        CVarDef.Create("anti_spam.mute_duration", 10f, CVar.SERVER | CVar.ARCHIVE);
    public static readonly CVarDef<float> AntiSpamTimeShort =
        CVarDef.Create("anti_spam.time_short", 1.5f, CVar.SERVER | CVar.ARCHIVE);
    public static readonly CVarDef<float> AntiSpamTimeLong =
        CVarDef.Create("anti_spam.time_long", 5f, CVar.SERVER | CVar.ARCHIVE);

    /// <summary>
    /// Вроде все очевидно
    /// </summary>
    public static readonly CVarDef<string> IpWhitelist =
        CVarDef.Create("admin.ip_whitelist", "", CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /*
     * Chat sanitization
     */

    /// <summary>
    /// Включена ли санитизация чата (антиспам от набегаторов)
    /// </summary>
    public static readonly CVarDef<bool> ChatSanitizationEnable =
        CVarDef.Create("chatsan.enable", true, CVar.SERVER | CVar.ARCHIVE);

    /// <summary>
    /// Контроллирует поведение санитизации.
    /// Агрессивное: если сообщение не проходит критерии - блокировать полностью его.
    /// Обычное: в сообщении, которое не проходит критерии, удалять не проходящие критерии части.
    /// </summary>
    public static readonly CVarDef<bool> ChatSanitizationAggressive =
        CVarDef.Create("chatsan.aggressive", true, CVar.SERVER | CVar.ARCHIVE);

    /// <summary>
    /// Смена даты на документах от принтера
    /// </summary>
    public static readonly CVarDef<int> PrinterDocTimeOffsetHours =
        CVarDef.Create("printerdoc.time_offset_hours", 3, CVar.SERVERONLY);
    public static readonly CVarDef<int> PrinterDocYearOffset =
        CVarDef.Create("printerdoc.year_offset", 1000, CVar.SERVERONLY);

    public static readonly CVarDef<bool> HoldLookUp =
        CVarDef.Create("scope.hold_look_up", true, CVar.CLIENT | CVar.ARCHIVE);
}

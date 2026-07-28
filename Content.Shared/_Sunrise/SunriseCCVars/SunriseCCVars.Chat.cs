using Robust.Shared.Configuration;

namespace Content.Shared._Sunrise.SunriseCCVars;

public sealed partial class SunriseCCVars
{
    /*
     * Визуал чата
     */

    /// <summary>
    /// Включает отображение иконок радиоканалов и других тегов иконок чата в клиентском чате.
    /// </summary>
    public static readonly CVarDef<bool> ChatIconsEnabled =
        CVarDef.Create("chat_icon.enable", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Включает клиентские визуальные теги для pointing popup-сообщений, дублируемых в чат.
    /// </summary>
    public static readonly CVarDef<bool> ChatPointingVisualsEnabled =
        CVarDef.Create("chat_icon_pointing.enable", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    /*
     * Антиспам чата
     */

    /// <summary>
    /// Включает серверную антиспам-проверку повторяющихся сообщений в игровом чате.
    /// </summary>
    public static readonly CVarDef<bool> ChatAntiSpamEnabled =
        CVarDef.Create("anti_spam.enable", false, CVar.SERVER | CVar.ARCHIVE);

    /// <summary>
    /// Максимальное число одинаковых сообщений в коротком временном окне до срабатывания мута.
    /// </summary>
    public static readonly CVarDef<int> ChatAntiSpamShortRepeatLimit =
        CVarDef.Create("anti_spam.counter_short", 1, CVar.SERVER | CVar.ARCHIVE);

    /// <summary>
    /// Максимальное число одинаковых сообщений в длинном временном окне до срабатывания мута.
    /// </summary>
    public static readonly CVarDef<int> ChatAntiSpamLongRepeatLimit =
        CVarDef.Create("anti_spam.counter_long", 2, CVar.SERVER | CVar.ARCHIVE);

    /// <summary>
    /// Длительность мута за срабатывание спама, в секундах.
    /// </summary>
    public static readonly CVarDef<float> ChatAntiSpamMuteDuration =
        CVarDef.Create("anti_spam.mute_duration", 10f, CVar.SERVER | CVar.ARCHIVE);

    /// <summary>
    /// Размер короткого окна антиспама, в пределах которого считается быстрый повтор сообщений, в секундах.
    /// </summary>
    public static readonly CVarDef<float> ChatAntiSpamShortWindow =
        CVarDef.Create("anti_spam.time_short", 1.5f, CVar.SERVER | CVar.ARCHIVE);

    /// <summary>
    /// Размер длинного окна антиспама, в пределах которого считается общий повтор сообщений, в секундах.
    /// </summary>
    public static readonly CVarDef<float> ChatAntiSpamLongWindow =
        CVarDef.Create("anti_spam.time_long", 5f, CVar.SERVER | CVar.ARCHIVE);

    /*
     * Санитизация чата
     */

    /// <summary>
    /// Включена ли санитизация чата (антиспам от набегаторов)
    /// </summary>
    public static readonly CVarDef<bool> ChatSanitizationEnabled =
        CVarDef.Create("chatsan.enable", true, CVar.SERVER | CVar.ARCHIVE);

    /// <summary>
    /// Контроллирует режим санитизации.
    /// True: сообщение, не прошедшее проверку, блокируется полностью.
    /// False: из сообщения удаляются только запрещённые фрагменты.
    /// </summary>
    public static readonly CVarDef<bool> ChatSanitizationAggressive =
        CVarDef.Create("chatsan.aggressive", true, CVar.SERVER | CVar.ARCHIVE);

    /*
     * Менторская помощь
     */

    /// <summary>
    /// Добавляет админский префикс к имени отправителя в сообщениях менторской помощи, если он является администратором.
    /// </summary>
    public static readonly CVarDef<bool> MentorHelpAdminPrefixEnabled =
        CVarDef.Create("mentor_help.admin_prefix", true, CVar.SERVERONLY);

    /// <summary>
    /// Длина окна rate limit для менторской помощи, в секундах.
    /// </summary>
    public static readonly CVarDef<float> MentorHelpRateLimitPeriod =
        CVarDef.Create("mentor_help.rate_limit_period", 2f, CVar.SERVERONLY);

    /// <summary>
    /// Максимальное количество сообщений менторской помощи за окно rate limit.
    /// </summary>
    public static readonly CVarDef<int> MentorHelpRateLimitCount =
        CVarDef.Create("mentor_help.rate_limit_count", 10, CVar.SERVERONLY);

    /// <summary>
    /// Включает клиентский звук уведомления при получении сообщения менторской помощи.
    /// </summary>
    public static readonly CVarDef<bool> MentorHelpSoundEnabled =
        CVarDef.Create("mentor_help.mentor_help_sound_enabled", true, CVar.ARCHIVE | CVar.CLIENTONLY);

    /// <summary>
    /// Авто-открывать тикет при получении нового сообщения (только для автора и назначенного ментора).
    /// </summary>
    public static readonly CVarDef<bool> MentorHelpAutoOpenOnNewMessage =
        CVarDef.Create("mentor_help.auto_open_on_new_message", false, CVar.ARCHIVE | CVar.CLIENTONLY);

    /*
     * Messenger
     */

    /// <summary>
    /// Недавно использованные смайлики в интерфейсе messenger, разделённые запятыми.
    /// Хранится на клиенте, используется для быстрого доступа в окне выбора.
    /// </summary>
    public static readonly CVarDef<string> MessengerRecentEmojis =
        CVarDef.Create("messenger.recent_emojis", "", CVar.ARCHIVE | CVar.CLIENTONLY);

    /// <summary>
    /// Избранные смайлики в интерфейсе messenger, разделённые запятыми.
    /// </summary>
    public static readonly CVarDef<string> MessengerFavoriteEmojis =
        CVarDef.Create("messenger.favorite_emojis", "", CVar.ARCHIVE | CVar.CLIENTONLY);

    /// <summary>
    /// Включает механику автоматических спам-рассылок в messenger.
    /// </summary>
    public static readonly CVarDef<bool> MessengerSpamEnabled =
        CVarDef.Create("messenger.spam_enabled", true, CVar.SERVERONLY);

    /// <summary>
    /// Минимальная задержка между спам-волнами messenger, в секундах.
    /// </summary>
    public static readonly CVarDef<float> MessengerSpamMinInterval =
        CVarDef.Create("messenger.spam_min_time", 300f, CVar.SERVERONLY);

    /// <summary>
    /// Максимальная задержка между спам-волнами messenger, в секундах.
    /// </summary>
    public static readonly CVarDef<float> MessengerSpamMaxInterval =
        CVarDef.Create("messenger.spam_max_time", 600f, CVar.SERVERONLY);

    /// <summary>
    /// Доля пользователей станции, которые получат спам во время одной волны.
    /// Значение задаётся в диапазоне от 0.0 до 1.0.
    /// </summary>
    public static readonly CVarDef<float> MessengerSpamPlayerPercentage =
        CVarDef.Create("messenger.spam_player_percentage", 0.4f, CVar.SERVERONLY);
}

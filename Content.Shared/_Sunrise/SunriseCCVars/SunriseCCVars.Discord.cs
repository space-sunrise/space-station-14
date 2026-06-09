using Robust.Shared.Configuration;

namespace Content.Shared._Sunrise.SunriseCCVars;

public sealed partial class SunriseCCVars
{
    /// <summary>
    ///     Адрес прокси-сервера для подключений к Discord API (например, "http://proxy.example.com:8080" или "socks5://proxy.example.com:1080").
    ///     Если оставить пустым, прокси не используется.
    /// </summary>
    public static readonly CVarDef<string> DiscordProxyAddress =
        CVarDef.Create("discord.proxy_address", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    ///     Имя пользователя для аутентификации в прокси. Если оставить пустым, аутентификация не используется.
    /// </summary>
    public static readonly CVarDef<string> DiscordProxyUsername =
        CVarDef.Create("discord.proxy_username", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    ///     Пароль для аутентификации в прокси. Если оставить пустым, аутентификация не используется.
    /// </summary>
    public static readonly CVarDef<string> DiscordProxyPassword =
        CVarDef.Create("discord.proxy_password", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    ///     Пользовательские заголовки для запросов Discord webhook. Формат: "Header1:Value1|Header2:Value2"
    /// </summary>
    public static readonly CVarDef<string> DiscordCustomHeaders =
        CVarDef.Create("discord.custom_headers", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);
}

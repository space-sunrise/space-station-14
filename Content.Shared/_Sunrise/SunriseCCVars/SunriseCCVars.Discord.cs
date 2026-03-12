using Robust.Shared.Configuration;

namespace Content.Shared._Sunrise.SunriseCCVars;

public sealed partial class SunriseCCVars
{
    /// <summary>
    ///     Proxy server address for Discord API connections (e.g., "http://proxy.example.com:8080" or "socks5://proxy.example.com:1080").
    ///     If left empty, no proxy will be used.
    /// </summary>
    public static readonly CVarDef<string> DiscordProxyAddress =
        CVarDef.Create("discord.proxy_address", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    ///     Username for proxy authentication. If left empty, no authentication will be used.
    /// </summary>
    public static readonly CVarDef<string> DiscordProxyUsername =
        CVarDef.Create("discord.proxy_username", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    ///     Password for proxy authentication. If left empty, no authentication will be used.
    /// </summary>
    public static readonly CVarDef<string> DiscordProxyPassword =
        CVarDef.Create("discord.proxy_password", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    ///     Custom headers to add to Discord webhook requests. Format: "Header1:Value1|Header2:Value2"
    /// </summary>
    public static readonly CVarDef<string> DiscordCustomHeaders =
        CVarDef.Create("discord.custom_headers", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);
}

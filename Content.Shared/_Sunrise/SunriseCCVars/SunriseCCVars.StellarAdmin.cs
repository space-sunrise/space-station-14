using Robust.Shared.Configuration;

namespace Content.Shared._Sunrise.SunriseCCVars;

public sealed partial class SunriseCCVars
{
    public static readonly CVarDef<bool> StellarAdminEnabled =
        CVarDef.Create("stellar_admin.enabled", false, CVar.SERVERONLY | CVar.ARCHIVE);

    public static readonly CVarDef<string> StellarAdminApiUrl =
        CVarDef.Create("stellar_admin.api_url", string.Empty, CVar.SERVERONLY | CVar.ARCHIVE);

    public static readonly CVarDef<string> StellarAdminApiToken =
        CVarDef.Create("stellar_admin.api_token", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    public static readonly CVarDef<string> StellarAdminWebhookToken =
        CVarDef.Create("stellar_admin.webhook_token", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    public static readonly CVarDef<bool> StellarAdminWebhookIpAllowlistEnabled =
        CVarDef.Create("stellar_admin.webhook_ip_allowlist_enabled", false, CVar.SERVERONLY | CVar.ARCHIVE);

    public static readonly CVarDef<string> StellarAdminWebhookIpAllowlist =
        CVarDef.Create("stellar_admin.webhook_ip_allowlist", string.Empty, CVar.SERVERONLY | CVar.ARCHIVE);

    public static readonly CVarDef<string> StellarAdminWebhookTrustedProxyCidrs =
        CVarDef.Create("stellar_admin.webhook_trusted_proxy_cidrs", string.Empty, CVar.SERVERONLY | CVar.ARCHIVE);

    public static readonly CVarDef<string> StellarAdminProjectSlug =
        CVarDef.Create("stellar_admin.project_slug", "test-sunrise-station", CVar.SERVERONLY | CVar.ARCHIVE);

    public static readonly CVarDef<int> StellarAdminCacheTtlSeconds =
        CVarDef.Create("stellar_admin.cache_ttl_seconds", 300, CVar.SERVERONLY | CVar.ARCHIVE);

    public static readonly CVarDef<bool> StellarAdminFailClosed =
        CVarDef.Create("stellar_admin.fail_closed", true, CVar.SERVERONLY | CVar.ARCHIVE);
}

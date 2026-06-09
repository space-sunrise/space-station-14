using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared.Database;
using Robust.Shared.Log;
using Robust.Shared.Network;
#if SUNRISE_PRIVATE
using System.Net.Http;
using Content.Server._SunrisePrivate.MakuraAuth;
#endif

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Server.Administration.Managers;

public sealed partial class BanManager
{
#if SUNRISE_PRIVATE
    private readonly HttpClient _sunriseBanIdentityHttpClient = new();
    private MakuraDiscordIdentityResolver? _sunriseBanIdentityResolver;
#endif

    private string _sunriseBanIdentityApiUrl = string.Empty;
    private string _sunriseBanIdentityApiKey = string.Empty;

    private void InitializeSunriseBanWebhookHooks()
    {
#if SUNRISE_PRIVATE
        _sunriseBanIdentityResolver = new MakuraDiscordIdentityResolver(
            _sunriseBanIdentityHttpClient,
            _sawmill);
        _cfg.OnValueChanged(SunriseCCVars.MakuraAuthInternalApiUrl, value => _sunriseBanIdentityApiUrl = value, true);
        _cfg.OnValueChanged(SunriseCCVars.MakuraAuthInternalApiKey, value => _sunriseBanIdentityApiKey = value, true);
#endif
    }

    private async Task SendServerBanWebhookBestEffort(ServerBanDef banDef, uint? minutes)
    {
        try
        {
            var ban = await ResolveServerBanForWebhook(_db, banDef, _sawmill);
            await SendWebhook(await GenerateBanPayload(ban, minutes));
        }
        catch (Exception ex)
        {
            _sawmill.Warning("Failed to send ban webhook: {Message}", ex.Message);
        }
    }

    private async Task<SunriseBanWebhookMentionData> GetSunriseBanWebhookMentions(
        NetUserId? adminId,
        NetUserId? targetId)
    {
        var adminDiscordIdTask = GetSunriseBanDiscordUserId(adminId);
        var targetDiscordIdTask = GetSunriseBanDiscordUserId(targetId);
        await Task.WhenAll(adminDiscordIdTask, targetDiscordIdTask);

        var adminLink = string.Empty;
        var targetLink = string.Empty;
        var mentions = new List<User>();

        var adminDiscordId = await adminDiscordIdTask;
        if (adminDiscordId != null)
        {
            adminLink = $"<@{adminDiscordId}>";
            mentions.Add(new User { Id = adminDiscordId });
        }

        var targetDiscordId = await targetDiscordIdTask;
        if (targetDiscordId != null)
        {
            targetLink = $"<@{targetDiscordId}>";
            mentions.Add(new User { Id = targetDiscordId });
        }

        var allowedMentions = new Dictionary<string, string[]>
        {
            { "parse", new[] { "users" } },
        };

        return new SunriseBanWebhookMentionData(adminLink, targetLink, mentions, allowedMentions);
    }

    private async Task<string?> GetSunriseBanDiscordUserId(
        NetUserId? userId,
        CancellationToken cancel = default)
    {
#if SUNRISE_PRIVATE
        if (_sunriseBanIdentityResolver == null)
            return null;

        return await _sunriseBanIdentityResolver.GetDiscordUserId(
            userId,
            _sunriseBanIdentityApiUrl,
            _sunriseBanIdentityApiKey,
            cancel);
#else
        await Task.CompletedTask;
        return null;
#endif
    }

    internal static ServerBanWebhookLookup GetServerBanWebhookLookup(ServerBanDef banDef)
    {
        ImmutableArray<byte>? hwId = null;
        ImmutableArray<ImmutableArray<byte>>? modernHwIds = null;

        switch (banDef.HWId?.Type)
        {
            case HwidType.Legacy:
                hwId = banDef.HWId.Hwid;
                break;
            case HwidType.Modern:
                modernHwIds = ImmutableArray.Create(banDef.HWId.Hwid);
                break;
        }

        return new ServerBanWebhookLookup(
            banDef.Address?.address,
            banDef.UserId,
            hwId,
            modernHwIds);
    }

    internal static async Task<ServerBanDef> ResolveServerBanForWebhook(
        IServerDbManager db,
        ServerBanDef banDef,
        ISawmill sawmill)
    {
        var lookup = GetServerBanWebhookLookup(banDef);

        try
        {
            var resolvedBan = await db.GetServerBanAsync(
                lookup.Address,
                lookup.UserId,
                lookup.HWId,
                lookup.ModernHWIds);

            return resolvedBan ?? banDef;
        }
        catch (Exception ex)
        {
            sawmill.Warning("Failed to resolve persisted server ban for webhook: {Message}", ex.Message);
            return banDef;
        }
    }

    private readonly record struct SunriseBanWebhookMentionData(
        string AdminLink,
        string TargetLink,
        List<User> Mentions,
        Dictionary<string, string[]> AllowedMentions);
}

internal readonly record struct ServerBanWebhookLookup(
    IPAddress? Address,
    NetUserId? UserId,
    ImmutableArray<byte>? HWId,
    ImmutableArray<ImmutableArray<byte>>? ModernHWIds);

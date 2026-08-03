using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Robust.Shared.Network;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Server.Database;

public sealed partial class ServerDbPostgres
{
    private const string SunriseBanLookupError =
        "At least one ban lookup key (IP/UserID/Legacy HWID/Modern HWID) must have been given to make query not null.";

    private static void EnsureSunriseBanLookupKey(
        IPAddress? address,
        NetUserId? userId,
        ImmutableArray<byte>? hwId,
        ImmutableArray<ImmutableArray<byte>>? modernHWIds)
    {
        if (!HasBanLookupKey(address, userId, hwId, modernHWIds))
            throw new ArgumentException("Address, userId, hwId, and modernHWIds cannot all be empty");
    }

    internal static bool HasBanLookupKey(
        IPAddress? address,
        NetUserId? userId,
        ImmutableArray<byte>? hwId,
        ImmutableArray<ImmutableArray<byte>>? modernHWIds)
    {
        return address != null ||
               userId != null ||
               hwId is { Length: > 0 } ||
               modernHWIds is { Length: > 0 };
    }

    public override async Task<List<ServerBanDef>> GetServerBansByAdminAsync(
        NetUserId adminId,
        DateTimeOffset since)
    {
        await using var db = await GetDbImpl();
        var bans = await db.PgDbContext.Ban
            .Include(ban => ban.Unban)
            .Where(ban => ban.BanningAdmin == adminId.UserId && ban.BanTime >= since.UtcDateTime)
            .ToListAsync();

        var result = new List<ServerBanDef>();
        foreach (var ban in bans)
        {
            if (ConvertBan(ban) is { } banDefinition)
                result.Add(banDefinition);
        }

        return result;
    }

    public override async Task DeleteServerBanAsync(int banId)
    {
        await using var db = await GetDbImpl();
        var unbans = db.PgDbContext.Unban.Where(unban => unban.BanId == banId);
        db.PgDbContext.Unban.RemoveRange(unbans);

        var ban = await db.PgDbContext.Ban.SingleOrDefaultAsync(entry => entry.Id == banId);
        if (ban == null)
            return;

        db.PgDbContext.Ban.Remove(ban);
        await db.PgDbContext.SaveChangesAsync();
    }
}

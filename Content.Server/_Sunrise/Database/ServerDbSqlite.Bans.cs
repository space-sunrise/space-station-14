using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Robust.Shared.Network;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Server.Database;

public sealed partial class ServerDbSqlite
{
    public override async Task<List<ServerBanDef>> GetServerBansByAdminAsync(
        NetUserId adminId,
        DateTimeOffset since)
    {
        await using var db = await GetDbImpl();
        var bans = await db.SqliteDbContext.Ban
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
        var unbans = db.SqliteDbContext.Unban.Where(unban => unban.BanId == banId);
        db.SqliteDbContext.Unban.RemoveRange(unbans);

        var ban = await db.SqliteDbContext.Ban.SingleOrDefaultAsync(entry => entry.Id == banId);
        if (ban == null)
            return;

        db.SqliteDbContext.Ban.Remove(ban);
        await db.SqliteDbContext.SaveChangesAsync();
    }
}

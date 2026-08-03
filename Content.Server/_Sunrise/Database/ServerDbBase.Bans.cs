using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Robust.Shared.Network;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Server.Database;

public abstract partial class ServerDbBase
{
    public abstract Task<List<ServerBanDef>> GetServerBansByAdminAsync(NetUserId adminId, DateTimeOffset since);
    public abstract Task DeleteServerBanAsync(int banId);

    private DateTime? NormalizeSunriseBanTimestamp(DateTime? timestamp)
    {
        return NormalizeDatabaseTime(timestamp);
    }
}

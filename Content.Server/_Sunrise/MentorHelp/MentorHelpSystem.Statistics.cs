using System.Linq;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared._Sunrise.MentorHelp;
using Content.Shared.Administration;
using Robust.Shared.Network;

namespace Content.Server._Sunrise.MentorHelp;

public sealed partial class MentorHelpSystem
{
    private sealed record OfflineAdminLookups(
        Dictionary<Guid, Admin> AdminsByUserId,
        Dictionary<int, AdminRank> AdminRanksById);

    private async Task<MentorStatisticsCache> GetStatisticsCacheAsync()
    {
        var now = DateTimeOffset.UtcNow;

        if (_mentorStatsCache != null &&
            _mentorStatsCacheTime != null &&
            (now - _mentorStatsCacheTime.Value).TotalMinutes < _mentorCacheInterval)
        {
            return _mentorStatsCache;
        }

        var cacheVersion = _mentorStatsCacheVersion;
        var cache = await BuildStatisticsCacheAsync(now);

        if (cacheVersion == _mentorStatsCacheVersion)
        {
            _mentorStatsCache = cache;
            _mentorStatsCacheTime = now;
        }

        return cache;
    }

    private static HashSet<Guid> CollectMentorUserIds(params IEnumerable<MentorHelpStatistics>[] statisticsBuckets)
    {
        var mentorUserIds = new HashSet<Guid>();

        foreach (var statistics in statisticsBuckets)
        {
            foreach (var stat in statistics)
            {
                mentorUserIds.Add(stat.MentorUserId);
            }
        }

        return mentorUserIds;
    }

    private async Task<HashSet<Guid>> GetActiveMentorIdsAsync(HashSet<Guid> mentorUserIds)
    {
        var activeMentorIds = new HashSet<Guid>();
        OfflineAdminLookups? offlineLookups = null;

        foreach (var mentorUserId in mentorUserIds)
        {
            var (isMentor, updatedLookups) = await IsMentorUserAsync(mentorUserId, offlineLookups);
            offlineLookups = updatedLookups;

            if (isMentor)
                activeMentorIds.Add(mentorUserId);
        }

        return activeMentorIds;
    }

    private async Task<(bool IsMentor, OfflineAdminLookups? Lookups)> IsMentorUserAsync(
        Guid mentorUserId,
        OfflineAdminLookups? lookups)
    {
        if (_playerManager.TryGetSessionById(new NetUserId(mentorUserId), out var mentorSession))
            return (_adminManager.GetAdminData(mentorSession)?.HasFlag(AdminFlags.Mentor) ?? false, lookups);

        lookups ??= await GetOfflineAdminLookupsAsync();

        var isMentor = lookups.AdminsByUserId.TryGetValue(mentorUserId, out var adminData) &&
            HasAdminFlag(adminData, lookups.AdminRanksById, AdminFlags.Mentor);

        return (isMentor, lookups);
    }

    private async Task<OfflineAdminLookups> GetOfflineAdminLookupsAsync()
    {
        var (admins, adminRanks) = await _dbManager.GetAllAdminAndRanksAsync();

        return new OfflineAdminLookups(
            admins.ToDictionary(admin => admin.Item1.UserId, admin => admin.Item1),
            adminRanks.ToDictionary(rank => rank.Id));
    }
}

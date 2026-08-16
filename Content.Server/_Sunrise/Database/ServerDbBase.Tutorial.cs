using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Shared._Sunrise.Tutorial.Prototypes;
using Microsoft.EntityFrameworkCore;
using Robust.Shared.Prototypes;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Server.Database;

public abstract partial class ServerDbBase
{
    public async Task<bool> AddTutorial(
        Guid player,
        ProtoId<TutorialSequencePrototype> tutorial,
        TimeSpan? accountAge = null)
    {
        await using var db = await GetDb();
        var entry = await db.DbContext.TutorialCompletions
            .Where(completion => completion.PlayerUserId == player)
            .Where(completion => completion.TutorialId == tutorial.Id)
            .SingleOrDefaultAsync();

        var now = DateTimeOffset.UtcNow;
        var accountAgeDays = accountAge != null
            ? (double?) accountAge.Value.TotalDays
            : null;
        var isNew = entry == null;

        if (isNew)
        {
            entry = new TutorialCompletion
            {
                PlayerUserId = player,
                TutorialId = tutorial.Id,
                CompletedAt = now,
                AccountAgeDays = accountAgeDays,
                CompletionCount = 1,
            };
            db.DbContext.TutorialCompletions.Add(entry);
        }
        else
        {
            entry!.CompletedAt = now;
            entry.CompletionCount++;
            if (accountAgeDays != null)
                entry.AccountAgeDays = accountAgeDays;
        }

        await db.DbContext.SaveChangesAsync();
        return isNew;
    }

    public async Task<List<string>> GetTutorial(Guid player, CancellationToken cancel)
    {
        await using var db = await GetDb(cancel);
        return await db.DbContext.TutorialCompletions
            .Where(completion => completion.PlayerUserId == player)
            .Select(completion => completion.TutorialId)
            .ToListAsync(cancellationToken: cancel);
    }

    public async Task<bool> IsTutorialCompleted(
        Guid player,
        ProtoId<TutorialSequencePrototype> tutorial)
    {
        await using var db = await GetDb();
        return await db.DbContext.TutorialCompletions
            .Where(completion => completion.PlayerUserId == player)
            .Where(completion => completion.TutorialId == tutorial.Id)
            .AnyAsync();
    }

    public async Task<bool> RemoveTutorial(
        Guid player,
        ProtoId<TutorialSequencePrototype> tutorial)
    {
        await using var db = await GetDb();
        var entry = await db.DbContext.TutorialCompletions
            .Where(completion => completion.PlayerUserId == player)
            .Where(completion => completion.TutorialId == tutorial.Id)
            .SingleOrDefaultAsync();

        if (entry == null)
            return false;

        db.DbContext.TutorialCompletions.Remove(entry);
        await db.DbContext.SaveChangesAsync();
        return true;
    }

    public async Task<List<TutorialCompletionMetrics>> GetTutorialCompletionMetricsAsync(
        CancellationToken cancel = default)
    {
        await using var db = await GetDb(cancel);
        var isSqlite = db.DbContext.Database.ProviderName?.Contains("Sqlite") == true;

        if (isSqlite)
        {
            var metrics = await db.DbContext.TutorialCompletions
                .AsNoTracking()
                .GroupBy(completion => completion.TutorialId)
                .Select(group => new
                {
                    TutorialId = group.Key,
                    CompletedPlayers = group.Count(),
                    CompletionCount = group.Sum(completion => completion.CompletionCount),
                    AccountAgeSamples = group.Count(completion => completion.AccountAgeDays != null),
                    AverageAccountAgeDays = group.Average(completion => completion.AccountAgeDays),
                })
                .ToListAsync(cancellationToken: cancel);

            var completedAt = await db.DbContext.TutorialCompletions
                .AsNoTracking()
                .Select(completion => new
                {
                    completion.TutorialId,
                    completion.CompletedAt,
                })
                .ToListAsync(cancellationToken: cancel);
            var lastCompletedAtByTutorial = completedAt
                .GroupBy(completion => completion.TutorialId)
                .ToDictionary(
                    group => group.Key,
                    group => group.Max(completion => completion.CompletedAt));

            return metrics
                .Select(metric => new TutorialCompletionMetrics(
                    metric.TutorialId,
                    metric.CompletedPlayers,
                    metric.CompletionCount,
                    metric.AccountAgeSamples,
                    metric.AverageAccountAgeDays,
                    lastCompletedAtByTutorial[metric.TutorialId]))
                .ToList();
        }

        return await db.DbContext.TutorialCompletions
            .AsNoTracking()
            .GroupBy(completion => completion.TutorialId)
            .Select(group => new TutorialCompletionMetrics(
                group.Key,
                group.Count(),
                group.Sum(completion => completion.CompletionCount),
                group.Count(completion => completion.AccountAgeDays != null),
                group.Average(completion => completion.AccountAgeDays),
                group.Max(completion => completion.CompletedAt)))
            .ToListAsync(cancellationToken: cancel);
    }

    public async Task<int> PruneInvalidTutorialCompletionsAsync(
        IEnumerable<string> validTutorialIds,
        CancellationToken cancel = default)
    {
        await using var db = await GetDb(cancel);
        var validList = validTutorialIds.ToList();
        if (validList.Count == 0)
            return 0;

        var toRemove = await db.DbContext.TutorialCompletions
            .Where(completion => !validList.Contains(completion.TutorialId))
            .ToListAsync(cancellationToken: cancel);
        if (toRemove.Count == 0)
            return 0;

        db.DbContext.TutorialCompletions.RemoveRange(toRemove);
        await db.DbContext.SaveChangesAsync();
        return toRemove.Count;
    }
}

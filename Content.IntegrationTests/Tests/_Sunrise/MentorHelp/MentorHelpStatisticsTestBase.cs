using System.Collections.Generic;
using System.Linq;
using Content.Server.Database;
using Content.Shared._Sunrise.MentorHelp;
using Content.Shared.Database;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Robust.Shared.Configuration;
using Robust.Shared.Log;
using Robust.UnitTesting;

namespace Content.IntegrationTests.Tests._Sunrise.MentorHelp;

public abstract class MentorHelpStatisticsTestBase
{
    protected static readonly DateTimeOffset ReferenceTime = new(2026, 03, 06, 12, 00, 00, TimeSpan.Zero);

    protected static ServerDbSqlite GetDb(RobustIntegrationTest.ServerIntegrationInstance server)
    {
        var cfg = server.ResolveDependency<IConfigurationManager>();
        var opsLog = server.ResolveDependency<ILogManager>().GetSawmill("db.ops");
        var builder = new DbContextOptionsBuilder<SqliteServerDbContext>();
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        builder.UseSqlite(connection);
        return new ServerDbSqlite(() => builder.Options, true, cfg, true, opsLog);
    }

    protected static MentorHelpTicket CreateTicket(
        Guid playerId,
        Guid? assignedToUserId,
        MentorHelpTicketStatus status,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        DateTimeOffset? closedAt = null,
        Guid? closedByUserId = null)
    {
        return new MentorHelpTicket
        {
            PlayerId = playerId,
            AssignedToUserId = assignedToUserId,
            Subject = Guid.NewGuid().ToString(),
            Status = status,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            ClosedAt = closedAt,
            ClosedByUserId = closedByUserId
        };
    }

    protected static MentorHelpMessage CreateMessage(int ticketId, Guid senderUserId, string message, DateTimeOffset sentAt)
    {
        return new MentorHelpMessage
        {
            TicketId = ticketId,
            SenderUserId = senderUserId,
            Message = message,
            SentAt = sentAt
        };
    }

    protected static Dictionary<Guid, MentorHelpStatistics> GetStatisticsByMentor(IEnumerable<MentorHelpStatistics> statistics)
    {
        return statistics.ToDictionary(stat => stat.MentorUserId);
    }

    protected static void AssertTicketIds(IEnumerable<MentorHelpTicket> tickets, params int[] expectedIds)
    {
        Assert.That(tickets.Select(ticket => ticket.Id).ToArray(), Is.EqualTo(expectedIds));
    }
}

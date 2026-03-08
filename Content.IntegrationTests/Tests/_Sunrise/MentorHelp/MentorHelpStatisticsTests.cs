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

namespace Content.IntegrationTests.Tests.Sunrise.MentorHelp
{
    [TestFixture]
    [TestOf(typeof(ServerDbSqlite))]
    public sealed class MentorHelpStatisticsTests
    {
        private static readonly DateTimeOffset ReferenceTime = new(2026, 03, 06, 12, 00, 00, TimeSpan.Zero);

        private static ServerDbSqlite GetDb(RobustIntegrationTest.ServerIntegrationInstance server)
        {
            var cfg = server.ResolveDependency<IConfigurationManager>();
            var opsLog = server.ResolveDependency<ILogManager>().GetSawmill("db.ops");
            var builder = new DbContextOptionsBuilder<SqliteServerDbContext>();
            var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            builder.UseSqlite(connection);
            return new ServerDbSqlite(() => builder.Options, true, cfg, true, opsLog);
        }

        private static MentorHelpTicket CreateTicket(
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

        private static MentorHelpMessage CreateMessage(int ticketId, Guid senderUserId, string message, DateTimeOffset sentAt)
        {
            return new MentorHelpMessage
            {
                TicketId = ticketId,
                SenderUserId = senderUserId,
                Message = message,
                SentAt = sentAt
            };
        }

        private static Dictionary<Guid, MentorHelpStatistics> GetStatisticsByMentor(IEnumerable<MentorHelpStatistics> statistics)
        {
            return statistics.ToDictionary(stat => stat.MentorUserId);
        }

        private static void AssertTicketIds(IEnumerable<MentorHelpTicket> tickets, params int[] expectedIds)
        {
            Assert.That(tickets.Select(ticket => ticket.Id).ToArray(), Is.EqualTo(expectedIds));
        }

        [Test]
        public async Task GetMentorHelpStatisticsAsync_IncludesClosersAndMentorReplies()
        {
            await using var pair = await PoolManager.GetServerClient();
            var db = GetDb(pair.Server);

            var playerId = Guid.NewGuid();
            var closerId = Guid.NewGuid();
            var replierId = Guid.NewGuid();

            var closedTicket = CreateTicket(
                playerId,
                closerId,
                MentorHelpTicketStatus.Closed,
                ReferenceTime.AddDays(-3),
                ReferenceTime.AddDays(-1),
                ReferenceTime.AddDays(-1),
                closerId);

            var openTicket = CreateTicket(
                playerId,
                replierId,
                MentorHelpTicketStatus.Assigned,
                ReferenceTime.AddDays(-2),
                ReferenceTime.AddHours(-2));

            await db.AddMentorHelpTicketAsync(closedTicket);
            await db.AddMentorHelpTicketAsync(openTicket);

            await db.AddMentorHelpMessageAsync(CreateMessage(openTicket.Id, replierId, "mentor reply", ReferenceTime.AddHours(-2)));
            await db.AddMentorHelpMessageAsync(CreateMessage(openTicket.Id, playerId, "player reply", ReferenceTime.AddHours(-1)));

            var statistics = GetStatisticsByMentor(await db.GetMentorHelpStatisticsAsync(null));

            Assert.Multiple(() =>
            {
                Assert.That(statistics.Keys, Is.EquivalentTo(new[] { closerId, replierId }));

                Assert.That(statistics[closerId].TicketsClosed, Is.EqualTo(1));
                Assert.That(statistics[closerId].MessagesCount, Is.EqualTo(0));

                Assert.That(statistics[replierId].TicketsClosed, Is.EqualTo(0));
                Assert.That(statistics[replierId].MessagesCount, Is.EqualTo(1));

                Assert.That(statistics.ContainsKey(playerId), Is.False);
            });

            await pair.CleanReturnAsync();
        }

        [Test]
        public async Task GetMentorHelpStatisticsAsync_FiltersFromInclusively_OnSqlite()
        {
            await using var pair = await PoolManager.GetServerClient();
            var db = GetDb(pair.Server);

            var from = ReferenceTime.AddDays(-7);
            var playerId = Guid.NewGuid();
            var mentorId = Guid.NewGuid();

            var boundaryTicket = CreateTicket(
                playerId,
                mentorId,
                MentorHelpTicketStatus.Closed,
                from.AddDays(-1),
                from,
                from,
                mentorId);

            var oldClosedTicket = CreateTicket(
                playerId,
                mentorId,
                MentorHelpTicketStatus.Closed,
                from.AddDays(-2),
                from.AddTicks(-1),
                from.AddTicks(-1),
                mentorId);

            var messageTicket = CreateTicket(
                playerId,
                mentorId,
                MentorHelpTicketStatus.Assigned,
                from.AddDays(-1),
                ReferenceTime.AddMinutes(-30));

            await db.AddMentorHelpTicketAsync(boundaryTicket);
            await db.AddMentorHelpTicketAsync(oldClosedTicket);
            await db.AddMentorHelpTicketAsync(messageTicket);

            await db.AddMentorHelpMessageAsync(CreateMessage(messageTicket.Id, mentorId, "mentor boundary", from));
            await db.AddMentorHelpMessageAsync(CreateMessage(messageTicket.Id, mentorId, "mentor old", from.AddTicks(-1)));
            await db.AddMentorHelpMessageAsync(CreateMessage(messageTicket.Id, playerId, "player boundary", from));

            var statistics = GetStatisticsByMentor(await db.GetMentorHelpStatisticsAsync(from));

            Assert.Multiple(() =>
            {
                Assert.That(statistics.Keys, Is.EquivalentTo(new[] { mentorId }));
                Assert.That(statistics[mentorId].TicketsClosed, Is.EqualTo(1));
                Assert.That(statistics[mentorId].MessagesCount, Is.EqualTo(1));
            });

            await pair.CleanReturnAsync();
        }

        [Test]
        public async Task GetMentorHelpStatisticsAsync_IgnoresIncompleteClosedTicketMetadata()
        {
            await using var pair = await PoolManager.GetServerClient();
            var db = GetDb(pair.Server);

            var playerId = Guid.NewGuid();
            var validMentorId = Guid.NewGuid();
            var incompleteMentorId = Guid.NewGuid();

            var validClosedTicket = CreateTicket(
                playerId,
                validMentorId,
                MentorHelpTicketStatus.Closed,
                ReferenceTime.AddDays(-3),
                ReferenceTime.AddDays(-2),
                ReferenceTime.AddDays(-2),
                validMentorId);

            var missingClosedAt = CreateTicket(
                playerId,
                incompleteMentorId,
                MentorHelpTicketStatus.Closed,
                ReferenceTime.AddDays(-4),
                ReferenceTime.AddDays(-3),
                null,
                incompleteMentorId);

            var missingClosedBy = CreateTicket(
                playerId,
                incompleteMentorId,
                MentorHelpTicketStatus.Closed,
                ReferenceTime.AddDays(-5),
                ReferenceTime.AddDays(-4),
                ReferenceTime.AddDays(-4),
                null);

            await db.AddMentorHelpTicketAsync(validClosedTicket);
            await db.AddMentorHelpTicketAsync(missingClosedAt);
            await db.AddMentorHelpTicketAsync(missingClosedBy);

            var statistics = GetStatisticsByMentor(await db.GetMentorHelpStatisticsAsync(null));

            Assert.Multiple(() =>
            {
                Assert.That(statistics.Keys, Is.EquivalentTo(new[] { validMentorId }));
                Assert.That(statistics[validMentorId].TicketsClosed, Is.EqualTo(1));
                Assert.That(statistics[validMentorId].MessagesCount, Is.EqualTo(0));
                Assert.That(statistics.ContainsKey(incompleteMentorId), Is.False);
            });

            await pair.CleanReturnAsync();
        }

        [Test]
        public async Task GetMentorHelpTicketsByPlayerAsync_OrdersByCreatedAtDescending()
        {
            await using var pair = await PoolManager.GetServerClient();
            var db = GetDb(pair.Server);

            var playerId = Guid.NewGuid();
            var mentorId = Guid.NewGuid();

            var oldestTicket = CreateTicket(
                playerId,
                null,
                MentorHelpTicketStatus.Open,
                ReferenceTime.AddDays(-4),
                ReferenceTime.AddHours(-1));

            var middleTicket = CreateTicket(
                playerId,
                mentorId,
                MentorHelpTicketStatus.Assigned,
                ReferenceTime.AddDays(-3),
                ReferenceTime.AddHours(-3));

            var newestTicket = CreateTicket(
                playerId,
                mentorId,
                MentorHelpTicketStatus.Closed,
                ReferenceTime.AddDays(-2),
                ReferenceTime.AddDays(-2),
                ReferenceTime.AddDays(-2),
                mentorId);

            await db.AddMentorHelpTicketAsync(oldestTicket);
            await db.AddMentorHelpTicketAsync(middleTicket);
            await db.AddMentorHelpTicketAsync(newestTicket);

            var playerTickets = await db.GetMentorHelpTicketsByPlayerAsync(playerId);

            AssertTicketIds(playerTickets, newestTicket.Id, middleTicket.Id, oldestTicket.Id);

            await pair.CleanReturnAsync();
        }

        [Test]
        public async Task MentorHelpTicketAdminQueries_FilterAndOrderByUpdatedAt()
        {
            await using var pair = await PoolManager.GetServerClient();
            var db = GetDb(pair.Server);

            var playerId = Guid.NewGuid();
            var mentorId = Guid.NewGuid();
            var otherMentorId = Guid.NewGuid();

            var openOld = CreateTicket(
                playerId,
                null,
                MentorHelpTicketStatus.Open,
                ReferenceTime.AddDays(-5),
                ReferenceTime.AddDays(-5));

            var assignedNewest = CreateTicket(
                playerId,
                mentorId,
                MentorHelpTicketStatus.Assigned,
                ReferenceTime.AddDays(-4),
                ReferenceTime.AddHours(-1));

            var awaitingMiddle = CreateTicket(
                playerId,
                mentorId,
                MentorHelpTicketStatus.AwaitingResponse,
                ReferenceTime.AddDays(-3),
                ReferenceTime.AddHours(-3));

            var assignedOtherMentor = CreateTicket(
                playerId,
                otherMentorId,
                MentorHelpTicketStatus.Assigned,
                ReferenceTime.AddDays(-2),
                ReferenceTime.AddHours(-2));

            var closedNewest = CreateTicket(
                playerId,
                mentorId,
                MentorHelpTicketStatus.Closed,
                ReferenceTime.AddDays(-2),
                ReferenceTime.AddHours(-4),
                ReferenceTime.AddHours(-4),
                mentorId);

            var closedOldest = CreateTicket(
                playerId,
                mentorId,
                MentorHelpTicketStatus.Closed,
                ReferenceTime.AddDays(-6),
                ReferenceTime.AddDays(-1),
                ReferenceTime.AddDays(-1),
                mentorId);

            await db.AddMentorHelpTicketAsync(openOld);
            await db.AddMentorHelpTicketAsync(assignedNewest);
            await db.AddMentorHelpTicketAsync(awaitingMiddle);
            await db.AddMentorHelpTicketAsync(assignedOtherMentor);
            await db.AddMentorHelpTicketAsync(closedNewest);
            await db.AddMentorHelpTicketAsync(closedOldest);

            var openTickets = await db.GetOpenMentorHelpTicketsAsync();
            var assignedTickets = await db.GetAssignedMentorHelpTicketsAsync(mentorId);
            var closedTickets = await db.GetClosedMentorHelpTicketsAsync();

            Assert.Multiple(() =>
            {
                AssertTicketIds(openTickets, assignedNewest.Id, assignedOtherMentor.Id, awaitingMiddle.Id, openOld.Id);
                AssertTicketIds(assignedTickets, assignedNewest.Id, awaitingMiddle.Id);
                AssertTicketIds(closedTickets, closedNewest.Id, closedOldest.Id);
            });

            await pair.CleanReturnAsync();
        }
    }
}

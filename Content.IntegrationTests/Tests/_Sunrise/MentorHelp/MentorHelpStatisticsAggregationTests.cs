using Content.Server.Database;
using Content.Shared.Database;

namespace Content.IntegrationTests.Tests._Sunrise.MentorHelp;

[TestFixture]
[TestOf(typeof(ServerDbSqlite))]
public sealed class MentorHelpStatisticsAggregationTests : MentorHelpStatisticsTestBase
{
    [Test]
    public async Task CountsAssignedMentorForPlayerSelfClosedTicketsAndExcludesPlayerReplies()
    {
        await using var pair = await PoolManager.GetServerClient();
        var db = GetDb(pair.Server);

        var playerId = Guid.NewGuid();
        var assignedMentorId = Guid.NewGuid();
        var replyingMentorId = Guid.NewGuid();

        var closedTicket = CreateTicket(
            playerId,
            assignedMentorId,
            MentorHelpTicketStatus.Closed,
            ReferenceTime.AddDays(-3),
            ReferenceTime.AddDays(-1),
            ReferenceTime.AddDays(-1),
            playerId);

        var openTicket = CreateTicket(
            playerId,
            replyingMentorId,
            MentorHelpTicketStatus.Assigned,
            ReferenceTime.AddDays(-2),
            ReferenceTime.AddHours(-2));

        await db.AddMentorHelpTicketAsync(closedTicket);
        await db.AddMentorHelpTicketAsync(openTicket);

        await db.AddMentorHelpMessageAsync(CreateMessage(openTicket.Id, replyingMentorId, "mentor reply", ReferenceTime.AddHours(-2)));
        await db.AddMentorHelpMessageAsync(CreateMessage(openTicket.Id, playerId, "player reply", ReferenceTime.AddHours(-1)));

        var statistics = GetStatisticsByMentor(await db.GetMentorHelpStatisticsAsync(null));

        Assert.Multiple(() =>
        {
            Assert.That(statistics.Keys, Is.EquivalentTo(new[] { assignedMentorId, replyingMentorId }));
            Assert.That(statistics[assignedMentorId].TicketsClosed, Is.EqualTo(1));
            Assert.That(statistics[assignedMentorId].MessagesCount, Is.EqualTo(0));
            Assert.That(statistics[replyingMentorId].TicketsClosed, Is.EqualTo(0));
            Assert.That(statistics[replyingMentorId].MessagesCount, Is.EqualTo(1));
            Assert.That(statistics.ContainsKey(playerId), Is.False);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task SkipsClosedTicketsWithoutAssignedMentor()
    {
        await using var pair = await PoolManager.GetServerClient();
        var db = GetDb(pair.Server);

        var playerId = Guid.NewGuid();
        var assignedMentorId = Guid.NewGuid();

        var countedTicket = CreateTicket(
            playerId,
            assignedMentorId,
            MentorHelpTicketStatus.Closed,
            ReferenceTime.AddDays(-3),
            ReferenceTime.AddDays(-2),
            ReferenceTime.AddDays(-2),
            assignedMentorId);

        var unassignedClosedTicket = CreateTicket(
            playerId,
            null,
            MentorHelpTicketStatus.Closed,
            ReferenceTime.AddDays(-2),
            ReferenceTime.AddDays(-1),
            ReferenceTime.AddDays(-1),
            playerId);

        await db.AddMentorHelpTicketAsync(countedTicket);
        await db.AddMentorHelpTicketAsync(unassignedClosedTicket);

        var statistics = GetStatisticsByMentor(await db.GetMentorHelpStatisticsAsync(null));

        Assert.Multiple(() =>
        {
            Assert.That(statistics.Keys, Is.EquivalentTo(new[] { assignedMentorId }));
            Assert.That(statistics[assignedMentorId].TicketsClosed, Is.EqualTo(1));
            Assert.That(statistics.ContainsKey(playerId), Is.False);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task SkipsClosedTicketsWithoutClosedAt()
    {
        await using var pair = await PoolManager.GetServerClient();
        var db = GetDb(pair.Server);

        var playerId = Guid.NewGuid();
        var mentorId = Guid.NewGuid();

        var countedTicket = CreateTicket(
            playerId,
            mentorId,
            MentorHelpTicketStatus.Closed,
            ReferenceTime.AddDays(-3),
            ReferenceTime.AddDays(-2),
            ReferenceTime.AddDays(-2),
            mentorId);

        var missingClosedAt = CreateTicket(
            playerId,
            mentorId,
            MentorHelpTicketStatus.Closed,
            ReferenceTime.AddDays(-4),
            ReferenceTime.AddDays(-3),
            null,
            mentorId);

        await db.AddMentorHelpTicketAsync(countedTicket);
        await db.AddMentorHelpTicketAsync(missingClosedAt);

        var statistics = GetStatisticsByMentor(await db.GetMentorHelpStatisticsAsync(null));

        Assert.Multiple(() =>
        {
            Assert.That(statistics.Keys, Is.EquivalentTo(new[] { mentorId }));
            Assert.That(statistics[mentorId].TicketsClosed, Is.EqualTo(1));
            Assert.That(statistics[mentorId].MessagesCount, Is.EqualTo(0));
        });

        await pair.CleanReturnAsync();
    }
}

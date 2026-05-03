using Content.Server.Database;
using Content.Shared.Database;

namespace Content.IntegrationTests.Tests._Sunrise.MentorHelp;

[TestFixture]
[TestOf(typeof(ServerDbSqlite))]
public sealed class MentorHelpStatisticsAggregationTests : MentorHelpStatisticsTestBase
{
    [Test]
    public async Task CountsHandledTicketsForAssignedMentorsWhoReplied()
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

        await db.AddMentorHelpMessageAsync(CreateMessage(closedTicket.Id, assignedMentorId, "assigned mentor reply", ReferenceTime.AddDays(-1)));
        await db.AddMentorHelpMessageAsync(CreateMessage(openTicket.Id, replyingMentorId, "mentor reply", ReferenceTime.AddHours(-2)));
        await db.AddMentorHelpMessageAsync(CreateMessage(openTicket.Id, playerId, "player reply", ReferenceTime.AddHours(-1)));

        var statistics = GetStatisticsByMentor(await db.GetMentorHelpStatisticsAsync(null));

        Assert.Multiple(() =>
        {
            Assert.That(statistics.Keys, Is.EquivalentTo(new[] { assignedMentorId, replyingMentorId }));
            Assert.That(statistics[assignedMentorId].TicketsClosed, Is.EqualTo(1));
            Assert.That(statistics[assignedMentorId].MessagesCount, Is.EqualTo(1));
            Assert.That(statistics[replyingMentorId].TicketsClosed, Is.EqualTo(1));
            Assert.That(statistics[replyingMentorId].MessagesCount, Is.EqualTo(1));
            Assert.That(statistics.ContainsKey(playerId), Is.False);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task SkipsTicketsWithoutAssignedMentorReply()
    {
        await using var pair = await PoolManager.GetServerClient();
        var db = GetDb(pair.Server);

        var playerId = Guid.NewGuid();
        var mentorId = Guid.NewGuid();

        var assignedWithoutReply = CreateTicket(
            playerId,
            mentorId,
            MentorHelpTicketStatus.Closed,
            ReferenceTime.AddDays(-3),
            ReferenceTime.AddDays(-2),
            ReferenceTime.AddDays(-2),
            mentorId);

        var unassignedClosedTicket = CreateTicket(
            playerId,
            null,
            MentorHelpTicketStatus.Closed,
            ReferenceTime.AddDays(-2),
            ReferenceTime.AddDays(-1),
            ReferenceTime.AddDays(-1),
            playerId);

        await db.AddMentorHelpTicketAsync(assignedWithoutReply);
        await db.AddMentorHelpTicketAsync(unassignedClosedTicket);

        var statistics = GetStatisticsByMentor(await db.GetMentorHelpStatisticsAsync(null));

        Assert.Multiple(() =>
        {
            Assert.That(statistics.Keys, Is.Empty);
            Assert.That(statistics.ContainsKey(playerId), Is.False);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task CountsMessagesButNotTicketForNonAssignedMentorReplies()
    {
        await using var pair = await PoolManager.GetServerClient();
        var db = GetDb(pair.Server);

        var playerId = Guid.NewGuid();
        var assignedMentorId = Guid.NewGuid();
        var otherMentorId = Guid.NewGuid();

        var countedTicket = CreateTicket(
            playerId,
            assignedMentorId,
            MentorHelpTicketStatus.Assigned,
            ReferenceTime.AddDays(-3),
            ReferenceTime.AddDays(-2),
            null,
            null);

        var reassignedByOtherReply = CreateTicket(
            playerId,
            assignedMentorId,
            MentorHelpTicketStatus.Assigned,
            ReferenceTime.AddDays(-4),
            ReferenceTime.AddDays(-1),
            null,
            null);

        await db.AddMentorHelpTicketAsync(countedTicket);
        await db.AddMentorHelpTicketAsync(reassignedByOtherReply);
        await db.AddMentorHelpMessageAsync(CreateMessage(countedTicket.Id, assignedMentorId, "assigned mentor reply", ReferenceTime.AddDays(-2)));
        await db.AddMentorHelpMessageAsync(CreateMessage(reassignedByOtherReply.Id, otherMentorId, "other mentor reply", ReferenceTime.AddDays(-1)));

        var statistics = GetStatisticsByMentor(await db.GetMentorHelpStatisticsAsync(null));

        Assert.Multiple(() =>
        {
            Assert.That(statistics.Keys, Is.EquivalentTo(new[] { assignedMentorId, otherMentorId }));
            Assert.That(statistics[assignedMentorId].TicketsClosed, Is.EqualTo(1));
            Assert.That(statistics[assignedMentorId].MessagesCount, Is.EqualTo(1));
            Assert.That(statistics[otherMentorId].TicketsClosed, Is.EqualTo(0));
            Assert.That(statistics[otherMentorId].MessagesCount, Is.EqualTo(1));
        });

        await pair.CleanReturnAsync();
    }
}

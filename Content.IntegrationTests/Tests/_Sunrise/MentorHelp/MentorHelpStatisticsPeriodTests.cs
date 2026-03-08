using Content.Server.Database;
using Content.Shared.Database;

namespace Content.IntegrationTests.Tests._Sunrise.MentorHelp;

[TestFixture]
[TestOf(typeof(ServerDbSqlite))]
public sealed class MentorHelpStatisticsPeriodTests : MentorHelpStatisticsTestBase
{
    [Test]
    public async Task FiltersPeriodInclusivelyOnSqlite()
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
}

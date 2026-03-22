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
            MentorHelpTicketStatus.Assigned,
            from.AddDays(-1),
            ReferenceTime.AddMinutes(-10));

        var oldHandledTicket = CreateTicket(
            playerId,
            mentorId,
            MentorHelpTicketStatus.Assigned,
            from.AddDays(-2),
            ReferenceTime.AddMinutes(-20));

        var playerOnlyMessageTicket = CreateTicket(
            playerId,
            mentorId,
            MentorHelpTicketStatus.Assigned,
            from.AddDays(-1),
            ReferenceTime.AddMinutes(-30));

        await db.AddMentorHelpTicketAsync(boundaryTicket);
        await db.AddMentorHelpTicketAsync(oldHandledTicket);
        await db.AddMentorHelpTicketAsync(playerOnlyMessageTicket);

        await db.AddMentorHelpMessageAsync(CreateMessage(boundaryTicket.Id, mentorId, "mentor boundary", from));
        await db.AddMentorHelpMessageAsync(CreateMessage(oldHandledTicket.Id, mentorId, "mentor old", from.AddTicks(-1)));
        await db.AddMentorHelpMessageAsync(CreateMessage(playerOnlyMessageTicket.Id, playerId, "player boundary", from));

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

using Content.Server.Database;
using Content.Shared.Database;

namespace Content.IntegrationTests.Tests._Sunrise.MentorHelp;

[TestFixture]
[TestOf(typeof(ServerDbSqlite))]
public sealed class MentorHelpTicketQueryTests : MentorHelpStatisticsTestBase
{
    [Test]
    public async Task OrdersByCreatedAtDesc()
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
    public async Task FiltersAdminQueriesByUpdatedAt()
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

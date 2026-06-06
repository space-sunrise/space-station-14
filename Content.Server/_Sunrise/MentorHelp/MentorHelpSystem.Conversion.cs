using System.Linq;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared._Sunrise.MentorHelp;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared.Administration;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Server._Sunrise.MentorHelp;

public sealed partial class MentorHelpSystem
{
    private async Task<List<MentorHelpTicketData>> GetTicketDataListAsync(List<MentorHelpTicket> tickets)
    {
        var userIds = new HashSet<Guid>();

        foreach (var ticket in tickets)
        {
            userIds.Add(ticket.PlayerId);

            if (ticket.AssignedToUserId is { } assignedToUserId)
                userIds.Add(assignedToUserId);

            if (ticket.ClosedByUserId is { } closedByUserId)
                userIds.Add(closedByUserId);
        }

        var playerNames = await _dbManager.GetPlayerNamesBatchAsync(userIds);
        var ticketDataList = new List<MentorHelpTicketData>(tickets.Count);

        foreach (var ticket in tickets)
            ticketDataList.Add(ConvertToTicketData(ticket, playerNames));


        return ticketDataList;
    }

    private static MentorHelpTicketData CreateTicketData(
        MentorHelpTicket ticket,
        string playerName,
        string? assignedToName,
        string? closedByName)
    {
        return new MentorHelpTicketData
        {
            Id = ticket.Id,
            PlayerId = new NetUserId(ticket.PlayerId),
            PlayerName = playerName,
            AssignedToUserId = ToNetUserId(ticket.AssignedToUserId),
            AssignedToName = assignedToName,
            Subject = ticket.Subject,
            Status = ticket.Status,
            CreatedAt = ticket.CreatedAt.DateTime,
            UpdatedAt = ticket.UpdatedAt.DateTime,
            ClosedAt = ticket.ClosedAt?.DateTime,
            ClosedByUserId = ToNetUserId(ticket.ClosedByUserId),
            ClosedByName = closedByName,
            RoundId = ticket.RoundId,
            HasUnreadMessages = false
        };
    }

    private async Task<List<MentorHelpMessageData>> GetTicketMessagesDataAsync(int ticketId, bool includeStaffOnly)
    {
        var messages = await GetOrderedTicketMessagesAsync(ticketId);
        return await ConvertToMessageDataListAsync(messages, includeStaffOnly);
    }

    private async Task<List<MentorHelpMessageData>> ConvertToMessageDataListAsync(
        IEnumerable<MentorHelpMessage> messages,
        bool includeStaffOnly)
    {
        var messageDatas = new List<MentorHelpMessageData>();

        foreach (var message in messages)
        {
            if (!includeStaffOnly && message.IsStaffOnly)
                continue;

            messageDatas.Add(await ConvertToMessageDataAsync(message));
        }

        return messageDatas;
    }

    private async Task<List<MentorHelpMessage>> GetOrderedTicketMessagesAsync(int ticketId)
    {
        var messages = await _dbManager.GetMentorHelpMessagesByTicketAsync(ticketId);
        return [.. messages.OrderBy(message => message.SentAt)];
    }

    private async Task<(string Username, AdminData? AdminData)> ResolveMessageSenderContextAsync(NetUserId senderUserId)
    {
        AdminData? senderAdminData = null;
        string? username = null;

        if (_playerManager.TryGetSessionById(senderUserId, out var senderSession))
        {
            senderAdminData = _adminManager.GetAdminData(senderSession);
            username = senderSession.Name;
        }
        else
        {
            var loadedAdminData = await _adminManager.LoadAdminData(senderUserId);
            if (loadedAdminData is not null)
                senderAdminData = loadedAdminData.Value.dat;
        }

        username ??= await GetStoredPlayerNameAsync(senderUserId);
        username ??= Loc.GetString("mentor-help-unknown-user");

        return (username, senderAdminData);
    }

    private string FormatMessageSender(string username, NetUserId senderUserId, AdminData? senderAdminData)
    {
        var adminPrefix = string.Empty;
        var escapedUsername = FormattedMessage.EscapeText(username);

        if (_config.GetCVar(SunriseCCVars.MentorHelpAdminPrefixEnabled) && senderAdminData?.Title is { } title)
            adminPrefix = $"[bold]\\[{FormattedMessage.EscapeText(title)}\\][/bold] ";

        if (senderAdminData != null && senderAdminData.HasFlag(AdminFlags.Mentor) && senderAdminData.Flags == AdminFlags.Mentor)
            return $"[color=purple]{adminPrefix}{escapedUsername}[/color]";

        if (senderAdminData != null && senderAdminData.HasFlag(AdminFlags.Mentor))
            return $"[color=red]{adminPrefix}{escapedUsername}[/color]";

        if (_sponsorsManager == null)
            return escapedUsername;

        _sponsorsManager.TryGetOocColor(senderUserId, out var oocColor);
        _sponsorsManager.TryGetOocTitle(senderUserId, out var oocTitle);

        var sponsorTitle = oocTitle is null ? string.Empty : $"\\[{FormattedMessage.EscapeText(oocTitle)}\\]";
        var sponsorPrefix = sponsorTitle == string.Empty ? string.Empty : $"{sponsorTitle} ";

        return oocColor != null
            ? $"[color={oocColor.Value.ToHex()}]{sponsorPrefix}{escapedUsername}[/color]"
            : $"{sponsorPrefix}{escapedUsername}";
    }

    private async Task<string?> GetOptionalPlayerNameAsync(Guid? userId)
    {
        if (userId is not { } value)
            return null;

        return await GetPlayerNameAsync(value);
    }

    private async Task<string?> GetStoredPlayerNameAsync(NetUserId userId)
    {
        var playerData = await _dbManager.GetPlayerRecordByUserId(userId);
        return string.IsNullOrWhiteSpace(playerData?.LastSeenUserName)
            ? null
            : playerData.LastSeenUserName;
    }

    private static string? TryGetPlayerName(IReadOnlyDictionary<Guid, string> playerNames, Guid? userId)
    {
        return userId is { } value && playerNames.TryGetValue(value, out var playerName)
            ? playerName
            : null;
    }

    private static NetUserId? ToNetUserId(Guid? userId)
    {
        return userId is { } value
            ? new NetUserId(value)
            : null;
    }
}

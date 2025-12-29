using Content.Shared.Database;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.MentorHelp
{
    /// <summary>
    /// Shared base class for mentor help system
    /// </summary>
    public abstract class SharedMentorHelpSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<MentorHelpCreateTicketMessage>(OnCreateTicketMessage);
            SubscribeNetworkEvent<MentorHelpClaimTicketMessage>(OnClaimTicketMessage);
            SubscribeNetworkEvent<MentorHelpReplyMessage>(OnReplyMessage);
            SubscribeNetworkEvent<MentorHelpCloseTicketMessage>(OnCloseTicketMessage);
            SubscribeNetworkEvent<MentorHelpRequestTicketsMessage>(OnRequestTicketsMessage);
            SubscribeNetworkEvent<MentorHelpUnassignTicketMessage>(OnUnassignTicketMessage);
            SubscribeNetworkEvent<MentorHelpRequestStatisticsMessage>(OnRequestStatisticsMessage);
            SubscribeNetworkEvent<MentorHelpRequestTicketMessagesMessage>(OnRequestTicketMessagesMessage);
        }

        protected virtual void OnCreateTicketMessage(MentorHelpCreateTicketMessage message, EntitySessionEventArgs eventArgs) { }
        protected virtual void OnClaimTicketMessage(MentorHelpClaimTicketMessage message, EntitySessionEventArgs eventArgs) { }
        protected virtual void OnReplyMessage(MentorHelpReplyMessage message, EntitySessionEventArgs eventArgs) { }
        protected virtual void OnCloseTicketMessage(MentorHelpCloseTicketMessage message, EntitySessionEventArgs eventArgs) { }
        protected virtual void OnRequestTicketsMessage(MentorHelpRequestTicketsMessage message, EntitySessionEventArgs eventArgs) { }
        protected virtual void OnUnassignTicketMessage(MentorHelpUnassignTicketMessage message, EntitySessionEventArgs eventArgs) { }
        protected virtual void OnRequestStatisticsMessage(MentorHelpRequestStatisticsMessage message, EntitySessionEventArgs eventArgs) { }
        protected virtual void OnRequestTicketMessagesMessage(MentorHelpRequestTicketMessagesMessage message, EntitySessionEventArgs eventArgs) { }
    }

    public struct MentorHelpStatistics
    {
        public Guid MentorUserId { get; set; }
        public int TicketsClaimed { get; set; }
        public int MessagesCount { get; set; }
    }

    /// <summary>
    /// Message to create a new mentor help ticket
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class MentorHelpCreateTicketMessage(string subject, string message) : EntityEventArgs
    {
        public readonly string Subject = subject;
        public readonly string Message = message;
    }

    /// <summary>
    /// Message to claim a mentor help ticket
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class MentorHelpClaimTicketMessage(int ticketId) : EntityEventArgs
    {
        public readonly int TicketId = ticketId;
    }

    /// <summary>
    /// Message to reply to a mentor help ticket
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class MentorHelpReplyMessage(int ticketId, string message, bool isStaffOnly = false) : EntityEventArgs
    {
        public readonly int TicketId = ticketId;
        public readonly string Message = message;
        public readonly bool IsStaffOnly = isStaffOnly;
    }

    /// <summary>
    /// Message to unassign a mentor help ticket
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class MentorHelpUnassignTicketMessage(int ticketId) : EntityEventArgs
    {
        public readonly int TicketId = ticketId;
    }

    /// <summary>
    /// Message to close a mentor help ticket
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class MentorHelpCloseTicketMessage(int ticketId) : EntityEventArgs
    {
        public readonly int TicketId = ticketId;
    }

    /// <summary>
    /// Message to request tickets (from client)
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class MentorHelpRequestTicketsMessage(bool onlyMine = false) : EntityEventArgs
    {
        public readonly bool OnlyMine = onlyMine;
    }

    /// <summary>
    /// Message with ticket update (to client)
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class MentorHelpTicketUpdateMessage(MentorHelpTicketData ticket) : EntityEventArgs
    {
        public readonly MentorHelpTicketData Ticket = ticket;
    }

    /// <summary>
    /// Message with tickets list (to client)
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class MentorHelpTicketsListMessage(List<MentorHelpTicketData> tickets) : EntityEventArgs
    {
        public readonly List<MentorHelpTicketData> Tickets = tickets;
    }

    /// <summary>
    /// Message with ticket messages (to client)
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class MentorHelpTicketMessagesMessage(int ticketId, List<MentorHelpMessageData> messages) : EntityEventArgs
    {
        public readonly int TicketId = ticketId;
        public readonly List<MentorHelpMessageData> Messages = messages;
    }

    /// <summary>
    /// Serializable ticket data for networking
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class MentorHelpTicketData
    {
        public int Id { get; set; }
        public NetUserId PlayerId { get; set; }
        public string PlayerName { get; set; } = string.Empty;
        public NetUserId? AssignedToUserId { get; set; }
        public string? AssignedToName { get; set; }
        public string Subject { get; set; } = string.Empty;
        public MentorHelpTicketStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public NetUserId? ClosedByUserId { get; set; }
        public string? ClosedByName { get; set; }
        public int? RoundId { get; set; }
        public bool HasUnreadMessages { get; set; }
    }

    /// <summary>
    /// Serializable message data for networking
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class MentorHelpMessageData
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public NetUserId SenderUserId { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string FormattedSender { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public bool IsStaffOnly { get; set; }
    }

    /// <summary>
    /// DTO для передачи статистики по менторам
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class MentorHelpStatisticsData
    {
        public string MentorName { get; set; } = string.Empty;
        public int TicketsClaimed { get; set; }
        public int MessagesCount { get; set; }
    }

    /// <summary>
    /// Сообщение-запрос статистики по менторам
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class MentorHelpRequestStatisticsMessage : EntityEventArgs
    {
        // Пустой класс-запрос
    }

    /// <summary>
    /// Сообщение с результатами статистики по менторам
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class MentorHelpStatisticsMessage(List<MentorHelpStatisticsData> statistics) : EntityEventArgs
    {
        public readonly List<MentorHelpStatisticsData> Statistics = statistics;
    }

    /// <summary>
    /// Message to request messages for a specific ticket (from client)
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class MentorHelpRequestTicketMessagesMessage(int ticketId) : EntityEventArgs
    {
        public readonly int TicketId = ticketId;
    }

    /// <summary>
    /// Message sent from server to client instructing it to open (focus) a specific ticket in the UI.
    /// This is used immediately after creating a ticket so the creating player sees their new ticket.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class MentorHelpOpenTicketMessage(int ticketId) : EntityEventArgs
    {
        public readonly int TicketId = ticketId;
    }

    [Serializable, NetSerializable]
    public sealed class MentorHelpClientTypingUpdated(int ticketId, bool typing) : EntityEventArgs
    {
        public readonly int TicketId = ticketId;
        public readonly bool Typing = typing;
    }

    [Serializable, NetSerializable]
    public sealed class MentorHelpPlayerTypingUpdated(int ticketId, NetUserId userId, string playerName, bool typing) : EntityEventArgs
    {
        public readonly int TicketId = ticketId;
        public readonly NetUserId UserId = userId;
        public readonly string PlayerName = playerName;
        public readonly bool Typing = typing;
    }
}

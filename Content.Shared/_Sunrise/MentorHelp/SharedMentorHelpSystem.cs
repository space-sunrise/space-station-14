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
    public sealed class MentorHelpCreateTicketMessage : EntityEventArgs
    {
        public string Subject { get; }
        public string Message { get; }

        public MentorHelpCreateTicketMessage(string subject, string message)
        {
            Subject = subject;
            Message = message;
        }
    }

    /// <summary>
    /// Message to claim a mentor help ticket
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class MentorHelpClaimTicketMessage : EntityEventArgs
    {
        public int TicketId { get; }

        public MentorHelpClaimTicketMessage(int ticketId)
        {
            TicketId = ticketId;
        }
    }

    /// <summary>
    /// Message to reply to a mentor help ticket
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class MentorHelpReplyMessage : EntityEventArgs
    {
        public int TicketId { get; }
        public string Message { get; }
        public bool IsStaffOnly { get; }

        public MentorHelpReplyMessage(int ticketId, string message, bool isStaffOnly = false)
        {
            TicketId = ticketId;
            Message = message;
            IsStaffOnly = isStaffOnly;
        }
    }

    /// <summary>
    /// Message to unassign a mentor help ticket
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class MentorHelpUnassignTicketMessage : EntityEventArgs
    {
        public int TicketId { get; }

        public MentorHelpUnassignTicketMessage(int ticketId)
        {
            TicketId = ticketId;
        }
    }

    /// <summary>
    /// Message to close a mentor help ticket
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class MentorHelpCloseTicketMessage : EntityEventArgs
    {
        public int TicketId { get; }

        public MentorHelpCloseTicketMessage(int ticketId)
        {
            TicketId = ticketId;
        }
    }

    /// <summary>
    /// Message to request tickets (from client)
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class MentorHelpRequestTicketsMessage : EntityEventArgs
    {
        public bool OnlyMine { get; }

        public MentorHelpRequestTicketsMessage(bool onlyMine = false)
        {
            OnlyMine = onlyMine;
        }
    }

    /// <summary>
    /// Message with ticket update (to client)
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class MentorHelpTicketUpdateMessage : EntityEventArgs
    {
        public MentorHelpTicketData Ticket { get; }

        public MentorHelpTicketUpdateMessage(MentorHelpTicketData ticket)
        {
            Ticket = ticket;
        }
    }

    /// <summary>
    /// Message with tickets list (to client)
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class MentorHelpTicketsListMessage : EntityEventArgs
    {
        public List<MentorHelpTicketData> Tickets { get; }

        public MentorHelpTicketsListMessage(List<MentorHelpTicketData> tickets)
        {
            Tickets = tickets;
        }
    }

    /// <summary>
    /// Message with ticket messages (to client)
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class MentorHelpTicketMessagesMessage : EntityEventArgs
    {
        public int TicketId { get; }
        public List<MentorHelpMessageData> Messages { get; }

        public MentorHelpTicketMessagesMessage(int ticketId, List<MentorHelpMessageData> messages)
        {
            TicketId = ticketId;
            Messages = messages;
        }
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
    public sealed class MentorHelpStatisticsMessage : EntityEventArgs
    {
        public List<MentorHelpStatisticsData> Statistics { get; }
        public MentorHelpStatisticsMessage(List<MentorHelpStatisticsData> statistics)
        {
            Statistics = statistics;
        }
    }

    /// <summary>
    /// Message to request messages for a specific ticket (from client)
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class MentorHelpRequestTicketMessagesMessage : EntityEventArgs
    {
        public int TicketId { get; }

        public MentorHelpRequestTicketMessagesMessage(int ticketId)
        {
            TicketId = ticketId;
        }
    }

    /// <summary>
    /// Message sent from server to client instructing it to open (focus) a specific ticket in the UI.
    /// This is used immediately after creating a ticket so the creating player sees their new ticket.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class MentorHelpOpenTicketMessage : EntityEventArgs
    {
        public int TicketId { get; }

        public MentorHelpOpenTicketMessage(int ticketId)
        {
            TicketId = ticketId;
        }
    }
}

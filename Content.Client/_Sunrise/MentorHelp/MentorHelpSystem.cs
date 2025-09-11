using Content.Shared._Sunrise.MentorHelp;
using JetBrains.Annotations;

namespace Content.Client._Sunrise.MentorHelp
{
    /// <summary>
    /// Client-side mentor help system
    /// </summary>
    [UsedImplicitly]
    public sealed class MentorHelpSystem : SharedMentorHelpSystem
    {
        public event EventHandler<MentorHelpTicketUpdateMessage>? OnTicketUpdated;
        public event EventHandler<MentorHelpTicketsListMessage>? OnTicketsListReceived;
        public event EventHandler<MentorHelpTicketMessagesMessage>? OnTicketMessagesReceived;
        public event EventHandler<MentorHelpStatisticsMessage>? OnStatisticsReceived;
        public event EventHandler<MentorHelpOpenTicketMessage>? OnOpenTicketReceived;

        protected override void OnCreateTicketMessage(MentorHelpCreateTicketMessage message, EntitySessionEventArgs eventArgs)
        {
            // Client doesn't handle this directly
        }

        protected override void OnClaimTicketMessage(MentorHelpClaimTicketMessage message, EntitySessionEventArgs eventArgs)
        {
            // Client doesn't handle this directly
        }

        protected override void OnReplyMessage(MentorHelpReplyMessage message, EntitySessionEventArgs eventArgs)
        {
            // Client doesn't handle this directly
        }

        protected override void OnCloseTicketMessage(MentorHelpCloseTicketMessage message, EntitySessionEventArgs eventArgs)
        {
            // Client doesn't handle this directly
        }

        protected override void OnRequestTicketsMessage(MentorHelpRequestTicketsMessage message, EntitySessionEventArgs eventArgs)
        {
            // Client doesn't handle this directly
        }

        protected override void OnUnassignTicketMessage(MentorHelpUnassignTicketMessage message, EntitySessionEventArgs eventArgs)
        {
            // Client doesn't handle this directly
        }

        protected override void OnRequestStatisticsMessage(MentorHelpRequestStatisticsMessage message, EntitySessionEventArgs eventArgs)
        {
            // Client doesn't handle this directly
        }

        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<MentorHelpTicketUpdateMessage>(OnTicketUpdate);
            SubscribeNetworkEvent<MentorHelpTicketsListMessage>(OnTicketsList);
            SubscribeNetworkEvent<MentorHelpTicketMessagesMessage>(OnTicketMessages);
            SubscribeNetworkEvent<MentorHelpStatisticsMessage>(OnStatistics);
            SubscribeNetworkEvent<MentorHelpOpenTicketMessage>(OnOpenTicket);
        }

        private void OnOpenTicket(MentorHelpOpenTicketMessage message, EntitySessionEventArgs eventArgs)
        {
            OnOpenTicketReceived?.Invoke(this, message);
        }

        private void OnTicketUpdate(MentorHelpTicketUpdateMessage message, EntitySessionEventArgs eventArgs)
        {
            OnTicketUpdated?.Invoke(this, message);
        }

        private void OnTicketsList(MentorHelpTicketsListMessage message, EntitySessionEventArgs eventArgs)
        {
            OnTicketsListReceived?.Invoke(this, message);
        }

        private void OnTicketMessages(MentorHelpTicketMessagesMessage message, EntitySessionEventArgs eventArgs)
        {
            OnTicketMessagesReceived?.Invoke(this, message);
        }

        /// <summary>
        /// Create a new mentor help ticket
        /// </summary>
        private void OnStatistics(MentorHelpStatisticsMessage message, EntitySessionEventArgs eventArgs)
        {
            OnStatisticsReceived?.Invoke(this, message);
        }

        public void CreateTicket(string subject, string message)
        {
            RaiseNetworkEvent(new MentorHelpCreateTicketMessage(subject, message));
        }

        /// <summary>
        /// Claim a mentor help ticket
        /// </summary>
        public void ClaimTicket(int ticketId)
        {
            RaiseNetworkEvent(new MentorHelpClaimTicketMessage(ticketId));
        }

        /// <summary>
        /// Unassign a mentor help ticket
        /// </summary>
        public void UnassignTicket(int ticketId)
        {
            RaiseNetworkEvent(new MentorHelpUnassignTicketMessage(ticketId));
        }

        /// <summary>
        /// Reply to a mentor help ticket
        /// </summary>
        public void ReplyToTicket(int ticketId, string message, bool isStaffOnly = false)
        {
            RaiseNetworkEvent(new MentorHelpReplyMessage(ticketId, message, isStaffOnly));
        }

        /// <summary>
        /// Close a mentor help ticket
        /// </summary>
        public void CloseTicket(int ticketId)
        {
            RaiseNetworkEvent(new MentorHelpCloseTicketMessage(ticketId));
        }

        /// <summary>
        /// Request tickets (either all for mentors/admins, or only own for players)
        /// </summary>
        public void RequestTickets(bool onlyMine = false)
        {
            RaiseNetworkEvent(new MentorHelpRequestTicketsMessage(onlyMine));
        }

        /// <summary>
        /// Request messages for a specific ticket
        /// </summary>
        public void RequestTicketMessages(int ticketId)
        {
            // Send a request to the server to fetch messages for the given ticket
            RaiseNetworkEvent(new MentorHelpRequestTicketMessagesMessage(ticketId));
        }

        /// <summary>
        /// Request mentor help statistics
        /// </summary>
        public void RequestStatistics()
        {
            RaiseNetworkEvent(new MentorHelpRequestStatisticsMessage());
        }
    }
}

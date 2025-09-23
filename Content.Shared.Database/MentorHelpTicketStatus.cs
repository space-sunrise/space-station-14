namespace Content.Shared.Database
{
    /// <summary>
    /// Status values for mentor help tickets
    /// </summary>
    public enum MentorHelpTicketStatus
    {
        /// <summary>
        /// Ticket is open and awaiting assignment or response
        /// </summary>
        Open = 0,
        
        /// <summary>
        /// Ticket has been claimed by a mentor/admin
        /// </summary>
        Assigned = 1,
        
        /// <summary>
        /// Ticket has been responded to, awaiting player response
        /// </summary>
        AwaitingResponse = 2,
        
        /// <summary>
        /// Ticket has been resolved/closed
        /// </summary>
        Closed = 3
    }
}
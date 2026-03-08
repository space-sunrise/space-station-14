using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.Administration.Managers;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Server.Players.RateLimiting;
using Content.Shared._Sunrise.MentorHelp;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Players.RateLimiting;
using Content.Sunrise.Interfaces.Shared;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._Sunrise.MentorHelp
{
    /// <summary>
    /// Server-side mentor help system for managing tickets
    /// </summary>
    [UsedImplicitly]
    public sealed class MentorHelpSystem : SharedMentorHelpSystem
    {
        private const string RateLimitKey = "MentorHelp";

        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IAdminManager _adminManager = default!;
        [Dependency] private readonly IConfigurationManager _config = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly GameTicker _gameTicker = default!;
        [Dependency] private readonly IServerDbManager _dbManager = default!;
        [Dependency] private readonly PlayerRateLimitManager _rateLimit = default!;
        private ISharedSponsorsManager? _sponsorsManager; // Sunrise-Sponsors

        private sealed class MentorStatisticsCache
        {
            public List<MentorHelpStatisticsData> WeekStatistics { get; init; } = new();
            public List<MentorHelpStatisticsData> MonthStatistics { get; init; } = new();
            public List<MentorHelpStatisticsData> AllTimeStatistics { get; init; } = new();
        }

        private MentorStatisticsCache? _mentorStatsCache;
        private DateTimeOffset? _mentorStatsCacheTime;
        private uint _mentorStatsCacheVersion;
        private readonly float _mentorCacheInterval = 10;

        public override void Initialize()
        {
            base.Initialize();

            _rateLimit.Register(
                RateLimitKey,
                new RateLimitRegistration(SunriseCCVars.MentorHelpRateLimitPeriod, // Reuse ahelp rate limit config
                    SunriseCCVars.MentorHelpRateLimitCount,
                    PlayerRateLimitedAction)
            );

            SubscribeNetworkEvent<MentorHelpClientTypingUpdated>(OnClientTypingUpdated);

            _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
            IoCManager.Instance!.TryResolveType(out _sponsorsManager); // Sunrise-Sponsors
        }

        private void PlayerRateLimitedAction(ICommonSession session)
        {
            Log.Warning($"Player {session.Name} ({session.UserId}) was rate limited for mentor help");
        }

        private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
        {
            // Could notify mentors about player connection status for active tickets
            // For now, keep it simple
        }

        protected override async void OnCreateTicketMessage(MentorHelpCreateTicketMessage message, EntitySessionEventArgs eventArgs)
        {
            var session = eventArgs.SenderSession;

            // Rate limiting
            if (_rateLimit.CountAction(session, RateLimitKey) != RateLimitStatus.Allowed)
                return;

            // Validate input
            if (string.IsNullOrWhiteSpace(message.Subject) || string.IsNullOrWhiteSpace(message.Message))
            {
                Log.Warning($"Player {session.Name} ({session.UserId}) tried to create mentor help ticket with empty subject or message");
                return;
            }

            if (message.Subject.Length > 256 || message.Message.Length > 4096)
            {
                Log.Warning($"Player {session.Name} ({session.UserId}) tried to create mentor help ticket with too long subject or message");
                return;
            }

            try
            {
                var now = DateTimeOffset.UtcNow;
                var ticket = new MentorHelpTicket
                {
                    PlayerId = session.UserId,
                    Subject = message.Subject.Trim(),
                    Status = MentorHelpTicketStatus.Open,
                    CreatedAt = now,
                    UpdatedAt = now,
                    RoundId = _gameTicker.RoundId,
                    ServerId = await GetServerIdAsync()
                };

                await _dbManager.AddMentorHelpTicketAsync(ticket);

                var ticketMessage = new MentorHelpMessage
                {
                    TicketId = ticket.Id,
                    SenderUserId = session.UserId.UserId,
                    Message = message.Message.Trim(),
                    SentAt = now,
                    IsStaffOnly = false
                };
                await _dbManager.AddMentorHelpMessageAsync(ticketMessage);

                Log.Info($"Player {session.Name} ({session.UserId}) created mentor help ticket #{ticket.Id}: {ticket.Subject}");

                var ticketData = await ConvertToTicketDataAsync(ticket);
                await NotifyTicketUpdate(ticketData);
                // Instruct the player's client to open the newly created ticket
                RaiseNetworkEvent(new MentorHelpOpenTicketMessage(ticket.Id), session.Channel);

                var messageData = await ConvertToMessageDataAsync(ticketMessage);
                await NotifyTicketMessage(ticketData, messageData);
            }
            catch (Exception ex)
            {
                Log.Error($"Error creating mentor help ticket for {session.Name} ({session.UserId}): {ex}");
            }
        }

        protected override async void OnClaimTicketMessage(MentorHelpClaimTicketMessage message, EntitySessionEventArgs eventArgs)
        {
            var session = eventArgs.SenderSession;

            // Check permissions
            if (!HasMentorPermissions(session))
            {
                Log.Warning($"Player {session.Name} ({session.UserId}) tried to claim mentor help ticket without permissions");
                return;
            }

            try
            {
                var ticket = await _dbManager.GetMentorHelpTicketAsync(message.TicketId);
                if (ticket == null)
                {
                    Log.Warning($"Mentor {session.Name} ({session.UserId}) tried to claim non-existent ticket #{message.TicketId}");
                    return;
                }

                if (ticket.Status == MentorHelpTicketStatus.Closed)
                {
                    Log.Warning($"Mentor {session.Name} ({session.UserId}) tried to claim closed ticket #{message.TicketId}");
                    return;
                }

                if (ticket.AssignedToUserId.HasValue && ticket.AssignedToUserId.Value != session.UserId.UserId)
                {
                    return;
                }

                // Claim the ticket
                ticket.AssignedToUserId = session.UserId.UserId;
                ticket.Status = MentorHelpTicketStatus.Assigned;
                ticket.UpdatedAt = DateTimeOffset.UtcNow;

                await _dbManager.UpdateMentorHelpTicketAsync(ticket);

                Log.Info($"Mentor {session.Name} ({session.UserId}) claimed ticket #{ticket.Id}");

                // Notify all relevant parties
                var ticketData = await ConvertToTicketDataAsync(ticket);
                await NotifyTicketUpdate(ticketData);
            }
            catch (Exception ex)
            {
                Log.Error($"Error claiming mentor help ticket #{message.TicketId} by {session.Name} ({session.UserId}): {ex}");
            }
        }

        protected override async void OnReplyMessage(MentorHelpReplyMessage message, EntitySessionEventArgs eventArgs)
        {
            var session = eventArgs.SenderSession;

            // Rate limiting
            if (_rateLimit.CountAction(session, RateLimitKey) != RateLimitStatus.Allowed)
                return;

            try
            {
                var ticket = await _dbManager.GetMentorHelpTicketAsync(message.TicketId);
                if (ticket == null)
                {
                    Log.Warning($"Player {session.Name} ({session.UserId}) tried to reply to non-existent ticket #{message.TicketId}");
                    return;
                }

                if (ticket.Status == MentorHelpTicketStatus.Closed)
                {
                    Log.Warning($"Player {session.Name} ({session.UserId}) tried to reply to closed ticket #{message.TicketId}");
                    return;
                }

                // Check permissions - player can reply to their own ticket, mentors/admins can reply to any
                var isTicketOwner = ticket.PlayerId == session.UserId.UserId;
                var hasMentorPerms = HasMentorPermissions(session);

                if (!isTicketOwner && !hasMentorPerms)
                {
                    Log.Warning($"Player {session.Name} ({session.UserId}) tried to reply to ticket #{message.TicketId} without permissions");
                    return;
                }

                // Staff-only messages can only be sent by mentors/admins
                if (message.IsStaffOnly && !hasMentorPerms)
                {
                    Log.Warning($"Player {session.Name} ({session.UserId}) tried to send staff-only message without permissions");
                    return;
                }

                // Validate message
                if (string.IsNullOrWhiteSpace(message.Message) || message.Message.Length > 4096)
                {
                    Log.Warning($"Player {session.Name} ({session.UserId}) tried to send invalid message to ticket #{message.TicketId}");
                    return;
                }

                // Create the message
                var ticketMessage = new MentorHelpMessage
                {
                    TicketId = message.TicketId,
                    SenderUserId = session.UserId.UserId,
                    Message = message.Message.Trim(),
                    SentAt = DateTimeOffset.UtcNow,
                    IsStaffOnly = message.IsStaffOnly
                };

                await _dbManager.AddMentorHelpMessageAsync(ticketMessage);

                // Update ticket status
                if (hasMentorPerms && ticket.Status == MentorHelpTicketStatus.Open)
                {
                    // Mentor replied to open ticket, mark as assigned
                    ticket.AssignedToUserId = session.UserId.UserId;
                    ticket.Status = MentorHelpTicketStatus.Assigned;
                }
                else if (hasMentorPerms)
                {
                    // Mentor replied, awaiting player response
                    ticket.Status = MentorHelpTicketStatus.AwaitingResponse;
                }
                else if (isTicketOwner && ticket.Status == MentorHelpTicketStatus.AwaitingResponse)
                {
                    // Player replied, mark as assigned again
                    ticket.Status = MentorHelpTicketStatus.Assigned;
                }

                ticket.UpdatedAt = DateTimeOffset.UtcNow;
                await _dbManager.UpdateMentorHelpTicketAsync(ticket);
                InvalidateStatisticsCache();

                Log.Info($"Player {session.Name} ({session.UserId}) replied to ticket #{message.TicketId}");

                // Notify relevant parties
                var ticketData = await ConvertToTicketDataAsync(ticket);
                var messageData = await ConvertToMessageDataAsync(ticketMessage);
                await NotifyTicketUpdate(ticketData);
                await NotifyTicketMessage(ticketData, messageData);

                if (hasMentorPerms)
                {
                    var userId = new NetUserId(ticket.PlayerId);

                    if (_playerManager.TryGetSessionById(userId, out var authorSession))
                    {
                        RaiseNetworkEvent(new MentorHelpOpenTicketMessage(ticket.Id), authorSession.Channel);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error adding reply to mentor help ticket #{message.TicketId} by {session.Name} ({session.UserId}): {ex}");
            }
        }

        protected override async void OnCloseTicketMessage(MentorHelpCloseTicketMessage message, EntitySessionEventArgs eventArgs)
        {
            var session = eventArgs.SenderSession;

            try
            {
                var ticket = await _dbManager.GetMentorHelpTicketAsync(message.TicketId);
                if (ticket == null)
                {
                    Log.Warning($"Player {session.Name} ({session.UserId}) tried to close non-existent ticket #{message.TicketId}");
                    return;
                }

                if (ticket.Status == MentorHelpTicketStatus.Closed)
                {
                    Log.Warning($"Player {session.Name} ({session.UserId}) tried to close already closed ticket #{message.TicketId}");
                    return;
                }

                // Check permissions - player can close their own ticket, mentors/admins can close any
                var isTicketOwner = ticket.PlayerId == session.UserId.UserId;
                var hasMentorPerms = HasMentorPermissions(session);

                if (!isTicketOwner && !hasMentorPerms)
                {
                    Log.Warning($"Player {session.Name} ({session.UserId}) tried to close ticket #{message.TicketId} without permissions");
                    return;
                }

                // Close the ticket
                ticket.Status = MentorHelpTicketStatus.Closed;
                ticket.ClosedAt = DateTimeOffset.UtcNow;
                ticket.ClosedByUserId = session.UserId.UserId;
                ticket.UpdatedAt = DateTimeOffset.UtcNow;

                await _dbManager.UpdateMentorHelpTicketAsync(ticket);
                InvalidateStatisticsCache();

                Log.Info($"Player {session.Name} ({session.UserId}) closed ticket #{ticket.Id}");

                // Notify relevant parties
                var ticketData = await ConvertToTicketDataAsync(ticket);
                await NotifyTicketUpdate(ticketData);
            }
            catch (Exception ex)
            {
                Log.Error($"Error closing mentor help ticket #{message.TicketId} by {session.Name} ({session.UserId}): {ex}");
            }
        }

        protected override async void OnRequestTicketsMessage(MentorHelpRequestTicketsMessage message, EntitySessionEventArgs eventArgs)
        {
            var session = eventArgs.SenderSession;

            try
            {
                List<MentorHelpTicket> tickets;

                if (message.OnlyMine)
                {
                    // Player requesting their own tickets (both open and closed)
                    tickets = await _dbManager.GetMentorHelpTicketsByPlayerAsync(session.UserId.UserId);
                }
                else
                {
                    // Mentor/admin requesting all tickets (both open and closed)
                    if (!HasMentorPermissions(session))
                    {
                        Log.Warning($"Player {session.Name} ({session.UserId}) tried to request all mentor help tickets without permissions");
                        return;
                    }

                    // Get both open and closed tickets for mentors
                    var openTickets = await _dbManager.GetOpenMentorHelpTicketsAsync();
                    var closedTickets = await _dbManager.GetClosedMentorHelpTicketsAsync();
                    tickets = openTickets.Concat(closedTickets).ToList();
                }

                // Collect all unique user IDs for batch loading
                var userIds = new HashSet<Guid>();
                foreach (var ticket in tickets)
                {
                    userIds.Add(ticket.PlayerId);
                    if (ticket.AssignedToUserId.HasValue)
                        userIds.Add(ticket.AssignedToUserId.Value);
                    if (ticket.ClosedByUserId.HasValue)
                        userIds.Add(ticket.ClosedByUserId.Value);
                }

                // Load all player names in one batch query
                var playerNames = await _dbManager.GetPlayerNamesBatchAsync(userIds);

                // Convert tickets to data using cached names
                var ticketDataList = new List<MentorHelpTicketData>();
                foreach (var ticket in tickets)
                {
                    ticketDataList.Add(ConvertToTicketData(ticket, playerNames));
                }

                RaiseNetworkEvent(new MentorHelpTicketsListMessage(ticketDataList), session.Channel);
            }
            catch (Exception ex)
            {
                Log.Error($"Error requesting mentor help tickets for {session.Name} ({session.UserId}): {ex}");
            }
        }

        protected override async void OnUnassignTicketMessage(MentorHelpUnassignTicketMessage message, EntitySessionEventArgs eventArgs)
        {
            var session = eventArgs.SenderSession;

            if (!HasMentorPermissions(session))
            {
                Log.Warning($"Player {session.Name} ({session.UserId}) tried to unassign mentor help ticket without permissions");
                return;
            }

            try
            {
                var ticket = await _dbManager.GetMentorHelpTicketAsync(message.TicketId);
                if (ticket == null)
                {
                    Log.Warning($"Mentor {session.Name} ({session.UserId}) tried to unassign non-existent ticket #{message.TicketId}");
                    return;
                }

                if (ticket.Status == MentorHelpTicketStatus.Closed)
                {
                    Log.Warning($"Mentor {session.Name} ({session.UserId}) tried to unassign closed ticket #{message.TicketId}");
                    return;
                }

                ticket.AssignedToUserId = null;
                ticket.Status = MentorHelpTicketStatus.Open;
                ticket.UpdatedAt = DateTimeOffset.UtcNow;

                await _dbManager.UpdateMentorHelpTicketAsync(ticket);

                Log.Info($"Mentor {session.Name} ({session.UserId}) unassigned ticket #{ticket.Id}");

                var ticketData = await ConvertToTicketDataAsync(ticket);
                await NotifyTicketUpdate(ticketData);
            }
            catch (Exception ex)
            {
                Log.Error($"Error unassigning mentor help ticket #{message.TicketId} by {session.Name} ({session.UserId}): {ex}");
            }
        }

        private bool HasMentorPermissions(ICommonSession session)
        {
            var adminData = _adminManager.GetAdminData(session);
            return adminData?.HasFlag(AdminFlags.Mentor) ?? false;
        }

        private async Task<int?> GetServerIdAsync()
        {
            // Implementation would depend on how server ID is tracked
            // For now, return null
            return null;
        }
        private void InvalidateStatisticsCache() // Чистка кеша
        {
            _mentorStatsCacheVersion++;
            _mentorStatsCache = null;
            _mentorStatsCacheTime = null;
        }

        private async Task<MentorStatisticsCache> BuildStatisticsCacheAsync(DateTimeOffset now)
        {
            var weekStatistics = await _dbManager.GetMentorHelpStatisticsAsync(now.AddDays(-7)); // Неделя
            var monthStatistics = await _dbManager.GetMentorHelpStatisticsAsync(now.AddMonths(-1)); // Месяц
            var allTimeStatistics = await _dbManager.GetMentorHelpStatisticsAsync(null); // Все время

            var mentorUserIds = new HashSet<Guid>();

            foreach (var stat in weekStatistics)
            {
                mentorUserIds.Add(stat.MentorUserId);
            }

            foreach (var stat in monthStatistics)
            {
                mentorUserIds.Add(stat.MentorUserId);
            }

            foreach (var stat in allTimeStatistics)
            {
                mentorUserIds.Add(stat.MentorUserId);
            }

            var activeMentorIds = new HashSet<Guid>();
            Dictionary<Guid, Admin>? offlineAdminsByUserId = null;
            Dictionary<int, AdminRank>? adminRanksById = null;

            foreach (var mentorUserId in mentorUserIds)
            {
                var mentorNetUserId = new NetUserId(mentorUserId);
                var isMentor = false;

                if (_playerManager.TryGetSessionById(mentorNetUserId, out var mentorSession))
                    isMentor = _adminManager.GetAdminData(mentorSession)?.HasFlag(AdminFlags.Mentor) ?? false;

                else
                {
                    if (offlineAdminsByUserId == null || adminRanksById == null)
                    {
                        var (admins, adminRanks) = await _dbManager.GetAllAdminAndRanksAsync();
                        offlineAdminsByUserId = admins.ToDictionary(admin => admin.Item1.UserId, admin => admin.Item1);
                        adminRanksById = adminRanks.ToDictionary(rank => rank.Id);
                    }

                    isMentor = offlineAdminsByUserId.TryGetValue(mentorUserId, out var adminData) &&
                        HasAdminFlag(adminData, adminRanksById, AdminFlags.Mentor);
                }

                if (!isMentor)
                    continue;

                activeMentorIds.Add(mentorUserId);
            }

            var mentorNames = await _dbManager.GetPlayerNamesBatchAsync(activeMentorIds);

            return new MentorStatisticsCache
            {
                WeekStatistics = ConvertStatistics(weekStatistics, mentorNames),
                MonthStatistics = ConvertStatistics(monthStatistics, mentorNames),
                AllTimeStatistics = ConvertStatistics(allTimeStatistics, mentorNames)
            };
        }

        private static bool HasAdminFlag(Admin admin, Dictionary<int, AdminRank> adminRanksById, AdminFlags flag)
        {
            if (admin.Suspended || admin.Deadminned)
                return false;

            var flags = AdminFlags.None;

            if (admin.AdminRankId != null &&
                adminRanksById.TryGetValue(admin.AdminRankId.Value, out var adminRank))
                flags = AdminFlagsHelper.NamesToFlags(adminRank.Flags.Select(rankFlag => rankFlag.Flag));

            foreach (var dbFlag in admin.Flags)
            {
                var adminFlag = AdminFlagsHelper.NameToFlag(dbFlag.Flag);
                if (dbFlag.Negative)
                    flags &= ~adminFlag;
                else
                    flags |= adminFlag;
            }

            return flags.HasFlag(flag);
        }

        private static List<MentorHelpStatisticsData> ConvertStatistics(List<MentorHelpStatistics> statistics, Dictionary<Guid, string> mentorNames)
        {
            var result = new List<MentorHelpStatisticsData>(statistics.Count);

            foreach (var stat in statistics)
            {
                mentorNames.TryGetValue(stat.MentorUserId, out var mentorName);

                result.Add(new MentorHelpStatisticsData
                {
                    MentorName = mentorName ?? "Unknown",
                    TicketsClosed = stat.TicketsClosed,
                    MessagesCount = stat.MessagesCount
                });
            }

            result.Sort((left, right) =>
            {
                var compare = right.TicketsClosed.CompareTo(left.TicketsClosed);
                if (compare != 0)
                    return compare;

                compare = right.MessagesCount.CompareTo(left.MessagesCount);
                if (compare != 0)
                    return compare;

                return string.Compare(left.MentorName, right.MentorName, StringComparison.Ordinal);
            });

            return result;
        }

        protected override async void OnRequestStatisticsMessage(MentorHelpRequestStatisticsMessage message, EntitySessionEventArgs eventArgs)
        {
            var session = eventArgs.SenderSession;

            if (!HasMentorPermissions(session))
            {
                Log.Warning($"Player {session.Name} ({session.UserId}) tried to request mentor help statistics without permissions");
                return;
            }

            try
            {
                var now = DateTimeOffset.UtcNow;
                var cacheValid = _mentorStatsCache != null && _mentorStatsCacheTime != null && (now - _mentorStatsCacheTime.Value).TotalMinutes < _mentorCacheInterval;

                if (!cacheValid)
                {
                    var cacheVersion = _mentorStatsCacheVersion;
                    now = DateTimeOffset.UtcNow;
                    var cache = await BuildStatisticsCacheAsync(now);

                    if (cacheVersion == _mentorStatsCacheVersion)
                    {
                        _mentorStatsCache = cache;
                        _mentorStatsCacheTime = now;
                    }
                }

                RaiseNetworkEvent(new MentorHelpStatisticsMessage(
                    _mentorStatsCache!.WeekStatistics,
                    _mentorStatsCache.MonthStatistics,
                    _mentorStatsCache.AllTimeStatistics), session.Channel);
            }
            catch (Exception ex)
            {
                Log.Error($"Error requesting mentor help statistics for {session.Name} ({session.UserId}): {ex}");
            }
        }

        protected override async void OnRequestTicketMessagesMessage(MentorHelpRequestTicketMessagesMessage message, EntitySessionEventArgs eventArgs)
        {
            var session = eventArgs.SenderSession;

            Log.Info("Received RequestTicketMessages for ticket #{0} from {1} ({2})", message.TicketId, session.Name, session.UserId);

            try
            {
                var ticket = await _dbManager.GetMentorHelpTicketAsync(message.TicketId);
                if (ticket == null)
                {
                    Log.Warning($"Player {session.Name} ({session.UserId}) tried to request messages for non-existent ticket #{message.TicketId}");
                    return;
                }

                var hasMentorPerms = HasMentorPermissions(session);
                var isTicketOwner = ticket.PlayerId == session.UserId.UserId;

                if (!hasMentorPerms && !isTicketOwner)
                {
                    Log.Warning($"Player {session.Name} ({session.UserId}) tried to request messages for ticket #{message.TicketId} without permissions");
                    return;
                }

                var allMessages = await _dbManager.GetMentorHelpMessagesByTicketAsync(message.TicketId);
                var messageDatas = new List<MentorHelpMessageData>();
                foreach (var msg in allMessages.OrderBy(m => m.SentAt))
                {
                    if (!hasMentorPerms && msg.IsStaffOnly)
                        continue;

                    messageDatas.Add(await ConvertToMessageDataAsync(msg));
                }

                RaiseNetworkEvent(new MentorHelpTicketMessagesMessage(message.TicketId, messageDatas), session.Channel);
                Log.Info("Sent {0} messages for ticket #{1} to {2} ({3})", messageDatas.Count, message.TicketId, session.Name, session.UserId);
            }
            catch (Exception ex)
            {
                Log.Error($"Error requesting mentor help messages for ticket #{message.TicketId} by {session.Name} ({session.UserId}): {ex}");
            }
        }


        private MentorHelpTicketData ConvertToTicketData(MentorHelpTicket ticket, Dictionary<Guid, string> playerNames)
        {
            playerNames.TryGetValue(ticket.PlayerId, out var playerName);
            var assignedToName = ticket.AssignedToUserId.HasValue && playerNames.TryGetValue(ticket.AssignedToUserId.Value, out var assignedName) ? assignedName : null;
            var closedByName = ticket.ClosedByUserId.HasValue && playerNames.TryGetValue(ticket.ClosedByUserId.Value, out var closedName) ? closedName : null;

            return new MentorHelpTicketData
            {
                Id = ticket.Id,
                PlayerId = new NetUserId(ticket.PlayerId),
                PlayerName = playerName ?? "Unknown",
                AssignedToUserId = ticket.AssignedToUserId.HasValue ? new NetUserId(ticket.AssignedToUserId.Value) : null,
                AssignedToName = assignedToName,
                Subject = ticket.Subject,
                Status = ticket.Status,
                CreatedAt = ticket.CreatedAt.DateTime,
                UpdatedAt = ticket.UpdatedAt.DateTime,
                ClosedAt = ticket.ClosedAt?.DateTime,
                ClosedByUserId = ticket.ClosedByUserId.HasValue ? new NetUserId(ticket.ClosedByUserId.Value) : null,
                ClosedByName = closedByName,
                RoundId = ticket.RoundId,
                HasUnreadMessages = false // Would need to implement read tracking
            };
        }

        private async Task<MentorHelpTicketData> ConvertToTicketDataAsync(MentorHelpTicket ticket)
        {
            var playerName = await GetPlayerNameAsync(ticket.PlayerId);
            var assignedToName = ticket.AssignedToUserId.HasValue ? await GetPlayerNameAsync(ticket.AssignedToUserId.Value) : null;
            var closedByName = ticket.ClosedByUserId.HasValue ? await GetPlayerNameAsync(ticket.ClosedByUserId.Value) : null;

            return new MentorHelpTicketData
            {
                Id = ticket.Id,
                PlayerId = new NetUserId(ticket.PlayerId),
                PlayerName = playerName,
                AssignedToUserId = ticket.AssignedToUserId.HasValue ? new NetUserId(ticket.AssignedToUserId.Value) : null,
                AssignedToName = assignedToName,
                Subject = ticket.Subject,
                Status = ticket.Status,
                CreatedAt = ticket.CreatedAt.DateTime,
                UpdatedAt = ticket.UpdatedAt.DateTime,
                ClosedAt = ticket.ClosedAt?.DateTime,
                ClosedByUserId = ticket.ClosedByUserId.HasValue ? new NetUserId(ticket.ClosedByUserId.Value) : null,
                ClosedByName = closedByName,
                RoundId = ticket.RoundId,
                HasUnreadMessages = false // Would need to implement read tracking
            };
        }

        private async Task<MentorHelpMessageData> ConvertToMessageDataAsync(MentorHelpMessage message)
        {
            var senderUserId = new NetUserId(message.SenderUserId);

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

            if (string.IsNullOrWhiteSpace(username))
            {
                var senderData = await _dbManager.GetPlayerRecordByUserId(senderUserId);
                username = senderData?.LastSeenUserName;
            }

            username ??= "Unknown";

            string formatterSender;
            var adminPrefix = "";
            var escapedUsername = FormattedMessage.EscapeText(username);

            if (_config.GetCVar(SunriseCCVars.MentorHelpAdminPrefix) && senderAdminData?.Title is not null)
                adminPrefix = $"[bold]\\[{FormattedMessage.EscapeText(senderAdminData.Title)}\\][/bold] ";


            if (senderAdminData != null && senderAdminData.HasFlag(AdminFlags.Mentor) && senderAdminData.Flags == AdminFlags.Mentor)
                formatterSender = $"[color=purple]{adminPrefix}{escapedUsername}[/color]";

            else if (senderAdminData != null && senderAdminData.HasFlag(AdminFlags.Mentor))
                formatterSender = $"[color=red]{adminPrefix}{escapedUsername}[/color]";

            else if (_sponsorsManager != null)
            {
                _sponsorsManager.TryGetOocColor(senderUserId, out var oocColor);
                _sponsorsManager.TryGetOocTitle(senderUserId, out var oocTitle);

                var sponsorTitle = oocTitle is null ? "" : $"\\[{FormattedMessage.EscapeText(oocTitle)}\\]";
                var sponsorPrefix = sponsorTitle == "" ? "" : $"{sponsorTitle} ";
                if (oocColor != null)
                    formatterSender = $"[color={oocColor.Value.ToHex()}]{sponsorPrefix}{escapedUsername}[/color]";

                else
                    formatterSender = $"{sponsorPrefix}{escapedUsername}";
            }
            else
                formatterSender = escapedUsername;

            return new MentorHelpMessageData
            {
                Id = message.Id,
                TicketId = message.TicketId,
                SenderUserId = senderUserId,
                SenderName = username,
                FormattedSender = formatterSender,
                Message = message.Message,
                SentAt = message.SentAt.DateTime,
                IsStaffOnly = message.IsStaffOnly
            };
        }

        private async Task<string> GetPlayerNameAsync(Guid userId)
        {
            var playerData = await _dbManager.GetPlayerRecordByUserId(new NetUserId(userId));
            var name = playerData?.LastSeenUserName;
            if (string.IsNullOrWhiteSpace(name))
            {
                Log.Warning($"GetPlayerNameAsync: No name found for userId {userId}, returning 'Unknown'.");
                return "Unknown";
            }
            return name;
        }

        private async Task NotifyTicketUpdate(MentorHelpTicketData ticketData)
        {
            // Notify the player
            if (_playerManager.TryGetSessionById(ticketData.PlayerId, out var playerSession))
            {
                RaiseNetworkEvent(new MentorHelpTicketUpdateMessage(ticketData), playerSession.Channel);
            }

            // Notify mentors
            var mentors = GetTargetMentors();
            foreach (var mentor in mentors)
            {
                RaiseNetworkEvent(new MentorHelpTicketUpdateMessage(ticketData), mentor);
            }
        }

        private async Task NotifyTicketMessage(MentorHelpTicketData ticketData, MentorHelpMessageData messageData)
        {
            var allMessages = await _dbManager.GetMentorHelpMessagesByTicketAsync(ticketData.Id);
            var messageDatas = new List<MentorHelpMessageData>();
            foreach (var msg in allMessages.OrderBy(m => m.SentAt)) // сортировка теперь точно по объектам
            {
                messageDatas.Add(await ConvertToMessageDataAsync(msg));
            }

            // Notify the player (if not staff-only)
            if (!messageData.IsStaffOnly && _playerManager.TryGetSessionById(ticketData.PlayerId, out var playerSession))
            {
                RaiseNetworkEvent(new MentorHelpTicketMessagesMessage(ticketData.Id, messageDatas), playerSession.Channel);
            }

            // Notify mentors
            var mentors = GetTargetMentors();
            foreach (var mentor in mentors)
            {
                RaiseNetworkEvent(new MentorHelpTicketMessagesMessage(ticketData.Id, messageDatas), mentor);
            }
        }

        private IList<INetChannel> GetTargetMentors()
        {
            return _adminManager.ActiveAdmins
                .Where(p => _adminManager.GetAdminData(p)?.HasFlag(AdminFlags.Mentor) ?? false)
                .Select(p => p.Channel)
                .ToList();
        }

        private async void OnClientTypingUpdated(MentorHelpClientTypingUpdated msg, EntitySessionEventArgs args)
        {
            var session = args.SenderSession;
            var ticket = await _dbManager.GetMentorHelpTicketAsync(msg.TicketId);
            if (ticket == null)
                return;

            var update = new MentorHelpPlayerTypingUpdated(msg.TicketId, session.UserId, session.Name, msg.Typing);
            var recipients = new HashSet<INetChannel>();

            if (_playerManager.TryGetSessionById(new NetUserId(ticket.PlayerId), out var authorSession))
            {
                if (!authorSession.UserId.Equals(session.UserId))
                    recipients.Add(authorSession.Channel);
            }

            if (ticket.AssignedToUserId.HasValue &&
                _playerManager.TryGetSessionById(new NetUserId(ticket.AssignedToUserId.Value), out var mentorSession))
            {
                if (!mentorSession.UserId.Equals(session.UserId))
                    recipients.Add(mentorSession.Channel);
            }

            foreach (var recipient in recipients)
                RaiseNetworkEvent(update, recipient);
        }
    }
}

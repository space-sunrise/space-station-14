using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Administration.Systems;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Shared.Administration.Logs;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Mind;
using Content.Shared.Players.PlayTimeTracking;
using Prometheus;
using Robust.Server.GameObjects;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Reflection;
using System.Linq;
using System.Text.RegularExpressions;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Administration.Logs;

public sealed partial class AdminLogManager : SharedAdminLogManager, IAdminLogManager
{
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IDynamicTypeFactory _typeFactory = default!;
    [Dependency] private readonly IReflectionManager _reflection = default!;
    [Dependency] private readonly IDependencyCollection _dependencies = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly ISharedPlaytimeManager _playtime = default!;
    [Dependency] private readonly ISharedChatManager _chat = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public const string SawmillId = "admin.logs";

    private static readonly Histogram DatabaseUpdateTime = Metrics.CreateHistogram(
        "admin_logs_database_time",
        "Time used to send logs to the database in ms",
        new HistogramConfiguration
        {
            Buckets = Histogram.LinearBuckets(0, 0.5, 20)
        });

    private static readonly Gauge Queue = Metrics.CreateGauge(
        "admin_logs_queue",
        "How many logs are in the queue.");

    private static readonly Gauge PreRoundQueue = Metrics.CreateGauge(
        "admin_logs_pre_round_queue",
        "How many logs are in the pre-round queue.");

    private static readonly Gauge QueueCapReached = Metrics.CreateGauge(
        "admin_logs_queue_cap_reached",
        "Number of times the log queue cap has been reached in a round.");

    private static readonly Gauge PreRoundQueueCapReached = Metrics.CreateGauge(
        "admin_logs_queue_cap_reached",
        "Number of times the pre-round log queue cap has been reached in a round.");

    private static readonly Gauge LogsSent = Metrics.CreateGauge(
        "admin_logs_sent",
        "Amount of logs sent to the database in a round.");

    // Init only
    private ISawmill _sawmill = default!;

    // CVars
    private bool _metricsEnabled;
    // Sunrise-Start
    private bool _lokiEnabled;
    private string _lokiUrl = string.Empty;
    private string _lokiUsername = string.Empty;
    private string _lokiPassword = string.Empty;
    private string _lokiName = "unknown";
    // Sunrise-End

    private TimeSpan _queueSendDelay;
    private int _queueMax;
    private int _preRoundQueueMax;
    private int _dropThreshold;
    private int _highImpactLogPlaytime;

    // Sunrise-Start
    private readonly System.Net.Http.HttpClient _httpClient = new();
    // Sunrise-End

    // Per update
    private TimeSpan _nextUpdateTime;
    private readonly ConcurrentQueue<AdminLog> _logQueue = new();
    private readonly ConcurrentQueue<AdminLog> _preRoundLogQueue = new();

    // Per round
    private int _currentRoundId;
    private int _currentLogId;
    private int NextLogId => Interlocked.Increment(ref _currentLogId);
    private GameRunLevel _runLevel = GameRunLevel.PreRoundLobby;

    // 1 when saving, 0 otherwise
    private int _savingLogs;
    private int _logsDropped;

    public void Initialize()
    {
        _sawmill = _logManager.GetSawmill(SawmillId);

        InitializeJson();

        _configuration.OnValueChanged(CVars.MetricsEnabled,
            value => _metricsEnabled = value, true);
        _configuration.OnValueChanged(CCVars.AdminLogsEnabled,
            value => Enabled = value, true);
        _configuration.OnValueChanged(CCVars.AdminLogsQueueSendDelay,
            value => _queueSendDelay = TimeSpan.FromSeconds(value), true);
        _configuration.OnValueChanged(CCVars.AdminLogsQueueMax,
            value => _queueMax = value, true);
        _configuration.OnValueChanged(CCVars.AdminLogsPreRoundQueueMax,
            value => _preRoundQueueMax = value, true);
        // Sunrise-Start
        _configuration.OnValueChanged(CCVars.AdminLogsToLoki,
            value => _lokiEnabled = value, true);
        _configuration.OnValueChanged(CCVars.AdminLogsLokiUrl,
            value => _lokiUrl = value, true);
        _configuration.OnValueChanged(CCVars.AdminLogsLokiUsername,
            value =>
            {
                _lokiUsername = value;
                SetLokiAuth();
            }, true);
        _configuration.OnValueChanged(CCVars.AdminLogsLokiPassword,
            value =>
            {
                _lokiPassword = value;
                SetLokiAuth();
            }, true);
        _configuration.OnValueChanged(CCVars.AdminLogsLokiName,
            value => _lokiName = value, true);
        // Sunrise-End
        _configuration.OnValueChanged(CCVars.AdminLogsDropThreshold,
            value => _dropThreshold = value, true);
        _configuration.OnValueChanged(CCVars.AdminLogsHighLogPlaytime,
            value => _highImpactLogPlaytime = value, true);

        if (_metricsEnabled)
        {
            PreRoundQueueCapReached.Set(0);
            QueueCapReached.Set(0);
            LogsSent.Set(0);
        }
    }

    public override string ConvertName(string name)
    {
        // JsonNamingPolicy is not whitelisted by the sandbox.
        return NamingPolicy.ConvertName(name);
    }

    // Sunrise-Start
    private void SetLokiAuth()
    {
        _httpClient.DefaultRequestHeaders.Authorization = null;
        if (!string.IsNullOrEmpty(_lokiUsername) && !string.IsNullOrEmpty(_lokiPassword))
        {
            var authString = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{_lokiUsername}:{_lokiPassword}"));
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authString);
        }
    }
    // Sunrise-End

    public async Task Shutdown()
    {
        if (!_logQueue.IsEmpty)
        {
            await SaveLogs();
        }
    }

    public async void Update()
    {
        if (_runLevel == GameRunLevel.PreRoundLobby)
        {
            await PreRoundUpdate();
            return;
        }

        var count = _logQueue.Count;
        Queue.Set(count);

        var preRoundCount = _preRoundLogQueue.Count;
        PreRoundQueue.Set(preRoundCount);

        if (count + preRoundCount == 0)
        {
            return;
        }

        if (_timing.RealTime >= _nextUpdateTime)
        {
            await TrySaveLogs();
            return;
        }

        if (count >= _queueMax)
        {
            if (_metricsEnabled)
            {
                QueueCapReached.Inc();
            }

            await TrySaveLogs();
        }
    }

    private async Task PreRoundUpdate()
    {
        var preRoundCount = _preRoundLogQueue.Count;
        PreRoundQueue.Set(preRoundCount);

        if (preRoundCount < _preRoundQueueMax)
        {
            return;
        }

        if (_metricsEnabled)
        {
            PreRoundQueueCapReached.Inc();
        }

        await TrySaveLogs();
    }

    private async Task TrySaveLogs()
    {
        if (Interlocked.Exchange(ref _savingLogs, 1) == 1)
            return;

        try
        {
            await SaveLogs();
        }
        finally
        {
            Interlocked.Exchange(ref _savingLogs, 0);
        }
    }

    private async Task SaveLogs()
    {
        _nextUpdateTime = _timing.RealTime.Add(_queueSendDelay);

        // TODO ADMIN LOGS array pool
        var copy = new List<AdminLog>(_logQueue.Count + _preRoundLogQueue.Count);
        copy.AddRange(_logQueue);

        if (_logQueue.Count >= _queueMax)
        {
            _sawmill.Warning($"In-round cap of {_queueMax} reached for admin logs.");
        }

        var dropped = Interlocked.Exchange(ref _logsDropped, 0);
        if (dropped > 0)
        {
            _sawmill.Error($"Dropped {dropped} logs. Current max threshold: {_dropThreshold}");
        }

        if (_runLevel == GameRunLevel.PreRoundLobby && !_preRoundLogQueue.IsEmpty)
        {
            _sawmill.Error($"Dropping {_preRoundLogQueue.Count} pre-round logs. Current cap: {_preRoundQueueMax}");
        }
        else
        {
            foreach (var log in _preRoundLogQueue)
            {
                log.RoundId = _currentRoundId;
                CacheLog(log);
            }

            copy.AddRange(_preRoundLogQueue);
        }

        _logQueue.Clear();
        Queue.Set(0);

        _preRoundLogQueue.Clear();
        PreRoundQueue.Set(0);

        // Sunrise-Start
        Task task;
        if (_lokiEnabled)
        {
            task = SaveLogsToLoki(copy);
            _sawmill.Debug($"Saving {copy.Count} admin logs to Loki.");
        }
        else
        {
            task = _db.AddAdminLogs(copy);
            _sawmill.Debug($"Saving {copy.Count} admin logs.");
        }
        // Sunrise-End

        if (_metricsEnabled)
        {
            LogsSent.Inc(copy.Count);

            using (DatabaseUpdateTime.NewTimer())
            {
                await task;
                return;
            }
        }

        await task;
    }

    public void RoundStarting(int id)
    {
        _currentRoundId = id;
        CacheNewRound();
    }

    public void RunLevelChanged(GameRunLevel level)
    {
        _runLevel = level;

        if (level == GameRunLevel.PreRoundLobby)
        {
            Interlocked.Exchange(ref _currentLogId, 0);

            if (!_preRoundLogQueue.IsEmpty)
            {
                // This technically means that you could get pre-round logs from
                // a previous round passed onto the next one
                // If this happens please file a complaint with your nearest lottery
                foreach (var log in _preRoundLogQueue)
                {
                    log.Id = NextLogId;
                }
            }

            if (_metricsEnabled)
            {
                PreRoundQueueCapReached.Set(0);
                QueueCapReached.Set(0);
                LogsSent.Set(0);
            }
        }
    }

    public override void Add(LogType type, [System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("")] ref LogStringHandler handler)
    {
        Add(type, LogImpact.Medium, ref handler);
    }

    public override void Add(LogType type, LogImpact impact, [System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("")] ref LogStringHandler handler)
    {
        var message = handler.ToStringAndClear();
        if (!Enabled)
            return;

        var preRound = _runLevel == GameRunLevel.PreRoundLobby;
        var count = preRound ? _preRoundLogQueue.Count : _logQueue.Count;
        if (count >= _dropThreshold)
        {
            Interlocked.Increment(ref _logsDropped);
            return;
        }

        var json = JsonSerializer.SerializeToDocument(handler.Values, _jsonOptions);
        var id = NextLogId;
        var players = GetPlayers(handler.Values, id);

        // PostgreSQL does not support storing null chars in text values.
        if (message.Contains('\0'))
        {
            _sawmill.Error($"Null character detected in admin log message '{message}'! LogType: {type}, LogImpact: {impact}");
            message = message.Replace("\0", "");
        }

        var log = new AdminLog
        {
            Id = id,
            RoundId = _currentRoundId,
            Type = type,
            Impact = impact,
            Date = DateTime.UtcNow,
            Message = message,
            Json = json,
            Players = players,
        };

        DoAdminAlerts(players, message, impact, handler);

        if (preRound)
        {
            _preRoundLogQueue.Enqueue(log);
        }
        else
        {
            _logQueue.Enqueue(log);
            CacheLog(log);
        }
    }

    private List<AdminLogPlayer> GetPlayers(Dictionary<string, object?> values, int logId)
    {
        List<AdminLogPlayer> players = new();
        foreach (var value in values.Values)
        {
            switch (value)
            {
                case SerializablePlayer player:
                    AddPlayer(players, player.UserId, logId);
                    continue;

                case EntityStringRepresentation rep:
                    if (rep.Session is {} session)
                        AddPlayer(players, session.UserId.UserId, logId);
                    continue;

                case IAdminLogsPlayerValue playerValue:
                    foreach (var player in playerValue.Players)
                    {
                        AddPlayer(players, player, logId);
                    }

                    break;
            }
        }

        return players;
    }

    /// <summary>
    /// Get a list of coordinates from the <see cref="LogStringHandler"/>s values. Will transform all coordinate types
    /// to map coordinates!
    /// </summary>
    /// <returns>A list of map coordinates that were found in the value input, can return an empty list.</returns>
    private List<MapCoordinates> GetCoordinates(Dictionary<string, object?> values)
    {
        List<MapCoordinates> coordList = new();
        EntityManager.TrySystem(out TransformSystem? transform);

        foreach (var value in values.Values)
        {
            switch (value)
            {
                case EntityCoordinates entCords:
                    if (transform != null)
                        coordList.Add(transform.ToMapCoordinates(entCords));
                    continue;

                case MapCoordinates mapCord:
                    coordList.Add(mapCord);
                    continue;
            }
        }

        return coordList;
    }

    private void AddPlayer(List<AdminLogPlayer> players, Guid user, int logId)
    {
        // The majority of logs have a single player, or maybe two. Instead of allocating a List<AdminLogPlayer> and
        // HashSet<Guid>, we just iterate over the list to check for duplicates.
        foreach (var player in players)
        {
            if (player.PlayerUserId == user)
                return;
        }

        players.Add(new AdminLogPlayer
        {
            LogId = logId,
            PlayerUserId = user
        });
    }

    private void DoAdminAlerts(List<AdminLogPlayer> players, string message, LogImpact impact, LogStringHandler handler)
    {
        var adminLog = false;
        var logMessage = message;
        var playerNetEnts = new List<(NetEntity, string)>();

        foreach (var player in players)
        {
            var id = player.PlayerUserId;

            if (EntityManager.TrySystem(out AdminSystem? adminSys))
            {
                var cachedInfo = adminSys.GetCachedPlayerInfo(new NetUserId(id));
                if (cachedInfo != null && cachedInfo.Antag)
                {
                    var proto = cachedInfo.RoleProto == null ? null : _proto.Index(cachedInfo.RoleProto.Value);
                    var subtype = Loc.GetString(cachedInfo.Subtype ?? proto?.Name ?? RoleTypePrototype.FallbackName);
                    logMessage = Loc.GetString(
                        "admin-alert-antag-label",
                        ("message", logMessage),
                        ("name", cachedInfo.CharacterName),
                        ("subtype", subtype));
                }
                if (cachedInfo != null && cachedInfo.NetEntity != null)
                    playerNetEnts.Add((cachedInfo.NetEntity.Value, cachedInfo.CharacterName));
            }

            if (adminLog)
                continue;

            if (impact == LogImpact.Extreme) // Always chat-notify Extreme logs
                adminLog = true;

            if (impact == LogImpact.High) // Only chat-notify High logs if the player is below a threshold playtime
            {
                if (_highImpactLogPlaytime >= 0 && _player.TryGetSessionById(new NetUserId(id), out var session))
                {
                    var playtimes = _playtime.GetPlayTimes(session);
                    if (playtimes.TryGetValue(PlayTimeTrackingShared.TrackerOverall, out var overallTime) &&
                        overallTime <= TimeSpan.FromHours(_highImpactLogPlaytime))
                    {
                        adminLog = true;
                    }
                }
            }
        }

        if (adminLog)
        {
            _chat.SendAdminAlert(logMessage);

            if (CreateTpLinks(playerNetEnts, out var tpLinks))
                _chat.SendAdminAlertNoFormatOrEscape(tpLinks);

            var coords = GetCoordinates(handler.Values);

            if (CreateCordLinks(coords, out var cordLinks))
                _chat.SendAdminAlertNoFormatOrEscape(cordLinks);
        }
    }

    /// <summary>
    /// Creates a list of tpto command links of the given players
    /// </summary>
    private bool CreateTpLinks(List<(NetEntity NetEnt, string CharacterName)> players, out string outString)
    {
        outString = string.Empty;

        if (players.Count == 0)
            return false;

        outString = Loc.GetString("admin-alert-tp-to-players-header");

        for (var i = 0; i < players.Count; i++)
        {
            var player = players[i];
            outString += $"[cmdlink=\"{EscapeText(player.CharacterName)}\" command=\"tpto {player.NetEnt}\"/]";

            if (i < players.Count - 1)
                outString += ", ";
        }

        return true;
    }

    /// <summary>
    /// Creates a list of toto command links for the given map coordinates.
    /// </summary>
    private bool CreateCordLinks(List<MapCoordinates> cords, out string outString)
    {
        outString = string.Empty;

        if (cords.Count == 0)
            return false;

        outString = Loc.GetString("admin-alert-tp-to-coords-header");

        for (var i = 0; i < cords.Count; i++)
        {
            var cord = cords[i];
            outString += $"[cmdlink=\"{cord.ToString()}\" command=\"tp {cord.X} {cord.Y} {cord.MapId}\"/]";

            if (i < cords.Count - 1)
                outString += ", ";
        }

        return true;
    }

    /// <summary>
    /// Escape the given text to not allow breakouts of the cmdlink tags.
    /// </summary>
    private string EscapeText(string text)
    {
        return FormattedMessage.EscapeText(text).Replace("\"", "\\\"").Replace("'", "\\'");
    }

    public async Task<List<SharedAdminLog>> All(LogFilter? filter = null, Func<List<SharedAdminLog>>? listProvider = null)
    {
        if (TrySearchCache(filter, out var results))
        {
            return results;
        }

        var initialSize = Math.Min(filter?.Limit ?? 0, 1000);
        List<SharedAdminLog> list;
        if (listProvider != null)
        {
            list = listProvider();
            list.EnsureCapacity(initialSize);
        }
        else
        {
            list = new List<SharedAdminLog>(initialSize);
        }

        // Sunrise-Start
        if (_lokiEnabled)
        {
            await GetAdminLogsFromLoki(filter, list);
            return list;
        }
        // Sunrise-End

        await foreach (var log in _db.GetAdminLogs(filter).WithCancellation(filter?.CancellationToken ?? default))
        {
            list.Add(log);
        }

        return list;
    }

    // Sunrise-Start
    private static readonly Regex LogHighlightRegex = new Regex(@"(\bEntId=\d+\b|(?<=\()(\b\d+/[^\)]+|\bn\d+\b)(?=\)))|\b(dropped|picked up|inserted|removed|equipped|unequipped|thrown|wielded|unwielded|loaded|unloaded|spawned|deleted|shot|attacked|damaged|hit|exploded|fired|clicked|knocked down|activated|interacted|opened|closed|locked|unlocked|anchored|unanchored|welded|unwelded|bolted|unbolted|connected|disconnected|joined|left|banned|kicked|suicided|died|revived|cloned|respawned|joined|left|refilled|drained|poured|ingested|vomited|collapsed|unconscious|rejuvenated|mounted|dismounted)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private const string Gray = "\u001b[90m";
    private const string Cyan = "\u001b[36m";
    private const string Reset = "\u001b[0m";
    private const string White = "\u001b[37m";

    private async Task SaveLogsToLoki(List<AdminLog> logs)
    {
        if (logs.Count == 0 || string.IsNullOrEmpty(_lokiUrl)) return;
        try
        {
            var stream = new LokiStream
            {
                Labels = new Dictionary<string, string>
                {
                    { "app", _lokiName },
                    { "category", "admin_log" }
                }
            };

            long lastNano = 0;
            foreach (var log in logs)
            {
                var nanoTime = new DateTimeOffset(log.Date).ToUnixTimeMilliseconds() * 1000000;
                if (nanoTime <= lastNano)
                    nanoTime = lastNano + 1;
                lastNano = nanoTime;

                // Create a formatted version for display: IDs in gray, actions in cyan, rest in white
                var colorizedMessage = LogHighlightRegex.Replace(log.Message, m =>
                {
                    if (m.Value.Contains('(') || m.Value.Contains('=') || (m.Value.Length > 0 && char.IsDigit(m.Value[0])))
                        return $"{Gray}{m.Value}{White}";

                    return $"{Cyan}{m.Value}{White}";
                });
                var formattedMessage = $"{White}{colorizedMessage}{Reset}";

                // Fast manual serialization of the AdminLog POCO to avoid IoC/reflection overhead
                // We send both 'message' (clean for search) and 'message_fmt' (colored for display)
                var playerIds = log.Players.Select(p => p.PlayerUserId.ToString()).ToArray();
                var playerIdsJson = JsonSerializer.Serialize(playerIds);
                var jsonLine = $"{{\"id\":{log.Id},\"roundId\":{log.RoundId},\"type\":{(int)log.Type},\"impact\":{(int)log.Impact},\"date\":\"{log.Date:O}\",\"message\":{JsonSerializer.Serialize(log.Message)},\"message_fmt\":{JsonSerializer.Serialize(formattedMessage)},\"json\":{log.Json.RootElement.GetRawText()},\"playerIds\":{playerIdsJson}}}";

                stream.Values.Add(new[] { nanoTime.ToString(), jsonLine });
            }

            var req = new LokiPushRequest();
            req.Streams.Add(stream);

            var content = new System.Net.Http.StringContent(JsonSerializer.Serialize(req), System.Text.Encoding.UTF8, "application/json");
            var resp = await _httpClient.PostAsync($"{_lokiUrl}/loki/api/v1/push", content);
            if (!resp.IsSuccessStatusCode)
            {
                var err = await resp.Content.ReadAsStringAsync();
                _sawmill.Error($"Failed to push admin logs to Loki: {resp.StatusCode} {err}");
            }
        }
        catch (Exception e)
        {
            _sawmill.Error($"Exception pushing admin logs to Loki: {e}");
        }
    }

    private async Task GetAdminLogsFromLoki(LogFilter? filter, List<SharedAdminLog> list)
    {
        var limit = filter?.Limit ?? 1000;
        if (string.IsNullOrEmpty(_lokiUrl)) return;

        var query = $"{{app=\"{_lokiName}\", category=\"admin_log\"}} | json";
        if (filter != null)
        {
            var filters = new List<string>();
            if (filter.Round != null) filters.Add($"roundId=\"{filter.Round.Value}\"");
            if (filter.Types != null && filter.Types.Count > 0)
            {
                var types = string.Join("|", filter.Types.Select(t => (int)t));
                filters.Add($"type=~\"{types}\"");
            }
            if (filter.Impacts != null && filter.Impacts.Count > 0)
            {
                var impacts = string.Join("|", filter.Impacts.Select(i => (int)i));
                filters.Add($"impact=~\"{impacts}\"");
            }
            if (filters.Count > 0)
                query += " | " + string.Join(" | ", filters);

            if (!string.IsNullOrEmpty(filter.Search))
            {
                query += $" |~ \"(?i){Regex.Escape(filter.Search)}\"";
            }
        }

        var start = filter?.After != null ? new DateTimeOffset(filter.After.Value).ToUnixTimeMilliseconds() * 1000000 : DateTimeOffset.UtcNow.AddDays(-30).ToUnixTimeMilliseconds() * 1000000;
        var end = filter?.Before != null ? new DateTimeOffset(filter.Before.Value).ToUnixTimeMilliseconds() * 1000000 : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000000;

        if (filter?.Round != null && filter.After == null && filter.Before == null)
        {
            try
            {
                var round = await _db.GetRound(filter.Round.Value);
                if (round.StartDate != null)
                {
                    // Precise 49-hour window around the start of the round
                    start = new DateTimeOffset(round.StartDate.Value).AddHours(-1).ToUnixTimeMilliseconds() * 1000000;
                    end = new DateTimeOffset(round.StartDate.Value).AddHours(48).ToUnixTimeMilliseconds() * 1000000;

                    var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000000;
                    if (end > now) end = now;
                }
            }
            catch
            {
                // Round metadata might be missing in DB, fallback to default range
            }
        }

        var dir = (filter?.DateOrder ?? DateOrder.Descending) == DateOrder.Ascending ? "FORWARD" : "BACKWARD";
        var url = $"{_lokiUrl}/loki/api/v1/query_range?query={Uri.EscapeDataString(query)}&limit={limit}&start={start}&end={end}&direction={dir}";

        var cancel = filter?.CancellationToken ?? default;
        try
        {
            var resp = await _httpClient.GetAsync(url, cancel);
            if (!resp.IsSuccessStatusCode)
            {
                _sawmill.Error($"Loki query failed: {resp.StatusCode}");
                return;
            }

            var content = await resp.Content.ReadAsStringAsync(cancel);
            var lokiResp = JsonSerializer.Deserialize<LokiQueryResponse>(content);
            if (lokiResp?.Data?.Result != null)
            {
                var allValues = new List<string[]>();
                foreach (var stream in lokiResp.Data.Result)
                {
                    allValues.AddRange(stream.Values);
                }

                if (dir == "FORWARD")
                    allValues.Sort((a, b) => string.CompareOrdinal(a[0], b[0]));
                else
                    allValues.Sort((a, b) => string.CompareOrdinal(b[0], a[0]));

                if (allValues.Count > limit)
                    allValues = allValues.GetRange(0, limit);

                foreach (var val in allValues)
                {
                    var token = JsonDocument.Parse(val[1]).RootElement;
                    var id = token.GetProperty("id").GetInt32();
                    var type = (LogType)token.GetProperty("type").GetInt32();
                    var impact = (LogImpact)token.GetProperty("impact").GetInt32();
                    var date = token.GetProperty("date").GetDateTime();
                    var message = token.GetProperty("message").GetString() ?? "";

                    var playersList = new List<Guid>();
                    if (token.TryGetProperty("playerIds", out var playersProp) && playersProp.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var p in playersProp.EnumerateArray())
                        {
                            if (Guid.TryParse(p.GetString(), out var guid))
                            {
                                playersList.Add(guid);
                            }
                        }
                    }
                    else if (token.TryGetProperty("Players", out var oldPlayersProp) && oldPlayersProp.ValueKind == JsonValueKind.Array)
                    {
                        // Fallback for older logs if they existed
                        foreach (var p in oldPlayersProp.EnumerateArray())
                        {
                            if (p.TryGetProperty("PlayerUserId", out var puid) && Guid.TryParse(puid.GetString(), out var guid))
                            {
                                playersList.Add(guid);
                            }
                        }
                    }

                    if (filter != null)
                    {
                        if (filter.LastLogId != null)
                        {
                            if (dir == "FORWARD" && id <= filter.LastLogId.Value) continue;
                            if (dir == "BACKWARD" && id >= filter.LastLogId.Value) continue;
                        }

                        if (filter.AnyPlayers != null && filter.AnyPlayers.Length > 0)
                        {
                            if (!playersList.Any(p => filter.AnyPlayers.Contains(p))) continue;
                        }
                        if (filter.AllPlayers != null && filter.AllPlayers.Length > 0)
                        {
                            if (!filter.AllPlayers.All(p => playersList.Contains(p))) continue;
                        }
                    }

                    var shared = new SharedAdminLog(id, type, impact, date, message, playersList.ToArray());
                    list.Add(shared);
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _sawmill.Error($"Error querying logs from Loki: {ex}");
        }
    }

    public async IAsyncEnumerable<string> AllMessages(LogFilter? filter = null)
    {
        if (_lokiEnabled)
        {
            var list = new List<SharedAdminLog>();
            await GetAdminLogsFromLoki(filter, list);
            foreach (var l in list) yield return l.Message;
        }
        else
        {
            await foreach (var message in _db.GetAdminLogMessages(filter)) yield return message;
        }
    }

    public async IAsyncEnumerable<JsonDocument> AllJson(LogFilter? filter = null)
    {
        if (_lokiEnabled)
        {
            yield break;
        }
        else
        {
            await foreach (var json in _db.GetAdminLogsJson(filter)) yield return json;
        }
    }
    // Sunrise-End

    public Task<Round> Round(int roundId)
    {
        return _db.GetRound(roundId);
    }

    public Task<List<SharedAdminLog>> CurrentRoundLogs(LogFilter? filter = null)
    {
        filter ??= new LogFilter();
        filter.Round = _currentRoundId;
        return All(filter);
    }

    public IAsyncEnumerable<string> CurrentRoundMessages(LogFilter? filter = null)
    {
        filter ??= new LogFilter();
        filter.Round = _currentRoundId;
        return AllMessages(filter);
    }

    public IAsyncEnumerable<JsonDocument> CurrentRoundJson(LogFilter? filter = null)
    {
        filter ??= new LogFilter();
        filter.Round = _currentRoundId;
        return AllJson(filter);
    }

    public Task<Round> CurrentRound()
    {
        return Round(_currentRoundId);
    }

    // Sunrise-Start
    public async Task<int> CountLogs(int round)
    {
        var count = await _db.CountAdminLogs(round);
        if (_lokiEnabled && count == 0) return 1000;
        return count;
    }
    // Sunrise-End
}

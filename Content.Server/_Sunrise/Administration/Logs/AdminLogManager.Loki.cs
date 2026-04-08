#pragma warning disable IDE0130 // Namespace does not match folder structure

using System.Net.Http;
using System.Net.Http.Headers;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared.Administration.Logs;
using Content.Shared.CCVar;
using Content.Shared.Database;

namespace Content.Server.Administration.Logs;

public sealed partial class AdminLogManager
{
    // Loki-specific logging and integration for AdminLogManager.
    private static readonly Regex LogHighlightRegex = new(
        @"(\bEntId=\d+\b|(?<=\()(\b\d+/[^\)]+|\bn\d+\b)(?=\)))|\b(dropped|picked up|inserted|removed|equipped|unequipped|thrown|wielded|unwielded|loaded|unloaded|spawned|deleted|shot|attacked|damaged|hit|exploded|fired|clicked|knocked down|activated|interacted|opened|closed|locked|unlocked|anchored|unanchored|welded|unwelded|bolted|unbolted|connected|disconnected|joined|left|banned|kicked|suicided|died|revived|cloned|respawned|joined|left|refilled|drained|poured|ingested|vomited|collapsed|unconscious|rejuvenated|mounted|dismounted)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private const string Gray = "\u001b[90m";
    private const string Cyan = "\u001b[36m";
    private const string Reset = "\u001b[0m";
    private const string White = "\u001b[37m";

    private bool _lokiEnabled;
    private string _lokiUrl = string.Empty;
    private string _lokiUsername = string.Empty;
    private string _lokiPassword = string.Empty;
    private string _lokiName = "unknown";
    private readonly HttpClient _httpClient = new();

    private readonly record struct LokiTimeRange(DateTimeOffset Start, DateTimeOffset End, bool RoundStartKnown);
    private readonly record struct ParsedLokiLog(SharedAdminLog Log, Guid[] Players, long Timestamp, int RoundId);
    private readonly record struct LokiCursor(long Timestamp, int RoundId, int LogId);

    private void InitializeLokiConfiguration()
    {
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
    }

    private void SetLokiAuth()
    {
        _httpClient.DefaultRequestHeaders.Authorization = null;
        if (string.IsNullOrEmpty(_lokiUsername) || string.IsNullOrEmpty(_lokiPassword))
            return;

        var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_lokiUsername}:{_lokiPassword}"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authString);
    }

    private void ShutdownLoki()
    {
        _httpClient.Dispose();
    }

    private async Task SaveLogsToLoki(List<AdminLog> logs)
    {
        if (logs.Count == 0 || string.IsNullOrEmpty(_lokiUrl))
            return;

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
                var nanoTime = ToUnixTimeNanoseconds(new DateTimeOffset(log.Date));
                if (nanoTime <= lastNano)
                    nanoTime = lastNano + 1;

                lastNano = nanoTime;

                var colorizedMessage = LogHighlightRegex.Replace(log.Message, match =>
                {
                    var isNumericToken = match.Value.Length > 0 &&
                        (char.IsDigit(match.Value[0]) ||
                         (char.ToLowerInvariant(match.Value[0]) == 'n' &&
                          match.Value.Length > 1 &&
                          char.IsDigit(match.Value[1])));

                    if (match.Value.Contains('=') || isNumericToken)
                        return $"{Gray}{match.Value}{White}";

                    return $"{Cyan}{match.Value}{White}";
                });

                var formattedMessage = $"{White}{colorizedMessage}{Reset}";
                var playerIds = log.Players.Select(player => player.PlayerUserId.ToString()).ToArray();
                var playerIdsJson = JsonSerializer.Serialize(playerIds);
                var jsonLine =
                    $"{{\"id\":{log.Id},\"roundId\":{log.RoundId},\"type\":{(int) log.Type},\"impact\":{(int) log.Impact},\"date\":\"{log.Date:O}\",\"message\":{JsonSerializer.Serialize(log.Message)},\"message_fmt\":{JsonSerializer.Serialize(formattedMessage)},\"json\":{log.Json.RootElement.GetRawText()},\"playerIds\":{playerIdsJson}}}";

                stream.Values.Add([nanoTime.ToString(), jsonLine]);
            }

            var request = new LokiPushRequest();
            request.Streams.Add(stream);

            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_lokiUrl}/loki/api/v1/push", content);
            if (response.IsSuccessStatusCode)
                return;

            var error = await response.Content.ReadAsStringAsync();
            _sawmill.Error($"Failed to push admin logs to Loki: {response.StatusCode} {error}");
        }
        catch (Exception e)
        {
            _sawmill.Error($"Exception pushing admin logs to Loki: {e}");
        }
    }

    private async Task GetAdminLogsFromLoki(LogFilter? filter, List<SharedAdminLog> list)
    {
        if (string.IsNullOrEmpty(_lokiUrl))
            return;

        var requestedLimit = filter?.Limit ?? 1000;
        if (requestedLimit <= 0)
            return;

        var cancel = filter?.CancellationToken ?? default;
        var ascending = (filter?.DateOrder ?? DateOrder.Descending) == DateOrder.Ascending;
        var timeRange = await ResolveLokiTimeRange(filter);
        var canUseCursorInQuery = CanUseLokiCursorInQuery(filter);
        var requiresAdditionalPages = RequiresAdditionalLokiPages(filter);
        var cursor = CanUseLokiCursorInQuery(filter) && TryParseLokiCursor(filter?.LastLogCursor, out var startCursor)
            ? startCursor
            : (LokiCursor?)null;
        if (filter != null)
            filter.LastLogCursor = cursor.HasValue ? FormatLokiCursor(cursor.Value) : null;
        if (cursor != null && canUseCursorInQuery)
            timeRange = MoveTimeRangeCursorForward(timeRange, cursor.Value, ascending);

        var firstQuery = true;

        while (list.Count < requestedLimit)
        {
            var batchLimit = requestedLimit - list.Count;
            var query = BuildLokiQuery(filter, ascending);
            var url = BuildLokiQueryUrl(query, batchLimit, timeRange, ascending);

            try
            {
                var response = await _httpClient.GetAsync(url, cancel);
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(cancel);
                    _sawmill.Error($"Loki query failed: {response.StatusCode} {body}");
                    return;
                }

                var content = await response.Content.ReadAsStringAsync(cancel);
                var lokiResponse = JsonSerializer.Deserialize<LokiQueryResponse>(content);
                var rawValues = FlattenLokiValues(lokiResponse, ascending, batchLimit);

                if (rawValues.Count == 0)
                {
                    LogLokiZeroResult(filter, timeRange, query, ascending, batchLimit, cursor, firstQuery, list.Count);
                    return;
                }

                var nextCursor = cursor;
                foreach (var value in rawValues)
                {
                    var timestamp = ParseLokiTimestamp(value[0]);
                    if (!timestamp.HasValue)
                        continue;

                    if (!TryParseLokiLog(value, timestamp.Value, out var parsed))
                        continue;

                    nextCursor = new LokiCursor(timestamp.Value, parsed.RoundId, parsed.Log.Id);

                    if (filter != null && !MatchesLokiPostFilter(filter, parsed, cursor, ascending))
                        continue;

                    list.Add(parsed.Log);
                    if (list.Count >= requestedLimit)
                        break;
                }

                if (filter != null && nextCursor != null)
                    filter.LastLogCursor = FormatLokiCursor(nextCursor.Value);

                if (!requiresAdditionalPages || !canUseCursorInQuery)
                    return;

                if (!nextCursor.HasValue || nextCursor == cursor || rawValues.Count < batchLimit)
                    return;

                cursor = nextCursor;
                if (filter != null)
                    filter.LastLogCursor = FormatLokiCursor(cursor.Value);
                timeRange = MoveTimeRangeCursorForward(timeRange, cursor.Value, ascending);
                firstQuery = false;
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                _sawmill.Error($"Error querying logs from Loki: {ex}");
                return;
            }
        }
    }

    private async Task<LokiTimeRange> ResolveLokiTimeRange(LogFilter? filter)
    {
        var now = DateTimeOffset.UtcNow;
        if (filter?.After != null || filter?.Before != null)
        {
            var explicitStart = filter?.After != null
                ? new DateTimeOffset(filter.After.Value)
                : now.AddDays(-30);
            var explicitEnd = filter?.Before != null
                ? new DateTimeOffset(filter.Before.Value)
                : now;

            return new LokiTimeRange(explicitStart, explicitEnd, false);
        }

        if (filter?.Round == null)
            return new LokiTimeRange(now.AddDays(-30), now, false);

        try
        {
            var round = await _db.GetRound(filter.Round.Value);
            if (round.StartDate != null)
            {
                var roundStart = new DateTimeOffset(AssumeUtc(round.StartDate.Value));
                return new LokiTimeRange(roundStart.AddHours(-1), now, true);
            }
        }
        catch (Exception ex)
        {
            _sawmill.Warning($"Failed loading round metadata for Loki query window for round {filter.Round.Value}: {ex}");
        }

        return new LokiTimeRange(DateTimeOffset.UnixEpoch, now, false);
    }

    private string BuildLokiQuery(LogFilter? filter, bool ascending)
    {
        var query = $"{{app=\"{_lokiName}\", category=\"admin_log\"}} | json";

        if (filter != null)
        {
            if (filter.Round != null)
                query += $" | roundId=\"{filter.Round.Value}\"";

            if (filter.Types is { Count: > 0 })
            {
                var types = string.Join("|", filter.Types.Select(type => (int) type));
                query += $" | type=~\"{types}\"";
            }

            if (filter.Impacts is { Count: > 0 })
            {
                var impacts = string.Join("|", filter.Impacts.Select(impact => (int) impact));
                query += $" | impact=~\"{impacts}\"";
            }
        }

        if (!string.IsNullOrEmpty(filter?.Search))
            query += $" |~ \"(?i){Regex.Escape(filter.Search)}\"";

        return query;
    }

    private string BuildLokiQueryUrl(string query, int limit, LokiTimeRange timeRange, bool ascending)
    {
        var direction = ascending ? "forward" : "backward";
        var start = ToUnixTimeNanoseconds(timeRange.Start);
        var end = ToUnixTimeNanoseconds(timeRange.End);
        return $"{_lokiUrl}/loki/api/v1/query_range?query={Uri.EscapeDataString(query)}&limit={limit}&start={start}&end={end}&direction={direction}";
    }

    private static bool CanUseLokiCursorInQuery(LogFilter? filter)
    {
        return filter != null;
    }

    private static bool RequiresAdditionalLokiPages(LogFilter? filter)
    {
        return filter != null && (!filter.IncludePlayers || filter.AnyPlayers != null || filter.AllPlayers != null);
    }

    private static List<string[]> FlattenLokiValues(LokiQueryResponse? response, bool ascending, int limit)
    {
        if (response?.Data?.Result == null)
            return [];

        var values = new List<string[]>();
        foreach (var stream in response.Data.Result)
        {
            values.AddRange(stream.Values);
        }

        values.Sort((left, right) =>
        {
            var leftTimestamp = ParseLokiTimestamp(left[0]);
            var rightTimestamp = ParseLokiTimestamp(right[0]);
            var compare = ascending
                ? leftTimestamp.GetValueOrDefault().CompareTo(rightTimestamp.GetValueOrDefault())
                : rightTimestamp.GetValueOrDefault().CompareTo(leftTimestamp.GetValueOrDefault());

            if (compare != 0)
                return compare;

            return string.CompareOrdinal(left[0], right[0]);
        });

        if (values.Count > limit)
            values.RemoveRange(limit, values.Count - limit);

        return values;
    }

    private static bool TryParseLokiLog(string[] value, long timestamp, out ParsedLokiLog parsed)
    {
        parsed = default;
        if (value.Length < 2)
            return false;

        try
        {
            using var document = JsonDocument.Parse(value[1]);
            var token = document.RootElement;

            var id = token.GetProperty("id").GetInt32();
            var roundId = token.TryGetProperty("roundId", out var roundIdToken) && roundIdToken.TryGetInt32(out var parsedRoundId)
                ? parsedRoundId
                : 0;
            var type = (LogType) token.GetProperty("type").GetInt32();
            var impact = (LogImpact) token.GetProperty("impact").GetInt32();
            var date = token.GetProperty("date").GetDateTime();
            var message = token.GetProperty("message").GetString() ?? string.Empty;
            var players = ParseLokiPlayers(token);

            parsed = new ParsedLokiLog(new SharedAdminLog(id, type, impact, date, message, players), players, timestamp, roundId);
            return true;
        }
        catch (JsonException)
        {
            parsed = default;
            return false;
        }
        catch (FormatException)
        {
            parsed = default;
            return false;
        }
        catch (InvalidOperationException)
        {
            parsed = default;
            return false;
        }
        catch (Exception)
        {
            parsed = default;
            return false;
        }
    }

    private static Guid[] ParseLokiPlayers(JsonElement token)
    {
        var players = new List<Guid>();
        if (token.TryGetProperty("playerIds", out var playerIds) && playerIds.ValueKind == JsonValueKind.Array)
        {
            foreach (var playerId in playerIds.EnumerateArray())
            {
                if (Guid.TryParse(playerId.GetString(), out var guid))
                    players.Add(guid);
            }

            return players.ToArray();
        }

        if (token.TryGetProperty("Players", out var oldPlayers) && oldPlayers.ValueKind == JsonValueKind.Array)
        {
            foreach (var player in oldPlayers.EnumerateArray())
            {
                if (player.TryGetProperty("PlayerUserId", out var playerId) && Guid.TryParse(playerId.GetString(), out var guid))
                    players.Add(guid);
            }
        }

        return players.ToArray();
    }

    private static bool MatchesLokiPostFilter(LogFilter filter, ParsedLokiLog log, LokiCursor? cursor, bool ascending)
    {
        if (cursor != null)
        {
            if (ascending && !IsCursorBefore(log, cursor.Value))
                return false;

            if (!ascending && !IsCursorAfter(log, cursor.Value))
                return false;
        }
        else if (filter.LastLogId != null)
        {
            if (ascending && log.Log.Id <= filter.LastLogId.Value)
                return false;

            if (!ascending && log.Log.Id >= filter.LastLogId.Value)
                return false;
        }

        return MatchesLokiPlayerFilters(filter, log.Players);
    }

    private static long? ParseLokiTimestamp(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        if (long.TryParse(value, out var timestamp))
            return timestamp;

        return null;
    }

    private static bool IsCursorBefore(ParsedLokiLog log, LokiCursor cursor)
    {
        if (log.Timestamp != cursor.Timestamp)
            return log.Timestamp > cursor.Timestamp;

        if (log.RoundId != cursor.RoundId)
            return log.RoundId > cursor.RoundId;

        return log.Log.Id > cursor.LogId;
    }

    private static bool IsCursorAfter(ParsedLokiLog log, LokiCursor cursor)
    {
        if (log.Timestamp != cursor.Timestamp)
            return log.Timestamp < cursor.Timestamp;

        if (log.RoundId != cursor.RoundId)
            return log.RoundId < cursor.RoundId;

        return log.Log.Id < cursor.LogId;
    }

    private static bool TryParseLokiCursor(string? cursor, out LokiCursor parsedCursor)
    {
        parsedCursor = default;
        if (string.IsNullOrEmpty(cursor))
            return false;

        var parts = cursor.Split('|');
        if (parts.Length != 3)
            return false;

        if (!long.TryParse(parts[0], out var timestamp))
            return false;

        if (!int.TryParse(parts[1], out var roundId))
            return false;

        if (!int.TryParse(parts[2], out var logId))
            return false;

        parsedCursor = new LokiCursor(timestamp, roundId, logId);
        return true;
    }

    private static string FormatLokiCursor(LokiCursor cursor)
    {
        return $"{cursor.Timestamp}|{cursor.RoundId}|{cursor.LogId}";
    }

    private static LokiTimeRange MoveTimeRangeCursorForward(LokiTimeRange timeRange, LokiCursor cursor, bool ascending)
    {
        var cursorTime = FromUnixTimeNanoseconds(cursor.Timestamp);

        return ascending
            ? timeRange with { Start = cursorTime }
            : timeRange with { End = cursorTime };
    }

    private static DateTimeOffset FromUnixTimeNanoseconds(long timestamp)
    {
        var timeInMilliseconds = timestamp / 1_000_000;
        var ticks = (timestamp % 1_000_000) / 100;
        return DateTimeOffset.FromUnixTimeMilliseconds(timeInMilliseconds).AddTicks(ticks);
    }

    private static bool MatchesLokiPlayerFilters(LogFilter filter, Guid[] players)
    {
        if (!filter.IncludePlayers)
            return players.Length == 0;

        if (filter.AnyPlayers != null)
        {
            var anyMatched = players.Any(player => filter.AnyPlayers.Contains(player));
            if (!(anyMatched || players.Length == 0 && filter.IncludeNonPlayers))
                return false;
        }

        if (filter.AllPlayers != null)
        {
            var allMatched = filter.AllPlayers.All(player => players.Contains(player));
            if (!(allMatched || players.Length == 0 && filter.IncludeNonPlayers))
                return false;
        }

        return true;
    }

    private void LogLokiZeroResult(
        LogFilter? filter,
        LokiTimeRange timeRange,
        string query,
        bool ascending,
        int limit,
        LokiCursor? cursor,
        bool firstQuery,
        int visibleLogs)
    {
        var direction = ascending ? "forward" : "backward";
        _sawmill.Debug(
            $"Loki query returned 0 admin logs. round={filter?.Round?.ToString() ?? "null"} direction={direction} limit={limit} cursor={cursor?.ToString() ?? "null"} roundStartKnown={timeRange.RoundStartKnown} firstQuery={firstQuery} visible={visibleLogs} start={timeRange.Start:O} end={timeRange.End:O} query={query}");
    }

    private static DateTime AssumeUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }

    private static long ToUnixTimeNanoseconds(DateTimeOffset value)
    {
        return value.ToUnixTimeMilliseconds() * 1_000_000;
    }
}

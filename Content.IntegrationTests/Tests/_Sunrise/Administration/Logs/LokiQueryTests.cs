using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Web;
using Content.Server.Administration.Logs;
using Content.Server.Database;
using Content.Shared.Administration.Logs;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Microsoft.EntityFrameworkCore;
using Robust.Shared.Configuration;
using Robust.UnitTesting;

namespace Content.IntegrationTests.Tests._Sunrise.Administration.Logs;

[TestFixture]
[TestOf(typeof(AdminLogSystem))]
public sealed class LokiQueryTests
{
    private static readonly PoolSettings LokiTestSettings = new()
    {
        AdminLogsEnabled = true,
    };

    [Test]
    public async Task RoundScopedQuery_UsesRoundStartWindow()
    {
        await using var fakeLoki = new FakeLokiServer(_ => CreateLokiResponse());
        await using var pair = await PoolManager.GetServerClient(LokiTestSettings);
        var server = pair.Server;

        var roundId = await CreateRoundAsync(server, "loki-range-start");
        var db = server.ResolveDependency<IServerDbManager>();
        var round = await db.GetRound(roundId);
        var expectedStart = ToUnixTimeNanoseconds(new DateTimeOffset(AssumeUtc(round.StartDate!.Value)).AddHours(-1));

        await ConfigureLokiAsync(server, fakeLoki.Url);

        var adminLogs = server.ResolveDependency<IAdminLogManager>();
        _ = await adminLogs.All(new LogFilter { Round = roundId, Limit = 1 });

        var request = fakeLoki.Requests.Single();
        Assert.That(request.Start, Is.EqualTo(expectedStart));
        Assert.That(request.Query, Does.Contain($"roundId=\"{roundId}\""));
        Assert.That(request.Direction, Is.EqualTo("backward"));

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task RoundScopedQuery_WithoutRoundStart_UsesUnixEpoch()
    {
        await using var fakeLoki = new FakeLokiServer(_ => CreateLokiResponse());
        await using var pair = await PoolManager.GetServerClient(LokiTestSettings);
        var server = pair.Server;

        var roundId = await CreateRoundAsync(server, "loki-range-null");
        await SetRoundStartDateAsync(server, roundId, null);
        await ConfigureLokiAsync(server, fakeLoki.Url);

        var adminLogs = server.ResolveDependency<IAdminLogManager>();
        _ = await adminLogs.All(new LogFilter { Round = roundId, Limit = 1 });

        var request = fakeLoki.Requests.Single();
        Assert.That(request.Start, Is.EqualTo(0));
        Assert.That(request.Query, Does.Contain($"roundId=\"{roundId}\""));

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task Pagination_UsesCursorAndDoesNotRepeatAscending()
    {
        await Pagination_UsesCursorAndDoesNotRepeat(DateOrder.Ascending);
    }

    [Test]
    public async Task Pagination_UsesCursorAndDoesNotRepeatDescending()
    {
        await Pagination_UsesCursorAndDoesNotRepeat(DateOrder.Descending);
    }

    [Test]
    public async Task GlobalPagination_UsesCursorAndDoesNotRepeatAscending()
    {
        await Pagination_UsesCursorAndDoesNotRepeat(DateOrder.Ascending, roundScoped: false);
    }

    [Test]
    public async Task GlobalPagination_UsesCursorAndDoesNotRepeatDescending()
    {
        await Pagination_UsesCursorAndDoesNotRepeat(DateOrder.Descending, roundScoped: false);
    }

    private async Task Pagination_UsesCursorAndDoesNotRepeat(DateOrder order, bool roundScoped = true)
    {
        await using var fakeLoki = new FakeLokiServer(request =>
        {
            if (order == DateOrder.Ascending)
            {
                return request.Query.Contains("id > 2")
                    ? CreateLokiResponse(CreateEntry(300, 3), CreateEntry(400, 4))
                    : CreateLokiResponse(CreateEntry(100, 1), CreateEntry(200, 2));
            }

            return request.Query.Contains("id < 3")
                ? CreateLokiResponse(CreateEntry(200, 2), CreateEntry(100, 1))
                : CreateLokiResponse(CreateEntry(400, 4), CreateEntry(300, 3));
        });

        await using var pair = await PoolManager.GetServerClient(LokiTestSettings);
        var server = pair.Server;
        int? roundId = null;
        if (roundScoped)
            roundId = await CreateRoundAsync(server, $"loki-pagination-{order}");

        await ConfigureLokiAsync(server, fakeLoki.Url);

        var adminLogs = server.ResolveDependency<IAdminLogManager>();
        var pageOne = await adminLogs.All(new LogFilter
        {
            Round = roundId,
            Limit = 2,
            DateOrder = order
        });

        var pageTwo = await adminLogs.All(new LogFilter
        {
            Round = roundId,
            Limit = 2,
            DateOrder = order,
            LastLogId = pageOne[^1].Id
        });

        var expectedFirst = order == DateOrder.Ascending ? new[] { 1, 2 } : new[] { 4, 3 };
        var expectedSecond = order == DateOrder.Ascending ? new[] { 3, 4 } : new[] { 2, 1 };

        Assert.That(pageOne.Select(log => log.Id).ToArray(), Is.EqualTo(expectedFirst));
        Assert.That(pageTwo.Select(log => log.Id).ToArray(), Is.EqualTo(expectedSecond));
        Assert.That(fakeLoki.Requests.Count, Is.EqualTo(2));
        Assert.That(fakeLoki.Requests.Last().Query, Does.Contain(order == DateOrder.Ascending ? "id > 2" : "id < 3"));

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task AnyPlayersFilter_FetchesAdditionalPagesUntilBatchIsFilled()
    {
        await AnyPlayersFilter_FetchesAdditionalPagesUntilBatchIsFilled(roundScoped: true);
    }

    [Test]
    public async Task AnyPlayersFilter_WithoutRound_FetchesAdditionalPagesUntilBatchIsFilled()
    {
        await AnyPlayersFilter_FetchesAdditionalPagesUntilBatchIsFilled(roundScoped: false);
    }

    private async Task AnyPlayersFilter_FetchesAdditionalPagesUntilBatchIsFilled(bool roundScoped)
    {
        var targetPlayer = Guid.NewGuid();
        var otherPlayer = Guid.NewGuid();

        await using var fakeLoki = new FakeLokiServer(request =>
        {
            return request.Query.Contains("id < 3")
                ? CreateLokiResponse(CreateEntry(200, 2, targetPlayer), CreateEntry(100, 1, targetPlayer))
                : CreateLokiResponse(CreateEntry(400, 4, otherPlayer), CreateEntry(300, 3, otherPlayer));
        });

        await using var pair = await PoolManager.GetServerClient(LokiTestSettings);
        var server = pair.Server;
        int? roundId = null;
        if (roundScoped)
            roundId = await CreateRoundAsync(server, "loki-player-fill");

        await ConfigureLokiAsync(server, fakeLoki.Url);

        var adminLogs = server.ResolveDependency<IAdminLogManager>();
        var logs = await adminLogs.All(new LogFilter
        {
            Round = roundId,
            Limit = 2,
            DateOrder = DateOrder.Descending,
            AnyPlayers = [targetPlayer]
        });

        Assert.That(logs.Select(log => log.Id).ToArray(), Is.EqualTo(new[] { 2, 1 }));
        Assert.That(fakeLoki.Requests.Count, Is.EqualTo(2));
        Assert.That(fakeLoki.Requests.Last().Query, Does.Contain("id < 3"));

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task MalformedLokiEntry_DoesNotAbortBatch()
    {
        await using var fakeLoki = new FakeLokiServer(_ => CreateRawLokiResponse(
            new[] { "300", "{bad json" },
            new[]
            {
                "200",
                JsonSerializer.Serialize(new
                {
                    id = 2,
                    type = (int) LogType.Unknown,
                    impact = (int) LogImpact.Medium,
                    date = DateTimeOffset.UnixEpoch.AddSeconds(2).UtcDateTime.ToString("O"),
                    message = "log-2",
                    playerIds = Array.Empty<string>()
                })
            },
            new[]
            {
                "100",
                JsonSerializer.Serialize(new
                {
                    id = 1,
                    type = (int) LogType.Unknown,
                    impact = (int) LogImpact.Medium,
                    date = DateTimeOffset.UnixEpoch.AddSeconds(1).UtcDateTime.ToString("O"),
                    message = "log-1",
                    playerIds = Array.Empty<string>()
                })
            }));

        await using var pair = await PoolManager.GetServerClient(LokiTestSettings);
        var server = pair.Server;
        await ConfigureLokiAsync(server, fakeLoki.Url);

        var adminLogs = server.ResolveDependency<IAdminLogManager>();
        var logs = await adminLogs.All(new LogFilter { Limit = 10 });

        Assert.That(logs.Select(log => log.Id).ToArray(), Is.EqualTo(new[] { 2, 1 }));
        Assert.That(fakeLoki.Requests.Count, Is.EqualTo(1));

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task PlayerFilterFlags_MatchDatabaseSemantics()
    {
        var targetPlayer = Guid.NewGuid();
        var otherPlayer = Guid.NewGuid();

        await using var fakeLoki = new FakeLokiServer(_ => CreateLokiResponse(
            CreateEntry(300, 3, targetPlayer),
            CreateEntry(200, 2),
            CreateEntry(100, 1, otherPlayer)));

        await using var pair = await PoolManager.GetServerClient(LokiTestSettings);
        var server = pair.Server;
        var roundId = await CreateRoundAsync(server, "loki-player-flags");
        await ConfigureLokiAsync(server, fakeLoki.Url);

        var adminLogs = server.ResolveDependency<IAdminLogManager>();

        var noPlayersOnly = await adminLogs.All(new LogFilter
        {
            Round = roundId,
            Limit = 10,
            IncludePlayers = false
        });

        var anyPlayersWithoutNonPlayers = await adminLogs.All(new LogFilter
        {
            Round = roundId,
            Limit = 10,
            AnyPlayers = [targetPlayer],
            IncludeNonPlayers = false
        });

        var anyPlayersWithNonPlayers = await adminLogs.All(new LogFilter
        {
            Round = roundId,
            Limit = 10,
            AnyPlayers = [targetPlayer],
            IncludeNonPlayers = true
        });

        Assert.That(noPlayersOnly.Select(log => log.Id).ToArray(), Is.EqualTo(new[] { 2 }));
        Assert.That(anyPlayersWithoutNonPlayers.Select(log => log.Id).ToArray(), Is.EqualTo(new[] { 3 }));
        Assert.That(anyPlayersWithNonPlayers.Select(log => log.Id).ToArray(), Is.EqualTo(new[] { 3, 2 }));

        await pair.CleanReturnAsync();
    }

    private static async Task ConfigureLokiAsync(RobustIntegrationTest.ServerIntegrationInstance server, string url)
    {
        var cfg = server.ResolveDependency<IConfigurationManager>();
        await server.WaitPost(() =>
        {
            cfg.SetCVar(CCVars.AdminLogsToLoki, true);
            cfg.SetCVar(CCVars.AdminLogsLokiUrl, url);
            cfg.SetCVar(CCVars.AdminLogsLokiName, "test-loki");
        });
    }

    private static async Task<int> CreateRoundAsync(RobustIntegrationTest.ServerIntegrationInstance server, string serverName)
    {
        var db = server.ResolveDependency<IServerDbManager>();
        var dbServer = await db.AddOrGetServer(serverName);
        return await db.AddNewRound(dbServer);
    }

    private static async Task SetRoundStartDateAsync(
        RobustIntegrationTest.ServerIntegrationInstance server,
        int roundId,
        DateTime? startDate)
    {
        await using var dbContext = CreateServerDbContext(server);
        var round = await dbContext.Round.SingleAsync(existingRound => existingRound.Id == roundId);
        round.StartDate = startDate;
        await dbContext.SaveChangesAsync();
    }

    private static SqliteServerDbContext CreateServerDbContext(RobustIntegrationTest.ServerIntegrationInstance server)
    {
        // This test helper intentionally reflects private SQLite fields because the production DB API
        // does not expose a stable test context factory.
        var manager = (ServerDbManager) server.ResolveDependency<IServerDbManager>();
        var dbField = typeof(ServerDbManager).GetField("_db", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var sqliteDb = (ServerDbSqlite) dbField.GetValue(manager)!;
        var optionsField = typeof(ServerDbSqlite).GetField("_options", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var optionsFactory = (Func<DbContextOptions<SqliteServerDbContext>>) optionsField.GetValue(sqliteDb)!;
        return new SqliteServerDbContext(optionsFactory());
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

    private static string CreateLokiResponse(params LokiEntry[] entries)
    {
        var values = entries.Select(entry => new[]
        {
            entry.Timestamp.ToString(),
            JsonSerializer.Serialize(new
            {
                id = entry.Id,
                type = (int) LogType.Unknown,
                impact = (int) LogImpact.Medium,
                date = DateTimeOffset.UnixEpoch.AddSeconds(entry.Id).UtcDateTime.ToString("O"),
                message = $"log-{entry.Id}",
                playerIds = entry.Players.Select(player => player.ToString()).ToArray()
            })
        }).ToArray();

        return JsonSerializer.Serialize(new
        {
            status = "success",
            data = new
            {
                resultType = "streams",
                result = entries.Length == 0
                    ? Array.Empty<object>()
                    : new object[]
                    {
                        new
                        {
                            stream = new Dictionary<string, string>
                            {
                                { "app", "test-loki" },
                                { "category", "admin_log" }
                            },
                            values
                        }
                    }
            }
        });
    }

    private static string CreateRawLokiResponse(params string[][] values)
    {
        return JsonSerializer.Serialize(new
        {
            status = "success",
            data = new
            {
                resultType = "streams",
                result = values.Length == 0
                    ? Array.Empty<object>()
                    : new object[]
                    {
                        new
                        {
                            stream = new Dictionary<string, string>
                            {
                                { "app", "test-loki" },
                                { "category", "admin_log" }
                            },
                            values
                        }
                }
            }
        });
    }

    private static LokiEntry CreateEntry(long timestamp, int id, params Guid[] players)
    {
        return new LokiEntry(timestamp, id, players);
    }

    private readonly record struct LokiEntry(long Timestamp, int Id, Guid[] Players);

    private sealed record LokiRequestSnapshot(string Query, int Limit, long Start, long End, string Direction);

    private sealed class FakeLokiServer : IAsyncDisposable
    {
        private readonly HttpListener _listener = new();
        private readonly Func<LokiRequestSnapshot, string> _responseFactory;
        private readonly Task _serveTask;

        public string Url { get; }
        public ConcurrentQueue<LokiRequestSnapshot> Requests { get; } = new();

        public FakeLokiServer(Func<LokiRequestSnapshot, string> responseFactory)
        {
            _responseFactory = responseFactory;

            var port = GetFreePort();
            Url = $"http://127.0.0.1:{port}";
            _listener.Prefixes.Add($"{Url}/");
            try
            {
                _listener.Start();
                _serveTask = Task.Run(ServeAsync);
            }
            catch
            {
                if (_listener.IsListening)
                    _listener.Stop();

                _listener.Close();
                throw;
            }
        }

        public async ValueTask DisposeAsync()
        {
            _listener.Stop();
            _listener.Close();
            await _serveTask;
        }

        private async Task ServeAsync()
        {
            while (_listener.IsListening)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    await HandleRequestAsync(context);
                }
                catch (HttpListenerException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
            }
        }

        private async Task HandleRequestAsync(HttpListenerContext context)
        {
            var request = CreateSnapshot(context.Request);
            Requests.Enqueue(request);

            var body = _responseFactory(request);
            var bytes = Encoding.UTF8.GetBytes(body);
            context.Response.StatusCode = (int) HttpStatusCode.OK;
            context.Response.ContentType = "application/json";
            context.Response.ContentLength64 = bytes.Length;
            await context.Response.OutputStream.WriteAsync(bytes);
            context.Response.Close();
        }

        private static LokiRequestSnapshot CreateSnapshot(HttpListenerRequest request)
        {
            var requestUrl = request.Url?.ToString() ?? "<unknown>";
            var query = HttpUtility.ParseQueryString(request.Url?.Query ?? string.Empty);

            return new LokiRequestSnapshot(
                query["query"] ?? string.Empty,
                ParseIntQueryValue("limit"),
                ParseLongQueryValue("start"),
                ParseLongQueryValue("end"),
                query["direction"] ?? string.Empty);

            int ParseIntQueryValue(string key)
            {
                var rawValue = query[key];
                if (int.TryParse(rawValue, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var value))
                    return value;

                throw new InvalidOperationException(
                    $"Fake Loki request '{requestUrl}' contained invalid '{key}' query value '{rawValue ?? "<null>"}'.");
            }

            long ParseLongQueryValue(string key)
            {
                var rawValue = query[key];
                if (long.TryParse(rawValue, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var value))
                    return value;

                throw new InvalidOperationException(
                    $"Fake Loki request '{requestUrl}' contained invalid '{key}' query value '{rawValue ?? "<null>"}'.");
            }
        }

        private static int GetFreePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint) listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }
    }
}

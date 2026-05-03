using System.Threading;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;

namespace Content.Server.Administration.Logs;

public sealed class LogFilter
{
    public CancellationToken CancellationToken { get; set; }

    public int? Round { get; set; }

    public string? Search { get; set; }

    public HashSet<LogType>? Types { get; set; }

    public HashSet<LogImpact>? Impacts { get; set; }

    public DateTime? Before { get; set; }

    public DateTime? After { get; set; }

    public bool IncludePlayers  { get; set; } = true;

    public Guid[]? AnyPlayers { get; set; }

    public Guid[]? AllPlayers { get; set; }

    public bool IncludeNonPlayers { get; set; }

    public int? LastLogId { get; set; }

    public string? LastLogCursor { get; set; }

    // Sunrise edit start - indicate overfetch count used for page-level cursor updates
    public int LokiCursorOverfetch { get; set; }
    // Sunrise edit end

    public int LogsSent { get; set; }

    public int? Limit { get; set; }

    public DateOrder DateOrder { get; set; } = DateOrder.Descending;
}

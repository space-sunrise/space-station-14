using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    ///     Controls if admin logs are enabled. Highly recommended to shut this off for development.
    /// </summary>
    public static readonly CVarDef<bool> AdminLogsEnabled =
        CVarDef.Create("adminlogs.enabled", true, CVar.SERVERONLY);

    public static readonly CVarDef<float> AdminLogsQueueSendDelay =
        CVarDef.Create("adminlogs.queue_send_delay_seconds", 5f, CVar.SERVERONLY);

    /// <summary>
    ///     When to skip the waiting time to save in-round admin logs, if no admin logs are currently being saved
    /// </summary>
    public static readonly CVarDef<int> AdminLogsQueueMax =
        CVarDef.Create("adminlogs.queue_max", 5000, CVar.SERVERONLY);

    /// <summary>
    ///     When to skip the waiting time to save pre-round admin logs, if no admin logs are currently being saved
    /// </summary>
    public static readonly CVarDef<int> AdminLogsPreRoundQueueMax =
        CVarDef.Create("adminlogs.pre_round_queue_max", 5000, CVar.SERVERONLY);

    /// <summary>
    ///     When to start dropping logs
    /// </summary>
    public static readonly CVarDef<int> AdminLogsDropThreshold =
        CVarDef.Create("adminlogs.drop_threshold", 20000, CVar.SERVERONLY);

    /// <summary>
    ///     How many logs to send to the client at once
    /// </summary>
    public static readonly CVarDef<int> AdminLogsClientBatchSize =
        CVarDef.Create("adminlogs.client_batch_size", 1000, CVar.SERVERONLY);

    public static readonly CVarDef<string> AdminLogsServerName =
        CVarDef.Create("adminlogs.server_name", "unknown", CVar.SERVERONLY);

    /// <summary>
    /// Any session below this playtime will send an admin alert whenever they cause a LogImpact.High log.
    /// Set to -1 to disable.
    /// </summary>
    public static readonly CVarDef<int> AdminLogsHighLogPlaytime =
        CVarDef.Create("adminlogs.high_log_playtime", 5, CVar.SERVERONLY);

    // Sunrise-Start
    /// <summary>
    ///     If true, admin logs will be sent to and read from a Loki instance instead of PostgreSQL.
    ///     Useful for preventing database bloat.
    /// </summary>
    public static readonly CVarDef<bool> AdminLogsToLoki =
        CVarDef.Create("adminlogs.to_loki", false, CVar.SERVERONLY);

    /// <summary>
    ///     The Loki API URL (e.g. http://localhost:3100)
    /// </summary>
    public static readonly CVarDef<string> AdminLogsLokiUrl =
        CVarDef.Create("adminlogs.loki_url", "", CVar.SERVERONLY);

    /// <summary>
    ///     HTTP Basic Auth username for Loki API
    /// </summary>
    public static readonly CVarDef<string> AdminLogsLokiUsername =
        CVarDef.Create("adminlogs.loki_username", "", CVar.SERVERONLY);

    /// <summary>
    ///     HTTP Basic Auth password for Loki API
    /// </summary>
    public static readonly CVarDef<string> AdminLogsLokiPassword =
        CVarDef.Create("adminlogs.loki_password", "", CVar.SERVERONLY);

    /// <summary>
    ///     The label 'app' value attached to all admin logs pushed to Loki.
    ///     Defaults to sunrise-station.
    /// </summary>
    public static readonly CVarDef<string> AdminLogsLokiName =
        CVarDef.Create("adminlogs.loki_name", "sunrise-station", CVar.SERVERONLY);
    // Sunrise-End
}

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Content.Server.Administration.Logs;

/// <summary>
/// Root payload for POST /loki/api/v1/push
/// </summary>
public sealed class LokiPushRequest
{
    [JsonPropertyName("streams")]
    public List<LokiStream> Streams { get; set; } = new();
}

/// <summary>
/// Represents a single stream of logs with shared labels.
/// </summary>
public sealed class LokiStream
{
    [JsonPropertyName("stream")]
    public Dictionary<string, string> Labels { get; set; } = new();

    /// <summary>
    /// Array of values. Each value is a 2-element string array: [ "nano_timestamp", "log_line" ]
    /// </summary>
    [JsonPropertyName("values")]
    public List<string[]> Values { get; set; } = new();
}

/// <summary>
/// Root payload for GET /loki/api/v1/query_range
/// </summary>
public sealed class LokiQueryResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public LokiQueryData Data { get; set; } = new();
}

public sealed class LokiQueryData
{
    [JsonPropertyName("resultType")]
    public string ResultType { get; set; } = string.Empty;

    [JsonPropertyName("result")]
    public List<LokiStream> Result { get; set; } = new();
}

using Robust.Shared.Map;

namespace Content.Server.GameTicking.Events;

/// <summary>
/// Raised when observer/admin ghost spawn coordinates are needed, possibly excluding a map
/// (e.g. so the ghost is not spawned on a prison map that may be deleted).
/// Handlers can set <see cref="Coordinates"/> to provide a safe spawn position.
/// </summary>
public sealed class GetObserverSpawnPointEvent : EntityEventArgs
{
    /// <summary>Map to exclude from spawn (e.g. prison map being removed).</summary>
    public MapId? ExcludeMapId { get; }

    /// <summary>If set by a handler, use these coordinates instead of the default.</summary>
    public EntityCoordinates? Coordinates { get; set; }

    public GetObserverSpawnPointEvent(MapId? excludeMapId)
    {
        ExcludeMapId = excludeMapId;
    }
}

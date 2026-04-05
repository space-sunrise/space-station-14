namespace Content.Server._Sunrise.Shuttles.Components;

/// <summary>
/// Singleton component on the shared pool map where all arrivals shuttles are spawned.
/// Instead of creating a separate map per player, shuttles are placed on a single map
/// with X-offset spacing.
/// </summary>
[RegisterComponent]
public sealed partial class SunriseArrivalsPoolComponent : Component
{
    /// <summary>
    /// Next X-offset on the pool map for spawning another shuttle grid.
    /// </summary>
    public float NextOffset;

    /// <summary>
    /// Ordered queue of shuttle EntityUids waiting to be dispatched to a station.
    /// </summary>
    public List<EntityUid> Queue = new();
}

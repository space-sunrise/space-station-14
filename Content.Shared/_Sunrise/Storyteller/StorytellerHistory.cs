using Robust.Shared.Serialization;
using Robust.Shared.Map;

namespace Content.Shared._Sunrise.Storyteller;

/// <summary>
/// Categories of tracked gameplay events.
/// </summary>
[Serializable, NetSerializable]
public enum StorytellerHistoryType : byte
{
    Event,
    Threat,
    Death,
    ContainmentBreach,
    Supermatter,
    Explosion,
    Research,
    Arrival,
    Departure
}

/// <summary>
/// A single timeline event recorded by the storyteller history system.
/// </summary>
[Serializable, NetSerializable]
public sealed class StorytellerHistoryEntry
{
    public TimeSpan RoundTime { get; set; }
    public StorytellerHistoryType EventType { get; set; }
    public string Description { get; set; } = string.Empty;

    public StorytellerHistoryEntry() { }

    public StorytellerHistoryEntry(TimeSpan roundTime, StorytellerHistoryType eventType, string description)
    {
        RoundTime = roundTime;
        EventType = eventType;
        Description = description;
    }
}

/// <summary>
/// Broadcast event triggered on the server when an explosion spawns.
/// </summary>
public sealed class SunriseExplosionEvent : EntityEventArgs
{
    public MapCoordinates Epicenter { get; }
    public float Intensity { get; }
    public int AffectedTiles { get; }

    public SunriseExplosionEvent(MapCoordinates epicenter, float intensity, int affectedTiles)
    {
        Epicenter = epicenter;
        Intensity = intensity;
        AffectedTiles = affectedTiles;
    }
}

/// <summary>
/// Broadcast event triggered on the server when a supermatter crystal collapses (delaminates).
/// </summary>
public sealed class SunriseSupermatterDelaminatedEvent : EntityEventArgs
{
    public EntityUid Supermatter { get; }

    public SunriseSupermatterDelaminatedEvent(EntityUid supermatter)
    {
        Supermatter = supermatter;
    }
}

/// <summary>
/// Broadcast event raised on the server when a nuclear bomb is armed.
/// </summary>
public sealed class SunriseNukeArmedEvent : EntityEventArgs
{
    public string Location { get; }

    public SunriseNukeArmedEvent(string location)
    {
        Location = location;
    }
}

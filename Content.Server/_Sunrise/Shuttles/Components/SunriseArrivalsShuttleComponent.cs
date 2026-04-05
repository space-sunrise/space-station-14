using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Sunrise.Shuttles.Components;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class SunriseArrivalsShuttleComponent : Component
{
    /// <summary>
    /// The station this shuttle is heading to.
    /// </summary>
    public EntityUid Station;

    /// <summary>
    /// The player's mob currently on the shuttle.
    /// </summary>
    public EntityUid? Player;

    /// <summary>
    /// Current state of the shuttle lifecycle.
    /// </summary>
    public SunriseArrivalsShuttleState State = SunriseArrivalsShuttleState.Queued;

    /// <summary>
    /// Time when the shuttle was created (for failsafe timeout).
    /// </summary>
    [AutoPausedField]
    public TimeSpan SpawnTime;

    /// <summary>
    /// Time when the shuttle docked at the station.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan? DockTime;

    /// <summary>
    /// The attendant entity on the shuttle that provides voice feedback.
    /// </summary>
    public EntityUid? Attendant;

    /// <summary>
    /// Whether the player has been greeted by the attendant.
    /// </summary>
    public bool Greeted;

    /// <summary>
    /// Stored player name for the welcome message.
    /// </summary>
    public string PlayerName = string.Empty;

    /// <summary>
    /// Stored job name for the welcome message.
    /// </summary>
    public string PlayerJob = string.Empty;

    /// <summary>
    /// Time when the welcome message should be sent.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan? GreetTime;

    /// <summary>
    /// Whether the evac warning has been issued.
    /// </summary>
    public bool Warned;

    /// <summary>
    /// Time when the shuttle started leaving (for delayed delete).
    /// </summary>
    [AutoPausedField]
    public TimeSpan? LeaveStartTime;

    /// <summary>
    /// Time when the player was last detected as having left the shuttle.
    /// Used for a short grace period before departure so the airlock doesn't crush them.
    /// </summary>
    [AutoPausedField]
    public TimeSpan? PlayerExitTime;

    /// <summary>
    /// Docks reserved by this shuttle on the target station.
    /// </summary>
    public List<EntityUid> ReservedDocks = new();
}

public enum SunriseArrivalsShuttleState : byte
{
    Queued,
    Travelling,
    Docked,
    Leaving,
}

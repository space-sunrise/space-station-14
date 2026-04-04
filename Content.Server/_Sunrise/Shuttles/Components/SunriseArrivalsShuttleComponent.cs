using Content.Shared.Storage;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Sunrise.Shuttles.Components;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class SunriseArrivalsShuttleComponent : Component
{
    /// <summary>
    /// The station this shuttle is heading to.
    /// </summary>
    [DataField("station")]
    public EntityUid Station;

    /// <summary>
    /// The player's mob currently on the shuttle.
    /// </summary>
    [DataField("player")]
    public EntityUid? Player;

    /// <summary>
    /// Current state of the shuttle lifecycle.
    /// </summary>
    [DataField("state")]
    public SunriseArrivalsShuttleState State = SunriseArrivalsShuttleState.Travelling;

    /// <summary>
    /// Time when the shuttle docked at the station.
    /// </summary>
    [DataField("dockTime", customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan? DockTime;

    /// <summary>
    /// Time when the next announcement should be made if waiting for a dock.
    /// </summary>
    [DataField("nextAnnouncement", customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan? NextAnnouncement;

    /// <summary>
    /// Time when the next FTL retry should be made if waiting for a dock.
    /// </summary>
    [DataField("nextRetry", customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan? NextRetry;

    /// <summary>
    /// Time when the shuttle entered the hyperspace queue.
    /// </summary>
    [DataField("queuedStartTime", customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan? QueuedStartTime;

    /// <summary>
    /// Docks reserved by this shuttle on the target station.
    /// </summary>
    [DataField("reservedDocks")]
    public List<EntityUid> ReservedDocks = new();

    /// <summary>
    /// The attendant entity on the shuttle that provides voice feedback.
    /// </summary>
    [DataField("attendant")]
    public EntityUid? Attendant;

    /// <summary>
    /// Whether the shuttle has issued a warning to leave after docking.
    /// </summary>
    [DataField("warned")]
    public bool Warned;

    /// <summary>
    /// Time when the shuttle docked at the station.
    /// </summary>
    [DataField("dockedStartTime", customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan? DockedStartTime;

    /// <summary>
    /// The next time the attendant will issue a warning to leave.
    /// </summary>
    [DataField("nextWarningTime", customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan? NextWarningTime;

    /// <summary>
    /// Whether the player has been greeted by the attendant.
    /// </summary>
    [DataField("greeted")]
    public bool Greeted;

    /// <summary>
    /// Stored player name for the welcome message.
    /// </summary>
    [DataField("playerName")]
    public string PlayerName = string.Empty;

    /// <summary>
    /// Stored job name for the welcome message.
    /// </summary>
    [DataField("playerJob")]
    public string PlayerJob = string.Empty;

    /// <summary>
    /// Time when the welcome message should be sent.
    /// </summary>
    [DataField("greetTime", customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan? GreetTime;
}

public enum SunriseArrivalsShuttleState : byte
{
    Queued,
    Travelling,
    Waiting,
    Docked,
    Leaving,
}

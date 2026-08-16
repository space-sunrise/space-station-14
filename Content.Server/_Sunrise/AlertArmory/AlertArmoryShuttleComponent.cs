using Content.Shared.Tag;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.AlertArmory;

/// <summary>
/// Stores runtime state for a preloaded alert armory shuttle.
/// </summary>
[RegisterComponent]
public sealed partial class AlertArmoryShuttleComponent : Component
{
    /// <summary>Station that owns this armory shuttle.</summary>
    public EntityUid Station;

    /// <summary>Dock tag preferred when the shuttle arrives.</summary>
    public ProtoId<TagPrototype>? DockTag = "DockGamma";

    /// <summary>Localization key announced when the armory arrives.</summary>
    [ViewVariables]
    public LocId? Announcement;

    /// <summary>Color of the arrival announcement.</summary>
    [ViewVariables]
    public Color? AnnouncementColor;

    /// <summary>Localization key announced when the armory is recalled.</summary>
    [ViewVariables]
    public LocId? RecallAnnouncement;

    /// <summary>Color of the recall announcement.</summary>
    [ViewVariables]
    public Color? RecallAnnouncementColor;

    /// <summary>
    /// True while the shuttle is in FTL.
    /// </summary>
    [ViewVariables]
    public bool InTransit;

    /// <summary>Coordinates used to return the shuttle to armory space.</summary>
    [ViewVariables]
    public EntityCoordinates CoordsCache;

    /// <summary>Map entity containing all preloaded armories for the station.</summary>
    [ViewVariables]
    public EntityUid ArmorySpaceUid;
}

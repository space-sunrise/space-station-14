using Robust.Shared.Utility;

namespace Content.Server._Sunrise.AlertArmory;

[RegisterComponent]
public sealed partial class AlertArmoryStationComponent : Component
{
    /// <summary>
    /// Armory definitions keyed by request key.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<string, AlertArmoryDefinition> Shuttles = [];

    /// <summary>
    /// Preloaded armory grids keyed by request key.
    /// </summary>
    [ViewVariables]
    public Dictionary<string, EntityUid> Grids = [];
}

/// <summary>
/// Defines one armory shuttle and its station announcements.
/// </summary>
[DataDefinition]
public sealed partial class AlertArmoryDefinition
{
    /// <summary>Path to the shuttle grid loaded for this armory.</summary>
    [DataField(required: true)]
    public ResPath Shuttle;

    /// <summary>Localization key announced when the armory arrives.</summary>
    [DataField]
    public LocId? Announcement;

    /// <summary>Color of the arrival announcement.</summary>
    [DataField]
    public Color? AnnouncementColor;

    /// <summary>Localization key announced when the armory is recalled.</summary>
    [DataField]
    public LocId? RecallAnnouncement;

    /// <summary>Color of the recall announcement.</summary>
    [DataField]
    public Color? RecallAnnouncementColor;
}

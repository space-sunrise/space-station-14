using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Sunrise.Shuttles;

[RegisterComponent]
public sealed partial class CodeEquipmentShuttleComponent : Component
{
    [DataField("station")]
    public EntityUid Station;

    [DataField]
    public string PriorityTag = "DockGamma";

    [DataField]
    public bool EnableDockAnnouncement = true;

    [DataField]
    public string DockAnnounceMessage = "announcement-gamma-armory";
}

using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Sunrise.Medical;

/// <summary>
/// Component for medical borg hyposprays that enables automatic injection announcements
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BorgHyposprayComponent : Component
{
    /// <summary>
    /// How long to wait between injection announcements to prevent spam
    /// </summary>
    [DataField]
    public TimeSpan AnnouncementCooldown = TimeSpan.FromSeconds(5);

    /// <summary>
    /// When the next announcement can be made
    /// </summary>
    [DataField("nextAnnouncementTime", customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField]
    public TimeSpan NextAnnouncementTime = TimeSpan.Zero;
}
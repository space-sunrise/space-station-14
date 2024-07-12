using Robust.Shared.Network;

namespace Content.Server._Sunrise.CryoTeleport;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class CryoTeleportTargetComponent : Component
{
    [DataField]
    public EntityUid? Station;

    [DataField]
    public TimeSpan? ExitTime;

    [DataField]
    public NetUserId? UserId;
}

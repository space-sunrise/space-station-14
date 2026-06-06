using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._Starlight.Weapons.Gunnery;

/// <summary>
/// Sent by the client when the player clicks on the radar to fire a selected cannon
/// at the specified world-space target coordinates.
/// </summary>
[Serializable, NetSerializable]
public sealed class GunneryConsoleFireStartMessage : BoundUserInterfaceMessage
{
    /// <summary>The cannon (gun entity) that should fire.</summary>
    public NetEntity Cannon;

    /// <summary>World-space target coordinates.</summary>
    public NetCoordinates Target;
}

/// <summary>
/// Sent by the client when the player releases LMB to stop continuous fire.
/// </summary>
[Serializable, NetSerializable]
public sealed class GunneryConsoleFireStopMessage : BoundUserInterfaceMessage
{
}

/// <summary>
/// Sent continuously while the player holds LMB with a guided projectile active,
/// directing the rocket toward the cursor's current world-space position.
/// </summary>
[Serializable, NetSerializable]
public sealed class GunneryConsoleGuidanceMessage : BoundUserInterfaceMessage
{
    /// <summary>World-space steering target (cursor position on radar).</summary>
    public NetCoordinates Target;
}

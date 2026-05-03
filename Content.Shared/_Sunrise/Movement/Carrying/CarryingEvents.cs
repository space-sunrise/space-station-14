using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Movement.Carrying;

/// <summary>
/// Do-after event used to finish a carry pickup attempt.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class CarryDoAfterEvent : SimpleDoAfterEvent;

/// <summary>
/// Raised on an entity that has just stopped being carried.
/// </summary>
[ByRefEvent]
public readonly record struct CarryDroppedEvent;

/// <summary>
/// Raised on a carrier while checking whether it can carry another entity.
/// Systems can cancel this event to block the carry attempt.
/// </summary>
/// <param name="Target">Entity that the carrier is trying to carry.</param>
[ByRefEvent]
public record struct StartCarryAttemptEvent(EntityUid Target)
{
    /// <summary>
    /// Whether the carry attempt should be blocked.
    /// </summary>
    public bool Cancelled;
}

/// <summary>
/// Raised on an entity while another entity is trying to carry it.
/// Systems can cancel this event to block the carry attempt.
/// </summary>
/// <param name="Carrier">Entity that is trying to carry this entity.</param>
[ByRefEvent]
public record struct StartBeingCarryAttemptEvent(EntityUid Carrier)
{
    /// <summary>
    /// Whether the carry attempt should be blocked.
    /// </summary>
    public bool Cancelled;
}

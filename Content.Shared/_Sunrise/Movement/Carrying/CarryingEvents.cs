using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Movement.Carrying;

[Serializable, NetSerializable]
public sealed partial class CarryDoAfterEvent : SimpleDoAfterEvent;

/// <summary>
/// Вызывается на сущности, которую только отпустили (перестали carry)
/// </summary>
[ByRefEvent]
public readonly record struct CarryDroppedEvent;

/// <summary>
/// Вызывается при проверке возможности переноса сущности другим объектом.
/// Системы могут отменить это событие, чтобы запретить перенос.
/// </summary>
[ByRefEvent]
public record struct StartCarryAttemptEvent(EntityUid Target)
{
    public bool Cancelled;
}

[ByRefEvent]
public record struct StartBeingCarryAttemptEvent(EntityUid Carrier)
{
    public bool Cancelled;
}

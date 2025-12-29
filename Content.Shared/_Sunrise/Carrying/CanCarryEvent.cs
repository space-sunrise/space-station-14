using Robust.Shared.GameObjects;

namespace Content.Shared._Sunrise.Carrying;

/// <summary>
/// Вызывается при проверке возможности переноса сущности другим объектом.
/// Системы могут отменить это событие, чтобы запретить перенос.
/// </summary>
public sealed class CanCarryEvent(EntityUid carrier) : CancellableEntityEventArgs
{
    public readonly EntityUid Carrier = carrier;
}


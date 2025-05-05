namespace Content.Shared._Sunrise.Carrying;

/// <summary>
/// Вызывается на сущности, которую только отпустили (перестали carry)
/// </summary>
[ByRefEvent]
public readonly record struct CarryDroppedEvent
{

}

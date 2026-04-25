using Robust.Shared.Map;

namespace Content.Shared._Sunrise.Weapons.Melee.Events;

/// <summary>
/// Raised on a blink-enabled weapon after its user has been repositioned to the final landing coordinates.
/// Subscribers should use this to apply landing side effects such as knockdowns without duplicating blink logic.
/// </summary>
[ByRefEvent]
public record struct MeleeBlinkLandedEvent(EntityUid User, EntityCoordinates Coordinates);

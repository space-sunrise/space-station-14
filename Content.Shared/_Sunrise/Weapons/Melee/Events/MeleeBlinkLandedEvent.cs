using Robust.Shared.Map;

namespace Content.Shared._Sunrise.Weapons.Melee.Events;

[ByRefEvent]
public record struct MeleeBlinkLandedEvent(EntityUid User, EntityCoordinates Coordinates);

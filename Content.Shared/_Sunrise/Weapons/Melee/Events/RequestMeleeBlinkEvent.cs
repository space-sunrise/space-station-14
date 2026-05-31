using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Weapons.Melee.Events;

/// <summary>
/// Raised by a client to request a melee blink to the clicked world coordinates.
/// </summary>
[Serializable, NetSerializable]
public sealed class RequestMeleeBlinkEvent : EntityEventArgs
{
    public readonly NetEntity Weapon;
    public readonly NetCoordinates Coordinates;

    public RequestMeleeBlinkEvent(NetEntity weapon, NetCoordinates coordinates)
    {
        Weapon = weapon;
        Coordinates = coordinates;
    }
}

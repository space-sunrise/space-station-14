using Content.Shared.Hands.Components;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Melee.Events;

/// <summary>
/// Raised when a light attack is made.
/// </summary>
[Serializable, NetSerializable]
public sealed class LightAttackEvent : AttackEvent
{
    public readonly NetEntity? Target;
    public readonly NetEntity Weapon;
    /// <summary>
    /// Specifies which hand to use for the attack. If null, uses the active hand (legacy behavior).
    /// </summary>
    public readonly HandLocation? HandLocation;

    public LightAttackEvent(NetEntity? target, NetEntity weapon, NetCoordinates coordinates, HandLocation? handLocation = null) : base(coordinates)
    {
        Target = target;
        Weapon = weapon;
        HandLocation = handLocation;
    }
}

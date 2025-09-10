using Content.Shared.Hands.Components;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Melee.Events;

/// <summary>
/// Raised on the client when it attempts a heavy attack.
/// </summary>
[Serializable, NetSerializable]
public sealed class HeavyAttackEvent : AttackEvent
{
    public readonly NetEntity Weapon;
    /// <summary>
    /// Specifies which hand to use for the attack. If null, uses the active hand (legacy behavior).
    /// </summary>
    public readonly HandLocation? HandLocation;

    /// <summary>
    /// As what the client swung at will not match server we'll have them tell us what they hit so we can verify.
    /// </summary>
    public List<NetEntity> Entities;

    public HeavyAttackEvent(NetEntity weapon, List<NetEntity> entities, NetCoordinates coordinates, HandLocation? handLocation = null) : base(coordinates)
    {
        Weapon = weapon;
        Entities = entities;
        HandLocation = handLocation;
    }
}

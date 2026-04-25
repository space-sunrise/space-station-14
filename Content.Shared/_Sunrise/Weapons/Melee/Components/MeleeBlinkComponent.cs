using Content.Shared.Damage;

namespace Content.Shared._Sunrise.Weapons.Melee.Components;

[RegisterComponent]
/// <summary>
/// Configures a charge-based short-range melee blink that moves the user in the direction they are facing.
/// </summary>
public sealed partial class MeleeBlinkComponent : Component
{
    /// <summary>
    /// Extra random distance added to the base teleport distance.
    /// The actual bonus is picked uniformly from 0 to this value (inclusive).
    /// </summary>
    [DataField]
    public int RandomDistanceValue = 4;

    /// <summary>
    /// Base teleportation distance in tiles measured along the user's facing direction.
    /// </summary>
    [DataField]
    public float TeleportationValue = 4f;

    /// <summary>
    /// Damage dealt to the user when the teleport destination is blocked.
    /// Set to null to disable blocked-teleport damage.
    /// </summary>
    [DataField]
    public DamageSpecifier? DamageOnBlocked;

    /// <summary>
    /// Random offset in tiles applied to the user's landing position.
    /// The user lands within this radius of the target point.
    /// Set to 0 to land exactly at the target.
    /// </summary>
    [DataField]
    public float LandingRandomOffset = 1f;
}

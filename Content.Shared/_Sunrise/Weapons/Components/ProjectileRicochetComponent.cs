// Sunrise-Edit

using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Weapons.Components;

/// <summary>
/// Projectiles with this component will be able to ricochet off of things that have RicochetableComponent.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ProjectileRicochetComponent : Component
{
    /// <summary>
    /// The probability of a ricochet occurring.
    /// </summary>
    [DataField]
    public float Chance = 0f;
}

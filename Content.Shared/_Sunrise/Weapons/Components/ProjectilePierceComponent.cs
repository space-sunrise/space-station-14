// Sunrise-Edit

using Content.Shared._Sunrise.Weapons.Enums;
using Content.Shared.Projectiles;
using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Weapons.Components;

/// <summary>
/// Projectiles with this component will be able to pierce through entities that have PierceableComponent.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedProjectileSystem), Friend = AccessPermissions.ReadWriteExecute, Other = AccessPermissions.Read)]
public sealed partial class ProjectilePierceComponent : Component
{
    /// <summary>
    /// The probability of a pierce occurring.
    /// </summary>
    [DataField]
    public float Chance = 0.1f;

    /// <summary>
    /// The maximum deviation in radians when this projectile pierces.
    /// </summary>
    [DataField]
    public float Deviation = 0.1f;

    /// <summary>
    /// The maximum level of material this projectile can pierce through.
    /// </summary>
    [DataField]
    public PierceLevel PierceLevel = PierceLevel.Flesh;

    /// <summary>
    /// Transient list of entities already pierced during this shot, to avoid repetitive collisions.
    /// </summary>
    [ViewVariables]
    public List<EntityUid> PiercedEntities = new();
}

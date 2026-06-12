// Sunrise-Edit

namespace Content.Shared._Sunrise.Weapons.Components;

/// <summary>
/// Targets with this component can cause projectiles to ricochet off them.
/// </summary>
[RegisterComponent]
public sealed partial class RicochetableComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("chance")]
    public float Chance = 1f;
}

// Sunrise-Edit

using Content.Shared._Sunrise.Weapons.Enums;

namespace Content.Shared._Sunrise.Weapons.Components;

/// <summary>
/// Targets with this component can be pierced by projectiles.
/// </summary>
[RegisterComponent]
public sealed partial class PierceableComponent : Component
{
    [DataField]
    public PierceLevel Level = PierceLevel.Metal;
}

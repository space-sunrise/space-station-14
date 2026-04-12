using Content.Server._Sunrise.Weapons.Melee.Systems;
using Content.Shared.Damage;

namespace Content.Server._Sunrise.Weapons.Melee.Components;

[RegisterComponent, Access(typeof(BackstabOnHitSystem))]
public sealed partial class BackstabOnHitComponent : Component
{
    [DataField(required: true)]
    public DamageSpecifier BonusDamage = default!;

    [DataField]
    public List<LocId> PopupMessages = [];
}

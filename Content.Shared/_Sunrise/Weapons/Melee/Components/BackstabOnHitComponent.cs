using Content.Shared.Damage;
using Content.Shared._Sunrise.Weapons.Melee.Systems;

namespace Content.Shared._Sunrise.Weapons.Melee.Components;

[RegisterComponent, Access(typeof(SharedBackstabOnHitSystem))]
public sealed partial class BackstabOnHitComponent : Component
{
    [DataField(required: true)]
    public DamageSpecifier BonusDamage = default!;

    [DataField]
    public List<LocId> PopupMessages = [];

    [DataField]
    public List<float> PopupWeights = [];
}

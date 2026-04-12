using Content.Server._Sunrise.Weapons.Melee.Systems;
using Content.Shared.Damage;

namespace Content.Server._Sunrise.Weapons.Melee.Components;

[RegisterComponent, Access(typeof(BackstabOnHitSystem))]
public sealed partial class BackstabOnHitComponent : Component
{
    [DataField(required: true)]
    public DamageSpecifier BonusDamage = new();

    [DataField]
    public List<LocId> PopupMessages = [];

    [DataField]
    public List<float> PopupWeights = [];
}

using Content.Shared.Damage;
using Content.Shared.Random;
using Content.Shared._Sunrise.Weapons.Melee.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Weapons.Melee.Components;

[RegisterComponent, Access(typeof(SharedBackstabOnHitSystem))]
public sealed partial class BackstabOnHitComponent : Component
{
    /// <summary>
    /// Additional <see cref="DamageSpecifier"/> loaded from the required <c>bonusDamage</c> <see cref="DataFieldAttribute"/>
    /// and applied when the hit qualifies as a backstab.
    /// </summary>
    [DataField(required: true)]
    public DamageSpecifier BonusDamage = new();

    /// <summary>
    /// Optional weighted popup table used to select a localized backstab message.
    /// </summary>
    [DataField]
    public ProtoId<WeightedRandomPrototype>? PopupMessages;
}

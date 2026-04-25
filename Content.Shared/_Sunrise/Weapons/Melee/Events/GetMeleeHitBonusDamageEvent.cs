using Content.Shared.Damage;

namespace Content.Shared._Sunrise.Weapons.Melee.Events;

/// <summary>
/// Raised on a melee weapon so subscribers can add hit-specific bonus damage on top of the already computed base damage.
/// Handlers should mutate <see cref="BonusDamage"/> additively and may skip their modifier when <see cref="IsWideAttack"/> is true.
/// </summary>
[ByRefEvent]
public record struct GetMeleeHitBonusDamageEvent(
    EntityUid Weapon,
    EntityUid User,
    EntityUid Target,
    DamageSpecifier BonusDamage,
    bool IsWideAttack);

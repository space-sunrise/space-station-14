using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared._Sunrise.Damage;

/// <summary>
/// Применяет <see cref="MeleeDamageMultiplierComponent"/> к урону, наносимому владельцем в ближнем бою.
/// </summary>
public sealed class MeleeDamageMultiplierSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        // GetMeleeDamageEvent поднимается directed-вызовом (RaiseLocalEvent(weaponUid, ref ev)) без broadcast:true,
        // так что чисто broadcast-подписка (без TComp) его никогда не поймает. MeleeWeaponComponent есть на любом
        // оружии (и на скрытом безоружном ударе), поэтому подписываемся directed на него, а нужную сущность
        // (атакующего) достаём из GetMeleeDamageEvent.User.
        SubscribeLocalEvent<MeleeWeaponComponent, GetMeleeDamageEvent>(OnGetMeleeDamage);
    }

    private void OnGetMeleeDamage(Entity<MeleeWeaponComponent> ent, ref GetMeleeDamageEvent args)
    {
        if (!TryComp<MeleeDamageMultiplierComponent>(args.User, out var multiplier))
            return;

        args.Damage *= multiplier.Multiplier;
    }
}

using Content.Shared.Damage.Systems;

namespace Content.Shared._Sunrise.Damage;

/// <summary>
/// Применяет <see cref="HealingMultiplierComponent"/> к лечению, получаемому владельцем, из любого источника.
/// Подписка идёт на BeforeDamageChangedEvent, а не на DamageModifyEvent: большинство лечащих реагентов
/// (HealthChange) по умолчанию вызывают TryChangeDamage с ignoreResistances: true, из-за чего DamageModifyEvent
/// для них вообще не поднимается и не срабатывает. BeforeDamageChangedEvent поднимается всегда, до этой развилки.
/// </summary>
public sealed class HealingMultiplierSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HealingMultiplierComponent, BeforeDamageChangedEvent>(OnBeforeDamageChanged);
    }

    private void OnBeforeDamageChanged(Entity<HealingMultiplierComponent> ent, ref BeforeDamageChangedEvent args)
    {
        foreach (var (type, value) in args.Damage.DamageDict)
        {
            if (value >= 0)
                continue;

            args.Damage.DamageDict[type] = value * ent.Comp.Multiplier;
        }
    }
}

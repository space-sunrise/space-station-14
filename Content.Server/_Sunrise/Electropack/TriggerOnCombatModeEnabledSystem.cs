using Content.Shared.CombatMode;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Systems;
using Content.Shared.Trigger.Systems;

namespace Content.Server._Sunrise.Electropack;

/// <summary>
/// Система отслеживает включение боевого режима и запускает триггер на рюкзаке,
/// если на носителе надет рюкзак с <see cref="TriggerOnCombatModeEnabledComponent"/>.
/// Шок применяется через уже существующий <see cref="ShockOnTriggerComponent"/>,
/// после чего боевой режим принудительно отключается.
/// </summary>
public sealed class TriggerOnCombatModeEnabledSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly TriggerSystem _trigger = default!;
    [Dependency] private readonly SharedCombatModeSystem _combatMode = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CombatModeComponent, CombatModeChangedEvent>(OnCombatModeChanged);
    }

    private void OnCombatModeChanged(EntityUid uid, CombatModeComponent combatComp, ref CombatModeChangedEvent args)
    {
        // Триггер на боевой режим
        if (!args.IsInCombatMode)
            return;

        // Это что бы зеки не суицидались
        if (!_mobState.IsAlive(uid))
            return;

        // Проверка на ношение
        if (!_inventory.TryGetSlotEntity(uid, "back", out var backpack))
            return;

        if (!HasComp<TriggerOnCombatModeEnabledComponent>(backpack))
            return;

        // Используем ShockOnTriggerComponent
        _trigger.Trigger(backpack.Value, uid);

        // Принудительное отключение боевоего режима после удара током. 
        _combatMode.SetInCombatMode(uid, false, combatComp);
    }
}

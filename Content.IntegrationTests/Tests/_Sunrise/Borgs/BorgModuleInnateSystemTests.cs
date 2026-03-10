using Content.IntegrationTests;
using Content.Server._Sunrise.Borgs.ModuleInnate;
using Content.Server.Cargo.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Power.Components;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Sunrise.Borgs;

/// <summary>
/// Интеграционные тесты для <see cref="BorgModuleInnateSystem"/>.
///
/// Проверяют три аспекта системы:
///   1. Добавление и удаление InnateComponents (компонентов борга) при установке/изъятии модуля.
///   2. Жизненный цикл встроенных предметов и экшенов.
///   3. Балансировку заряда между батареей борга и батареями встроенных предметов.
/// </summary>
[TestFixture]
[TestOf(typeof(BorgModuleInnateSystem))]
public sealed class BorgModuleInnateSystemTests
{
    // ─────────────────────────────────────────────────────────────────
    // Тестовые прототипы
    //
    // Правило: все прототипы для тестов объявляются прямо в константах
    // с атрибутом [TestPrototypes]. Это позволяет каждому тест-классу
    // иметь изолированный набор прототипов, не затрагивая игровые данные.
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Минимальный борг — только BorgChassisComponent.
    /// Достаточно для тестов установки компонентов и предметов.
    /// </summary>
    [TestPrototypes]
    private const string BorgPrototypes = @"
- type: entity
  id: TestBorgForInnate
  components:
  - type: BorgChassis
";

    /// <summary>
    /// Борг со слотом для ячейки питания.
    /// Нужен для теста балансировки заряда.
    /// ItemSlots: создаёт слот с именем cell_slot через ItemSlotsSystem.
    /// PowerCellSlot: указывает системе заряда, где искать батарею (по слоту cell_slot).
    /// BorgChassis: нужен для EntityQueryEnumerator в BalanceInnateItemCharges.
    /// </summary>
    [TestPrototypes]
    private const string BorgWithCellPrototypes = @"
- type: entity
  id: TestBorgForInnateWithCell
  components:
  - type: BorgChassis
  - type: ContainerContainer
    containers:
      cell_slot: !type:ContainerSlot { }
  - type: PowerCellSlot
    cellSlotId: cell_slot
  - type: ItemSlots
    slots:
      cell_slot:
        name: power-cell-slot-component-slot-name-default
";

    /// <summary>
    /// Тестовая ячейка питания: BatteryComponent с 1000 единиц / 500 заряда (50%).
    /// </summary>
    [TestPrototypes]
    private const string PowerCellPrototypes = @"
- type: entity
  id: TestBorgPowerCellInnate
  components:
  - type: PowerCell
  - type: Battery
    maxCharge: 1000
    startingCharge: 500
";

    /// <summary>
    /// Простой тестовый предмет без каких-либо компонент.
    /// Используется в UseItems для теста жизненного цикла.
    /// </summary>
    [TestPrototypes]
    private const string ItemPrototypes = @"
- type: entity
  id: TestBorgInnateUseItem
  name: Test Innate Use Item
";

    /// <summary>
    /// Тестовый предмет с батареей 500/500 (100%).
    /// Используется для теста балансировки заряда.
    /// </summary>
    [TestPrototypes]
    private const string ItemWithBatteryPrototypes = @"
- type: entity
  id: TestBorgInnateItemWithBattery
  name: Test Innate Battery Item
  components:
  - type: PowerCell
  - type: Battery
    maxCharge: 500
    startingCharge: 500
";

    /// <summary>
    /// Модуль с InnateComponents: добавляет StaticPriceComponent к боргу.
    /// StaticPriceComponent — простой серверный компонент без сложных зависимостей.
    /// </summary>
    [TestPrototypes]
    private const string ModuleWithComponentsPrototypes = @"
- type: entity
  id: TestBorgModuleInnateWithComponents
  components:
  - type: BorgModule
  - type: BorgModuleInnate
    innateComponents:
    - type: StaticPrice
      price: 100
";

    /// <summary>
    /// Модуль с одним UseItem — предметом, который активируется в руке.
    /// </summary>
    [TestPrototypes]
    private const string ModuleWithItemsPrototypes = @"
- type: entity
  id: TestBorgModuleInnateWithItems
  components:
  - type: BorgModule
  - type: BorgModuleInnate
    useItems:
    - TestBorgInnateUseItem
";

    /// <summary>
    /// Модуль с одним ToggleItem с батареей.
    /// powerUseCoefficient 0.5 означает, что вклад предмета в баланс заряда
    /// считается с весом 0.5 от реального.
    /// </summary>
    [TestPrototypes]
    private const string ModuleWithBatteryPrototypes = @"
- type: entity
  id: TestBorgModuleInnateWithBattery
  components:
  - type: BorgModule
  - type: BorgModuleInnate
    powerUseCoefficient: 0.5
    toggleItems:
    - TestBorgInnateItemWithBattery
    canCharge: true
";

    // ─────────────────────────────────────────────────────────────────
    // Тест 1 — InnateComponents добавляются и удаляются вместе с модулем
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Проверяет, что компоненты из поля <c>innateComponents</c> добавляются
    /// на борга при установке модуля и удаляются при его изъятии.
    ///
    /// КАК ПИСАТЬ ТАКИЕ ТЕСТЫ:
    ///   1. Получи TestPair через PoolManager.GetServerClient().
    ///      Пара содержит server (серверный интеграционный экземпляр) и client.
    ///   2. Получи IEntityManager и нужные системы через server.ResolveDependency
    ///      и entMan.System&lt;T&gt;().
    ///   3. Выполняй всю логику в server.WaitAssertion — это запускает код
    ///      в потоке сервера, что гарантирует корректную обработку событий ECS.
    ///   4. Спавни сущности через entMan.SpawnEntity с координатами карты.
    ///      SpawnEntity запускает MapInit для сущности, инициализируя всё.
    ///   5. В конце всегда вызывай pair.CleanReturnAsync() — возвращает пару
    ///      в пул для переиспользования другими тестами.
    /// </summary>
    [Test]
    public async Task TestInnateComponentsAddedAndRemovedOnInstall()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        // Разрешаем зависимости через сервер — это потокобезопасно вне WaitAssertion
        var entMan = server.ResolveDependency<IEntityManager>();
        var borgSystem = entMan.System<SharedBorgSystem>();

        // CreateTestMap создаёт карту и инициализирует её.
        // GridCoords — это координаты на сетке карты, куда можно спавнить сущности.
        var testMap = await pair.CreateTestMap();
        var coords = testMap.GridCoords;

        EntityUid borg = default;
        EntityUid module = default;

        // WaitAssertion запускает делегат в главном потоке сервера.
        // Все проверки Assert'ами должны быть внутри него (или в последующих WaitAssertion).
        await server.WaitAssertion(() =>
        {
            // Спавним минимальный борг и модуль
            borg = entMan.SpawnEntity("TestBorgForInnate", coords);
            module = entMan.SpawnEntity("TestBorgModuleInnateWithComponents", coords);

            var borgComp = entMan.GetComponent<BorgChassisComponent>(borg);
            var moduleComp = entMan.GetComponent<BorgModuleComponent>(module);

            // До установки — StaticPriceComponent на борге быть не должно
            Assert.That(entMan.HasComponent<StaticPriceComponent>(borg), Is.False,
                "StaticPriceComponent не должен присутствовать на борге ДО установки модуля");

            // InstallModule публичный метод SharedBorgSystem. Он устанавливает
            // module.Comp.InstalledEntity и поднимает BorgModuleInstalledEvent.
            // BorgModuleInnateSystem перехватывает это событие в OnInstalled.
            borgSystem.InstallModule((borg, borgComp), (module, moduleComp));

            // После установки — StaticPriceComponent должен появиться
            Assert.That(entMan.HasComponent<StaticPriceComponent>(borg), Is.True,
                "StaticPriceComponent должен быть добавлен на борга ПОСЛЕ установки модуля");

            // BorgWithInnateModulesComponent — служебный компонент-маркер,
            // добавляется при первом активном innate-модуле для оптимизации EntityQuery
            Assert.That(entMan.HasComponent<BorgWithInnateModulesComponent>(borg), Is.True,
                "BorgWithInnateModulesComponent должен присутствовать на борге после установки innate-модуля");

            // UninstallModule поднимает BorgModuleUninstalledEvent.
            // OnUninstalled удаляет все добавленные компоненты.
            borgSystem.UninstallModule((borg, borgComp), (module, moduleComp));

            // После удаления — оба компонента должны исчезнуть
            Assert.That(entMan.HasComponent<StaticPriceComponent>(borg), Is.False,
                "StaticPriceComponent должен быть УДАЛЁН с борга после изъятия модуля");

            Assert.That(entMan.HasComponent<BorgWithInnateModulesComponent>(borg), Is.False,
                "BorgWithInnateModulesComponent должен удалиться, если не осталось активных innate-модулей");
        });

        // Возвращаем пару в пул; без этого вызова ресурсы утекут
        await pair.CleanReturnAsync();
    }

    // ─────────────────────────────────────────────────────────────────
    // Тест 2 — Жизненный цикл встроенных предметов и экшенов
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Проверяет, что встроенные предметы спавнятся в контейнер при установке модуля
    /// и полностью удаляются при его изъятии вместе с экшенами.
    ///
    /// КАК РАБОТАЕТ ТЕСТ:
    ///   После InstallModule у BorgModuleInnateComponent должны быть:
    ///     - InnateItemsContainer — контейнер с созданными предметами
    ///     - Actions — список EntityUid экшенов, добавленных боргу
    ///   После UninstallModule:
    ///     - Предметы должны быть удалены из мира (EntityExists → false)
    ///     - InnateItemsContainer → null
    ///     - Actions → пустой список
    /// </summary>
    [Test]
    public async Task TestInnateItemsAndActionsLifecycle()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var entMan = server.ResolveDependency<IEntityManager>();
        var borgSystem = entMan.System<SharedBorgSystem>();

        var testMap = await pair.CreateTestMap();
        var coords = testMap.GridCoords;

        EntityUid borg = default;
        EntityUid module = default;

        await server.WaitAssertion(() =>
        {
            borg = entMan.SpawnEntity("TestBorgForInnate", coords);
            module = entMan.SpawnEntity("TestBorgModuleInnateWithItems", coords);

            var borgComp = entMan.GetComponent<BorgChassisComponent>(borg);
            var moduleComp = entMan.GetComponent<BorgModuleComponent>(module);

            // Компонент BorgModuleInnateComponent хранит данные о созданных предметах и экшенах
            var innateComp = entMan.GetComponent<BorgModuleInnateComponent>(module);

            // Устанавливаем модуль
            borgSystem.InstallModule((borg, borgComp), (module, moduleComp));

            // InnateItemsContainer должен быть проинициализирован системой
            var container = innateComp.InnateItemsContainer;
            Assert.Multiple(() =>
            {
                Assert.That(container, Is.Not.Null,
                    "InnateItemsContainer should be initialized by the system after module installation");
                Assert.That(container!.ContainedEntities, Has.Count.EqualTo(1),
                    "There should be exactly 1 item (1 UseItem) in the container after installation");
            });

            // Для каждого UseItem создаётся один экшен ModuleInnateUseItemAction
            Assert.That(innateComp.Actions, Has.Count.EqualTo(1),
                "For one UseItem, exactly one action should be created");

            // Сохраняем ссылку на созданный предмет для проверки после удаления
            var spawnedItem = container!.ContainedEntities[0];
            Assert.That(entMan.EntityExists(spawnedItem), Is.True,
                "Created item should exist in the world");

            // Изымаем модуль
            borgSystem.UninstallModule((borg, borgComp), (module, moduleComp));

            // Предмет должен быть полностью удалён из Entity Manager
            Assert.That(entMan.EntityExists(spawnedItem), Is.False,
                "Created innate item should be DELETED from the world when the module is uninstalled");

            // Ссылка на контейнер очищается в null
            Assert.That(innateComp.InnateItemsContainer, Is.Null,
                "InnateItemsContainer should become null after module is uninstalled");

            // Список экшенов должен быть очищен
            Assert.That(innateComp.Actions, Is.Empty,
                "Actions list should be empty after module is uninstalled");
        });

        await pair.CleanReturnAsync();
    }

    // ─────────────────────────────────────────────────────────────────
    // Тест 3 — Балансировка заряда между батареей борга и предметами
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Проверяет, что система раз в секунду выравнивает уровень заряда
    /// между батареей борга и батареями встроенных предметов.
    ///
    /// МАТЕМАТИКА БАЛАНСИРОВКИ (из BorgModuleInnateSystem.BalanceInnateItemCharges):
    ///   Borg battery:  500 / 1000 (50%)
    ///   Item battery:  500 / 500 (100%), вес = powerUseCoefficient = 0.5
    ///
    ///   totalCharge    = 500 + 500 * 0.5  = 750
    ///   totalMaxCharge = 1000 + 500 * 0.5 = 1250
    ///   setLevel       = 750 / 1250      ~= 0.6  (~60%)
    ///
    ///   Borg новый заряд = ~0.6 * 1000 ~= 600  (вырос с 500)
    ///   Item новый заряд = ~0.6 * 500  ~= 300  (уменьшился с 500)
    ///
    /// КАК ПИСАТЬ ТЕСТЫ С ТЕЧЕНИЕМ ВРЕМЕНИ:
    ///   1. Сделай Setup в первом WaitAssertion.
    ///   2. Продвинь время сервера через pair.RunTicksSync(N).
    ///      ChargeBalanceInterval = 1 секунда, при 60 TPS нужно 60+ тиков.
    ///      Используем 130 тиков (~2.17 с) для гарантированного срабатывания.
    ///   3. Проверяй результат во втором WaitAssertion.
    /// </summary>
    [Test]
    public async Task TestChargeSyncBetweenBorgAndInnateItems()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var entMan = server.ResolveDependency<IEntityManager>();
        var borgSystem = entMan.System<SharedBorgSystem>();
        var batterySystem = entMan.System<BatterySystem>();
        var powerCellSystem = entMan.System<PowerCellSystem>();
        var itemSlotsSystem = entMan.System<ItemSlotsSystem>();

        var testMap = await pair.CreateTestMap();
        var coords = testMap.GridCoords;

        EntityUid borg = default;
        EntityUid module = default;
        EntityUid powerCell = default;

        // ── Этап 1: Настройка ───────────────────────────────────────
        await server.WaitAssertion(() =>
        {
            borg = entMan.SpawnEntity("TestBorgForInnateWithCell", coords);
            module = entMan.SpawnEntity("TestBorgModuleInnateWithBattery", coords);
            // Ячейка питания спавнится с зарядом 500/1000 (50%) согласно прототипу
            powerCell = entMan.SpawnEntity("TestBorgPowerCellInnate", coords);

            // Вставляем ячейку в слот cell_slot борга.
            // TryInsert(entityUid, slotId, item, user?) — метод ItemSlotsSystem.
            // Слот TestBorgForInnateWithCell.cell_slot инициализируется ItemSlotsSystem.OnMapInit.
            var inserted = itemSlotsSystem.TryInsert(borg, "cell_slot", powerCell, null);
            Assert.That(inserted, Is.True, "Ячейка питания должна быть успешно вставлена в борга");

            // Проверяем что батарея борга теперь доступна (50%)
            Assert.That(powerCellSystem.TryGetBatteryFromSlot(borg, out var borgBattery), Is.True,
                "Borg should have a battery in the cell_slot after inserting the cell");
            Assert.That(batterySystem.GetChargeLevel(borgBattery!.Value), Is.EqualTo(0.5f).Within(0.01f),
                "Borg's battery should be at 50% at the start of the test");

            var borgComp = entMan.GetComponent<BorgChassisComponent>(borg);
            var moduleComp = entMan.GetComponent<BorgModuleComponent>(module);
            var innateComp = entMan.GetComponent<BorgModuleInnateComponent>(module);

            // Устанавливаем модуль — создаётся предмет TestBorgInnateItemWithBattery (100%: 500/500)
            borgSystem.InstallModule((borg, borgComp), (module, moduleComp));

            var container = innateComp.InnateItemsContainer;
            Assert.That(container, Is.Not.Null, "Контейнер предметов должен быть создан");
            Assert.That(container!.ContainedEntities, Has.Count.EqualTo(1));

            var item = container.ContainedEntities[0];
            var itemBattery = entMan.GetComponent<BatteryComponent>(item);

            // Предмет должен начинать на 100% согласно прототипу TestBorgInnateItemWithBattery
            Assert.That(batterySystem.GetChargeLevel((item, itemBattery)), Is.EqualTo(1.0f).Within(0.01f),
                "Innate item's battery should be at 100% at the start of the test");
        });

        // ── Этап 2: Продвигаем время ─────────────────────────────────
        // ChargeBalanceInterval = 1 секунда.
        // 130 тиков при 60 TPS ≈ 2.17 секунды — гарантированно 2 балансировки.
        // RunTicksSync запускает N тиков на обоих (server + client) синхронно.
        await pair.RunTicksSync(130);

        // ── Этап 3: Проверяем результат ──────────────────────────────
        await server.WaitAssertion(() =>
        {
            // Батарея борга должна вырасти с 500 до ~600
            Assert.That(powerCellSystem.TryGetBatteryFromSlot(borg, out var borgBattery), Is.True,
                "Borg's battery should be available after balancing");
            var borgCharge = batterySystem.GetCharge(borgBattery!.Value);
            Assert.That(borgCharge, Is.GreaterThan(500f),
                $"Borg's charge should have INCREASED after balancing (50% → ~60%), because borg battery recharging is allowed. Current value: {borgCharge}");

            // Батарея предмета должна уменьшиться с 500 до ~300
            var innateComp = entMan.GetComponent<BorgModuleInnateComponent>(module);
            var item = innateComp.InnateItemsContainer!.ContainedEntities[0];
            var itemBattery = entMan.GetComponent<BatteryComponent>(item);
            var itemCharge = batterySystem.GetCharge((item, itemBattery));
            Assert.That(itemCharge, Is.LessThan(500f),
                $"Innate item's charge should have DECREASED after balancing (100% → ~60%), because borg battery recharging is allowed. Current value: {itemCharge}");

            // Оба заряда должны стать равными (~60% от своего максимума)
            var borgLevel = borgCharge / borgBattery.Value.Comp.MaxCharge;
            var itemLevel = itemCharge / itemBattery.MaxCharge;
            Assert.That(borgLevel, Is.EqualTo(itemLevel).Within(0.02f),
                "Borg's and innate item's charge levels should match after balancing");
        });

        await pair.CleanReturnAsync();
    }
}

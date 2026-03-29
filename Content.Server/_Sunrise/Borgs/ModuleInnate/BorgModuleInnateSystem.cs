using System.Linq;
using Content.Server.Actions;
using Content.Server.Interaction;
using Content.Server.Power.EntitySystems;
using Content.Shared._Sunrise.Borgs.ModuleInnate;
using Content.Shared.Actions;
using Content.Shared.Power.Components;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.UserInterface;
using Robust.Server.Containers;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._Sunrise.Borgs.ModuleInnate;

/// <summary>
/// Система выдачи встраиваемых предметов и компонентов через модуль.
/// </summary>
public sealed class BorgModuleInnateSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly ContainerSystem _containers = default!;
    [Dependency] private readonly MetaDataSystem _metadata = default!;
    [Dependency] private readonly InteractionSystem _interactions = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    // Название контейнера-хранилища встроенных предметов
    private const string InnateItemsContainerId = "module_innate_items";

    /// <summary>
    /// Период обновления баланса зарядов между боргом и встроенными предметами.
    /// </summary>
    private const float ChargeBalanceInterval = 1f;

    private EntityQuery<BorgModuleInnateComponent> _borgModuleInnateQuery;
    private EntityQuery<PowerCellSlotComponent> _powerCellSlotQuery;
    private EntityQuery<BatteryComponent> _batteryQuery;
    private EntityQuery<BorgChassisComponent> _borgChassisQuery;

    private TimeSpan _nextChargeBalanceTime;

    public override void Initialize()
    {
        base.Initialize();

        _borgModuleInnateQuery = GetEntityQuery<BorgModuleInnateComponent>();
        _powerCellSlotQuery = GetEntityQuery<PowerCellSlotComponent>();
        _batteryQuery = GetEntityQuery<BatteryComponent>();
        _borgChassisQuery = GetEntityQuery<BorgChassisComponent>();

        SubscribeLocalEvent<BorgModuleInnateComponent, BorgModuleInstalledEvent>(OnInstalled);
        SubscribeLocalEvent<BorgModuleInnateComponent, BorgModuleUninstalledEvent>(OnUninstalled);

        SubscribeLocalEvent<BorgModuleInnateComponent, ModuleInnateUseItemEvent>(OnInnateUseItem);
        SubscribeLocalEvent<BorgModuleInnateComponent, ModuleInnateToggleItemEvent>(OnInnateToggleItem);
        SubscribeLocalEvent<BorgModuleInnateComponent, ModuleInnateInteractionItemEvent>(OnInnateInteractionItem);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextChargeBalanceTime)
            return;

        _nextChargeBalanceTime = _timing.CurTime + TimeSpan.FromSeconds(ChargeBalanceInterval);

        BalanceInnateItemCharges();
    }

    // Батарейки, которые будут отбалансированы (используется BalanceInnateItemCharges)
    private List<Entity<BatteryComponent>> _batteriesToBalance = [];
    /// <summary>
    /// Раз в секунду балансирует заряд между батарейкой борга и предметами, которые тоже имеют батарею.
    /// </summary>
    private void BalanceInnateItemCharges()
    {
        var borgQuery = EntityQueryEnumerator<BorgWithInnateModulesComponent, BorgChassisComponent>();

        while (borgQuery.MoveNext(out var borgUid, out var innateModules, out var chassis))
        {
            // Пытаемся получить основную батарею борга.
            if (!_powerCell.TryGetBatteryFromSlot(borgUid, out var borgBattery))
                continue;

            // Проверяем заряд борга.
            var borgCharge = _battery.GetCharge((borgBattery.Value.Owner, borgBattery.Value.Comp));
            if (borgCharge <= 5f || borgBattery.Value.Comp.MaxCharge <= 5f)
                continue;

            _batteriesToBalance.Clear();
            _batteriesToBalance.Add(borgBattery.Value);
            // Нужные значения для балансировки
            var totalChargeToBalance = borgCharge;
            var totalMaxChargeToBalance = borgBattery.Value.Comp.MaxCharge;

            // Подготавливаем переменные
            var borgChargeLevel = borgCharge / borgBattery.Value.Comp.MaxCharge;

            // Для всех модулей (берём айтемы из модулей, а не контейнера, чтобы потом можно было задавать рейты заряда в будущем)
            foreach (var moduleUid in innateModules.Modules)
            {
                // С компонентом иннейтов
                if (!_borgModuleInnateQuery.TryComp(moduleUid, out var moduleComp))
                    continue;
                // Для каждого предмета модуля
                foreach (var item in moduleComp.InnateItemsContainer?.ContainedEntities ?? [])
                {
                    // Если у него есть батарея
                    if (!TryGetItemBattery(item, out var itemBattery))
                        continue;

                    // Если уровень заряда больше уровня заряда боргича (чтобы не заряжали борга)
                    var chargeLevel = _battery.GetChargeLevel(itemBattery.AsNullable());
                    if (!moduleComp.CanCharge && chargeLevel > borgChargeLevel)
                        continue;

                    // Добавляем в список балансировки
                    _batteriesToBalance.Add(itemBattery);
                    // Ведём учет общего заряда / максимального заряда для балансировки
                    // Умножаем на PowerUseCoefficient для уменьшения потребления
                    totalChargeToBalance += _battery.GetCharge(itemBattery.AsNullable()) * moduleComp.PowerUseCoefficient;
                    totalMaxChargeToBalance += itemBattery.Comp.MaxCharge * moduleComp.PowerUseCoefficient;
                }
            }

            // Считаем уровень балансировки
            var setLevel = totalChargeToBalance / totalMaxChargeToBalance;
            // Раздаем заряд каждой батарее обратно в балансированном виде
            foreach (var battery in _batteriesToBalance)
            {
                _battery.SetCharge((battery.Owner, battery.Comp), setLevel * battery.Comp.MaxCharge);
            }
        }
    }

    /// <summary>
    /// Пытается получить батарею встроенного предмета (из PowerCellSlot или прямо из BatteryComponent).
    /// </summary>
    private bool TryGetItemBattery(EntityUid item, out Entity<BatteryComponent> battery)
    {
        // Сперва пробуем через слот аккумулятора, если он есть.
        if (_powerCellSlotQuery.TryComp(item, out var slot))
        {
            if (_powerCell.TryGetBatteryFromSlotOrEntity((item, slot), out var slotBattery) && slotBattery != null)
            {
                battery = slotBattery.Value;
                return true;
            }
        }

        // Если слота нет, проверяем является ли сам предмет батарейкой.
        if (_batteryQuery.TryComp(item, out var directBattery))
        {
            battery = (item, directBattery);
            return true;
        }

        battery = default;
        return false;
    }

    /// <summary>
    /// Добавляет нужные компоненты и предметы при "установке" модуля
    /// </summary>
    private void OnInstalled(Entity<BorgModuleInnateComponent> module, ref BorgModuleInstalledEvent args)
    {
        // Делаем контейнер для встроенных предметов
        var containerManager = EnsureComp<ContainerManagerComponent>(args.ChassisEnt);
        var container = _containers.EnsureContainer<Container>(
            args.ChassisEnt,
            InnateItemsContainerId + module.Owner.ToString(),
            containerManager
        );
        // Позволяет встраивать рабочие предметы-светильники в модуль
        container.OccludesLight = false;
        // Отслеживаем контейнер в компоненте для дальнейшего использования
        module.Comp.InnateItemsContainer = container;

        var withInnateModules = EnsureComp<BorgWithInnateModulesComponent>(args.ChassisEnt);
        withInnateModules.Modules.Add(module.Owner);

        // Добавляем прописанные иннейты
        EntityManager.AddComponents(args.ChassisEnt, module.Comp.InnateComponents);
        AddItems(args.ChassisEnt, module, container);
    }

    /// <summary>
    /// Удаляет нужные компоненты и предметы при "удалении" модуля
    /// </summary>
    private void OnUninstalled(Entity<BorgModuleInnateComponent> module, ref BorgModuleUninstalledEvent args)
    {
        // Выключаем включенные предметы
        foreach (var enabledItem in module.Comp.ToggledOn)
        {
            _interactions.UseInHandInteraction(args.ChassisEnt, enabledItem, false, true);
        }

        // Чистим сущностей и компоненты
        foreach (var action in module.Comp.Actions)
        {
            _actions.RemoveAction(args.ChassisEnt, action);
        }

        // Чистим служебные списки
        module.Comp.Actions.Clear();
        module.Comp.ToggledOn.Clear();

        // Проверяем валидность сущности перед дополнительными очистками
        // Работа с компонентами удаляемой сущности может привести к неприятным последствиям
        if (!TerminatingOrDeleted(args.ChassisEnt))
        {
            EntityManager.RemoveComponents(args.ChassisEnt, module.Comp.InnateComponents);

            // Удаляем модуль из регистра модулей-иннейтов
            if (TryComp<BorgWithInnateModulesComponent>(args.ChassisEnt, out var withInnateModules))
            {
                withInnateModules.Modules.Remove(module.Owner);
                if (withInnateModules.Modules.Count == 0)
                    RemComp<BorgWithInnateModulesComponent>(args.ChassisEnt);
            }

            // Получаем контейнер модуля из борга, чистим и удаляем его.
            if (
                TryComp<ContainerManagerComponent>(args.ChassisEnt, out var containerManager)
                && _containers.TryGetContainer(
                    args.ChassisEnt,
                    InnateItemsContainerId + module.Owner.ToString(),
                    out var container,
                    containerManager: containerManager
                )
            )
            {
                _containers.CleanContainer(container);
                containerManager.Containers.Remove(InnateItemsContainerId + module.Owner.ToString());
            }
        }

        module.Comp.InnateItemsContainer = null;
    }

    /// <summary>
    /// Добавляет предметы в контейнер, а также создаёт экшены их активации в модуле для тела киборга
    /// </summary>
    private void AddItems(EntityUid chassis, Entity<BorgModuleInnateComponent> module, BaseContainer container)
    {
        foreach (var itemProto in module.Comp.UseItems)
        {
            if (itemProto is null)
                continue;

            AddUseItem(itemProto.Value, chassis, module, container);
        }

        foreach (var itemProto in module.Comp.InteractionItems)
        {
            if (itemProto is null)
                continue;

            AddInteractionItem(itemProto.Value, chassis, module, container);
        }

        foreach (var itemProto in module.Comp.ToggleItems)
        {
            if (itemProto is null)
                continue;

            AddToggleItem(itemProto.Value, chassis, module, container);
        }
    }

    /// <summary>
    /// Добавляет предмет, который активируется в руке, вместе с экшеном для его активации
    /// </summary>
    private void AddUseItem(
        EntProtoId itemProto,
        EntityUid chassis,
        Entity<BorgModuleInnateComponent> module,
        BaseContainer container
    )
    {
        var item = CreateInnateItem(itemProto, module, container);
        var ev = new ModuleInnateUseItemEvent();
        ev.Item = item;
        var action = CreateAction(item, module.Owner, ev, module.Comp.InnateUseItemAction);
        AssignAction(chassis, module, action);
    }

    /// <summary>
    /// Добавляет предмет, который активируется выбором цели, вместе с экшеном для его активации
    /// </summary>
    private void AddInteractionItem(
        EntProtoId itemProto,
        EntityUid chassis,
        Entity<BorgModuleInnateComponent> module,
        BaseContainer container
    )
    {
        var item = CreateInnateItem(itemProto, module, container);
        var ev = new ModuleInnateInteractionItemEvent();
        ev.Item = item;
        var action = CreateAction(item, module.Owner, ev, module.Comp.InnateInteractionItemAction);
        AssignAction(chassis, module, action);
    }

    /// <summary>
    /// Создает предмет для использования через экшен, при чем считает его состояние переключаемым
    /// </summary>
    private void AddToggleItem(
        EntProtoId itemProto,
        EntityUid chassis,
        Entity<BorgModuleInnateComponent> module,
        BaseContainer container
    )
    {
        var item = CreateInnateItem(itemProto, module, container);
        var ev = new ModuleInnateToggleItemEvent();
        ev.Item = item;
        var action = CreateAction(item, module.Owner, ev, module.Comp.InnateToggleItemAction);
        AssignAction(chassis, module, action);
    }

    /// <summary>
    /// Создает предмет для использования через экшены согласно прототипу в заданном контейнере
    /// </summary>
    /// <returns>Сущность предмета</returns>
    private EntityUid CreateInnateItem(
        EntProtoId itemProto,
        Entity<BorgModuleInnateComponent> module,
        BaseContainer container
    )
    {
        var item = Spawn(itemProto);

        // Модифицируем компач юай, чтобы борг наверняка мог его использовать
        if (TryComp<ActivatableUIComponent>(item, out var activatableUIComponent))
        {
            activatableUIComponent.RequiresComplex = false;
            activatableUIComponent.InHandsOnly = false;
            activatableUIComponent.RequireActiveHand = false;
            Dirty(item, activatableUIComponent);
        }

        // Сохраняем его в контейнере предметов модуля
        _containers.Insert(item, container);

        return item;
    }

    /// <summary>
    /// Согласно прототипу и событию создает экшен для активации данной сущности-предмета
    /// </summary>
    /// <returns>Сущность экшена</returns>
    private EntityUid CreateAction(EntityUid item, EntityUid module, BaseActionEvent assignedEvent, EntProtoId actionProto)
    {
        var actionEnt = Spawn(actionProto);
        // Подгружаем спрайт для экшена в виде модуля
        _actions.SetIcon(actionEnt, new SpriteSpecifier.EntityPrototype(MetaData(module).EntityPrototype!.ID));
        // Заготовка события для экшена
        _actions.SetEvent(actionEnt, assignedEvent);
        // Устанавливаем сущность предмета в качестве иконки
        _actions.SetEntityIcon(actionEnt, item);

        // Даем экшену название и описание предмета
        _metadata.SetEntityName(actionEnt, MetaData(item).EntityName);
        _metadata.SetEntityDescription(actionEnt, MetaData(item).EntityDescription);
        return actionEnt;
    }

    /// <summary>
    /// Добавляет экшен в шасси и сохраняет его в контейнере модуля
    /// </summary>
    private void AssignAction(EntityUid chassis, Entity<BorgModuleInnateComponent> module, EntityUid action)
    {
        // Добавляем экшн в список экшенов и в список компача
        _actionContainer.AddAction(module.Owner, action);
        _actions.AddAction(chassis, action, module.Owner);
        module.Comp.Actions.Add(action);
    }

    /// <summary>
    /// Обработчик события использования предмета как будто он в руке
    /// </summary>
    private void OnInnateUseItem(Entity<BorgModuleInnateComponent> ent, ref ModuleInnateUseItemEvent args)
    {
        _interactions.UseInHandInteraction(args.Performer, args.Item, false, true);
        args.Handled = true;
    }

    /// <summary>
    /// Обработчик события использования предмета как будто он в руке
    /// </summary>
    private void OnInnateToggleItem(Entity<BorgModuleInnateComponent> ent, ref ModuleInnateToggleItemEvent args)
    {
        // Пытаемся взаимодействовать
        if (!_interactions.UseInHandInteraction(args.Performer, args.Item, false, true))
            return;

        // Обновляем состояние в соответствии с текущим
        var wasToggled = ent.Comp.ToggledOn.Contains(args.Item);
        if (!wasToggled)
            ent.Comp.ToggledOn.Add(args.Item);
        else
            ent.Comp.ToggledOn.Remove(args.Item);

        args.Handled = true;
    }

    /// <summary>
    /// Обработчик события использования предмета на заданной цели
    /// </summary>
    private void OnInnateInteractionItem(Entity<BorgModuleInnateComponent> ent, ref ModuleInnateInteractionItemEvent args)
    {
        _interactions.InteractUsing(
            args.Performer,
            args.Item,
            args.Target,
            Transform(args.Target).Coordinates,
            false,
            false,
            false
        );
        args.Handled = true;
    }
}

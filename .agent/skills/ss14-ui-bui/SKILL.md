---
name: SS14 UI BUI
description: Практический гайд по Bound User Interface (BUI) в SS14: архитектура, сетевые сообщения, валидация ввода, предикция через component state, lifecycle окна и рабочие паттерны сервер-клиент. Используй при разработке и рефакторинге entity-bound интерфейсов.
---

# Bound User Interface (BUI) в SS14

Этот skill покрывает только BUI (entity-bound UI) 🙂
XAML-вёрстку, EUI и стиль-систему веди в отдельных skill.

## Когда выбирать BUI

Выбирай BUI, когда интерфейс:

- жёстко привязан к конкретной сущности;
- должен открываться/закрываться по взаимодействию с этой сущностью;
- обменивается typed-сообщениями с серверной логикой этой же сущности.

Не выбирай BUI, когда нужен глобальный админский/сессионный интерфейс без привязки к сущности (это EUI).

## Ментальная модель BUI

BUI состоит из 4 слоёв:

1. `UiKey` + typed-сообщения (`BoundUserInterfaceMessage`) + optional `BoundUserInterfaceState` в shared.
2. Прототипная регистрация интерфейса (`UserInterface.interfaces`) и `ActivatableUI.key`.
3. Серверная обработка событий/сообщений через `SharedUserInterfaceSystem` и систему фичи.
4. Клиентский `BoundUserInterface`, который создаёт окно, отправляет сообщения и обновляет UI.

## Базовый контракт (shared)

```csharp
// Ключ конкретного BUI.
[NetSerializable, Serializable]
public enum BatteryUiKey : byte
{
    Key,
}

// Сообщение клиента на сервер (кнопка/переключатель).
[Serializable, NetSerializable]
public sealed class BatterySetInputBreakerMessage(bool on) : BoundUserInterfaceMessage
{
    public bool On = on;
}

// Полный UI-state (если действительно нужен отдельный BUI-state).
[Serializable, NetSerializable]
public sealed class BatteryBuiState : BoundUserInterfaceState
{
    public bool CanCharge;
    public float Charge;
    public float Capacity;
}
```

Паттерн:
- делай сообщения узкими и предметными (`SetX`, `ToggleY`), не “универсальными”.

Анти-паттерн:
- один “всё-в-одном” message с десятками nullable-полей.

## Привязка в прототипе

```yaml
# Связка ключа и client-side класса BUI
- type: UserInterface
  interfaces:
    enum.GasVolumePumpUiKey.Key:
      type: GasVolumePumpBoundUserInterface

# Чем игрок открывает этот ключ
- type: ActivatableUI
  key: enum.GasVolumePumpUiKey.Key
```

Паттерн:
- `ActivatableUI.key` и ключ в `UserInterface.interfaces` всегда должны совпадать.

Анти-паттерн:
- зарегистрировать `UserInterface`, но забыть `ActivatableUI` (или наоборот).

## Server-side обработка: правильная схема

Используй `Subs.BuiEvents<TComp>(uiKey, ...)` для подписки на BUI-события и сообщения.

```csharp
public override void Initialize()
{
    base.Initialize();

    Subs.BuiEvents<BatteryInterfaceComponent>(BatteryUiKey.Key, subs =>
    {
        // Сообщения пользователя.
        subs.Event<BatterySetInputBreakerMessage>(HandleSetInputBreaker);
        subs.Event<BatterySetOutputBreakerMessage>(HandleSetOutputBreaker);
    });
}

private void HandleSetInputBreaker(Entity<BatteryInterfaceComponent> ent, ref BatterySetInputBreakerMessage args)
{
    var netBattery = Comp<PowerNetworkBatteryComponent>(ent);
    netBattery.CanCharge = args.On; // Меняем доменную модель
}
```

Рекомендация:
- обновляй UI только при открытом интерфейсе (`IsUiOpen`) и только при реальном изменении данных.

## Client-side BUI: lifecycle

```csharp
public sealed class GasVolumePumpBoundUserInterface : BoundUserInterface
{
    private GasVolumePumpWindow? _window;

    protected override void Open()
    {
        base.Open();

        // Helper создаёт окно, открывает, связывает Close -> bui.Close(), регистрирует позицию.
        _window = this.CreateWindow<GasVolumePumpWindow>();

        _window.ToggleStatusButtonPressed += OnToggle;
        _window.PumpTransferRateChanged += OnRateChanged;
        Update();
    }

    private void OnToggle()
    {
        if (_window == null)
            return;

        SendPredictedMessage(new GasVolumePumpToggleStatusMessage(_window.PumpStatus));
    }
}
```

Паттерн:
- подписки UI-элементов делай в `Open()`, а не в конструкторе.

Анти-паттерн:
- прямой `new Window().OpenCentered()` без `CreateWindow<T>()` и без привязки закрытия к BUI.

## Предикция: современный путь (предпочтительно)

Приоритетный подход: UI читает уже сетевые поля компонента, а не дублирует всё в `BoundUserInterfaceState`.

### 1) Компонент поднимает `AfterAutoHandleStateEvent`

```csharp
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class GasVolumePumpComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    [DataField, AutoNetworkedField]
    public float TransferRate = Atmospherics.MaxTransferRate;
}
```

### 2) Клиентская система обновляет открытый BUI из component state

```csharp
public override void Initialize()
{
    base.Initialize();
    SubscribeLocalEvent<GasVolumePumpComponent, AfterAutoHandleStateEvent>(OnPumpState);
}

private void OnPumpState(Entity<GasVolumePumpComponent> ent, ref AfterAutoHandleStateEvent args)
{
    if (_ui.TryGetOpenUi(ent.Owner, GasVolumePumpUiKey.Key, out var bui))
        bui.Update(); // UI читает актуальный компонент
}
```

### 3) BUI отправляет ввод через `SendPredictedMessage`

```csharp
private void OnPumpTransferRatePressed(string value)
{
    var rate = UserInputParser.TryFloat(value, out var parsed) ? parsed : 0f;
    SendPredictedMessage(new GasVolumePumpChangeTransferRateMessage(rate));
}
```

## Когда нужен `BoundUserInterfaceState`

Используй `SetUiState(...)`, если:

- состояние тяжело/дорого собрать на клиенте из имеющихся компонентных полей;
- нужен server-authoritative snapshot сложной агрегированной модели;
- нужно раздать состояние только открывшим интерфейс актёрам.

Если всё нужное уже `AutoNetworkedField`, предпочитай компонентный путь.

## Высокочастотный ввод (слайдеры и т.п.)

Для “шумного” ввода используй связку:

- `InputCoalescer<T>` для склейки множества UI-событий;
- `IBuiPreTickUpdate` + `BuiPreTickUpdateSystem` для отправки не чаще раза за тик;
- `BuiPredictionState` для повторного наложения неподтверждённых клиентских сообщений на входящий серверный state.

```csharp
void IBuiPreTickUpdate.PreTickUpdate()
{
    if (_chargeRateCoalescer.CheckIsModified(out var chargeRateValue))
        _pred!.SendMessage(new BatterySetChargeRateMessage(chargeRateValue));
}
```

Паттерн:
- коалесцируй значения ползунков и отправляй пакетно на tick.

Анти-паттерн:
- слать `SendMessage` на каждый pixel-move слайдера.

## Безопасность и валидация сообщений

По умолчанию `InterfaceData.RequireInputValidation = true`, и это правильно ✅

На каждый входящий BUI-message движок поднимает `BoundUserInterfaceMessageAttempt`, где проверяются:

- возможность взаимодействия (`CanInteract`, `CanComplexInteract`);
- доступность/дистанция;
- single-user ограничения и другие доменные правила.

Выключай `RequireInputValidation` только в строго обоснованных кейсах.

## Дистанция и автозакрытие

`SharedUserInterfaceSystem` делает range-check открытых интерфейсов и закрывает BUI при выходе из радиуса.

Управляй этим через `InterfaceData.InteractionRange` и, при необходимости, через `BoundUserInterfaceCheckRangeEvent`.

Паттерн:
- ставь разумный `InteractionRange` для физически близких устройств.

Анти-паттерн:
- без причины выставлять большой range и получать “дистанционное управление всем”.

## Критические паттерны

- `UiKey`/messages/state держи в shared и делай строго типизированными.
- UI-кнопки поднимают сообщения; бизнес-логика живёт в системе, не в окне.
- Для предикции сначала пробуй component-state + `AfterAutoHandleStateEvent`.
- Обновляй UI только если он реально открыт (`TryGetOpenUi`).
- Используй `this.CreateWindow<T>()` для корректного lifecycle окна.
- Подписки на BUI-события группируй через `Subs.BuiEvents`.

## Анти-паттерны

- Дублировать одни и те же данные и в компоненте, и в большом BUI-state без причины.
- Прямо менять компоненты из окна/контрола, обходя сообщения.
- Спамить `SetUiState` каждый кадр без guard-проверок изменений.
- Игнорировать валидацию ввода и range-ограничения.
- Смешивать BUI и EUI в одном сценарии без архитектурной причины.

## Чеклист перед PR

- Ключ UI зарегистрирован и в `UserInterface`, и в `ActivatableUI`.
- Сообщения `BoundUserInterfaceMessage` минимальные и domain-driven.
- Серверная система валидирует и обрабатывает сообщения через BUI-events.
- Клиентский BUI не содержит бизнес-логики, только отображение и отправку ввода.
- Для предикции используется `SendPredictedMessage`, где это нужно.
- Нет лишнего сетевого дубляжа state-данных.


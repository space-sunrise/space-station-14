---
name: SS14 Netcode Architecture
description: Architecture guide for networking in Space Station 14 — Lidgren integration, NetManager abstraction, message system, game state synchronization, PVS, network events, and component networking
---

# Сетевая архитектура SS14

## Обзор стека

Сетевой стек SS14 состоит из нескольких уровней абстракции, от низкоуровневого транспорта до логики синхронизации игрового состояния:

```
┌─────────────────────────────────────────────┐
│            Контент (системы игры)            │
│  RaiseNetworkEvent / ComponentState / Dirty  │
├─────────────────────────────────────────────┤
│         ServerGameStateManager              │
│         ClientGameStateManager              │
│         GameStateProcessor                  │
├─────────────────────────────────────────────┤
│              PVS System                     │
│     (фильтрация видимости сущностей)        │
├─────────────────────────────────────────────┤
│       NetMessage / MsgState / MsgGroups     │
│    (типизированные сообщения с сериализацией)│
├─────────────────────────────────────────────┤
│            NetManager                       │
│    (обёртка над Lidgren, управление         │
│     каналами, диспатч пакетов)              │
├─────────────────────────────────────────────┤
│     Lidgren (NetPeer/NetServer/NetClient)   │
│          UDP + reliability layer            │
└─────────────────────────────────────────────┘
```

## Lidgren: транспортный уровень

SS14 использует библиотеку **Lidgren** для UDP-транспорта. Движок не использует Lidgren напрямую в игровом коде — вся работа идёт через абстракции `INetManager` и `NetMessage`.

### Конфигурация Lidgren

Движок настраивает Lidgren через CVars:

- **MTU** — Maximum Transmission Unit. Пакеты больше MTU фрагментируются. По умолчанию ~1408 байт
- **Симуляция сетевых проблем** — для тестирования:
  - `net.fakeloss` — процент потерянных пакетов (0.0–1.0)
  - `net.fakelagmin` — минимальная задержка в секундах
  - `net.fakelagrand` — случайная добавочная задержка
  - `net.fakeduplicates` — шанс дублирования пакета
- **Буферы** — `net.sendbuffersize`, `net.receivebuffersize`
- **AppIdentifier** — строка для идентификации протокола (через `CVars.NetLidgrenAppIdentifier`)

### Синхронизация времени

При инициализации движок синхронизирует `NetTime` Lidgren с собственным `RealTime`:

```csharp
NetTime.SetNow(_timing.RealTime.TotalSeconds);
```

Это критически важно для корректной работы таймингов пакетов.

## NetMessage: система типизированных сообщений

Все сетевые сообщения наследуются от абстрактного класса `NetMessage`.

### Группы сообщений (MsgGroups)

Каждое сообщение принадлежит к группе, определяющей способ доставки по умолчанию:

| Группа | Доставка | Назначение |
|--------|----------|------------|
| `Core` | ReliableUnordered | Подключения, отключения, тики |
| `Entity` | Unreliable | Синхронизация игрового состояния |
| `String` | ReliableOrdered | Чат, текстовые сообщения |
| `Command` | ReliableUnordered | Команды клиент → сервер |
| `EntityEvent` | ReliableOrdered | ECS-события между сервером и клиентом |

### Способы доставки Lidgren

- **Unreliable** — без гарантии доставки, без порядка. Самый быстрый
- **ReliableUnordered** — гарантия доставки, без гарантии порядка
- **ReliableOrdered** — гарантия доставки и порядка (в пределах sequence channel)

### Sequence Channels

Lidgren поддерживает до 32 каналов для ordered-сообщений. Каналы 16+ зарезервированы для внутренних нужд движка. Сообщения в разных каналах упорядочиваются независимо.

### Регистрация и обработка

```csharp
// Регистрация сообщения с обработчиком
_networkManager.RegisterNetMessage<MsgStateAck>(HandleStateAck);

// Регистрация без обработчика (только для отправки)
_networkManager.RegisterNetMessage<MsgState>();
```

## NetManager: управление сетью

`NetManager` — центральная точка сетевой подсистемы. Он реализует оба интерфейса: `IClientNetManager` и `IServerNetManager`.

### Основной цикл обработки

Метод `ProcessPackets()` вызывается каждый кадр и выполняет:

1. Читает все входящие сообщения из Lidgren peers
2. Классифицирует по `NetIncomingMessageType`:
   - `Data` → десериализует в `NetMessage`, вызывает зарегистрированный callback
   - `StatusChanged` → обрабатывает подключения/отключения
   - `VerboseDebugMessage`, `WarningMessage`, `ErrorMessage` → логирует
3. Обновляет метрики Prometheus (отправленные/полученные пакеты, байты, ресенды)
4. Перерабатывает использованные буферы Lidgren

### StringTable

Для экономии трафика имена типов сообщений передаются как числовые ID через `StringTable`. При подключении сервер и клиент синхронизируют таблицу соответствий.

### Каналы (NetChannel)

Каждое подключение представлено `NetChannel`, который хранит:
- Lidgren `NetConnection`
- Идентификатор пользователя (`NetUserId`)
- Ping
- Состояние аутентификации

## Синхронизация игрового состояния (GameState)

### Структура GameState

`GameState` — снимок состояния игры на конкретном тике. Содержит:

- **EntityStates** — изменённые состояния компонентов сущностей
- **PlayerStates** — состояния сессий игроков
- **EntityDeletions** — удалённые сущности
- **FromSequence / ToSequence** — диапазон тиков (дельта-состояние)
- **LastProcessedInput** — последний обработанный сервером ввод клиента

### Дельта-состояния

Сервер отправляет **дельту** между двумя тиками, а не полное состояние каждый раз. `FromSequence = 0` означает полное состояние (после подключения или при ошибке).

### Сжатие и надёжность (MsgState)

Класс `MsgState` реализует умную логику отправки:

- **Сжатие ZStd** — состояния > 256 байт сжимаются
- **Адаптивная надёжность** — если итоговый размер превышает Lidgren MTU (~1408 байт), сообщение отправляется как Reliable. Иначе — Unreliable
- **Принудительная надёжность** — `ForceSendReliably` для состояний, которые обязан получить клиент

### Поток сервер → клиент

1. **Сервер:** `ServerGameStateManager.SendGameStateUpdate()` → `PvsSystem.SendGameStates(players)`
2. PVS рассчитывает видимые сущности для каждого игрока
3. Для каждого игрока формируется индивидуальный `GameState`
4. Состояние сериализуется, сжимается, отправляется как `MsgState`
5. **Клиент:** получает `MsgState`, десериализует, помещает в `GameStateProcessor`
6. Клиент отвечает `MsgStateAck` подтверждением

### Клиентская обработка состояний

`GameStateProcessor` буферизует полученные состояния и выдаёт их для применения:

- **Буфер состояний** — хранит несколько состояний наперёд для сглаживания
- **Target buffer size** — целевой размер буфера, влияет на задержку vs плавность
- **Tick timing adjustment** — клиент слегка ускоряет/замедляет свой тик для синхронизации с сервером

### Запрос полного состояния

При ошибке (отсутствие метаданных сущности, рассинхронизация) клиент запрашивает полное состояние через `MsgStateRequestFull`. Сервер отвечает состоянием с `FromSequence = 0`.

## Potentially Visible Set (PVS)

PVS — серверная система оптимизации, определяющая какие сущности видит каждый клиент.

### Принцип работы

- Мир разделён на **чанки**
- Сервер отслеживает позицию каждого игрока
- Сущности в пределах определённого радиуса входят в PVS игрока
- Только данные видимых сущностей отправляются клиенту

### Жизненный цикл видимости

1. **Сущность входит в PVS** — клиент получает полное состояние, флаг `Detached` снимается
2. **Сущность в PVS** — клиент получает дельта-обновления
3. **Сущность покидает PVS** — сервер отправляет `MsgStateLeavePvs`, клиент устанавливает флаг `MetaDataFlags.Detached`

### Detached сущности

Когда сущность выходит из PVS:
- Она **не удаляется** — остаётся в памяти клиента
- Устанавливается флаг `MetaDataFlags.Detached`
- Сущность перемещается в «null-space» (убирается из broadphase)
- При повторном входе в PVS флаг снимается, сущность возвращается на место

### PVS Overrides

Для сущностей, которые должны быть видны всегда или конкретным игрокам:

```csharp
// Видна всем клиентам, независимо от расстояния
_pvs.AddGlobalOverride(entityUid);

// Видна конкретной сессии
_pvs.AddSessionOverride(entityUid, session);
```

Типичные применения: UI-сущности игрока, глобальные контроллеры, объекты «на руках» персонажа.

## Сетевые события (Network Events)

### Два типа событий

В SS14 есть **локальные** и **сетевые** события. Это принципиально разные механизмы:

```csharp
// Локальное событие — только в текущем процессе
RaiseLocalEvent(uid, new MyLocalEvent());
SubscribeLocalEvent<MyComponent, MyLocalEvent>(OnMyEvent);

// Сетевое событие — отправляется по сети
RaiseNetworkEvent(new MyNetEvent());
SubscribeNetworkEvent<MyNetEvent>(OnNetEvent);
```

### Серверная валидация

**Все события от клиента к серверу должны быть валидированы.** Клиент может отправить любые данные:

```csharp
// ❌ Опасно — нет валидации
private void OnClientEvent(MyEvent ev, EntitySessionEventArgs args)
{
    DoAction(ev.TargetEntity); // Клиент мог подделать TargetEntity!
}

// ✅ Безопасно — с валидацией
private void OnClientEvent(MyEvent ev, EntitySessionEventArgs args)
{
    if (!HasComp<MyComponent>(ev.TargetEntity))
        return;
    if (!_interaction.InRangeUnobstructed(args.SenderSession, ev.TargetEntity))
        return;
    DoAction(ev.TargetEntity);
}
```

Валидация должна происходить на сервере после получения сообщения и на клиенте до его отправки.
Код валидации должен находиться в shared системе/хелпер классе для предотвращения дублирования логики!

### Паттерны отправки

```csharp
// Сервер → всем клиентам
RaiseNetworkEvent(new MyEvent());

// Сервер → конкретному клиенту
RaiseNetworkEvent(new MyEvent(), session);

// Сервер → всем, кроме одного
var filter = Filter.Broadcast().RemovePlayerByAttachedEntity(uid);
RaiseNetworkEvent(new MyEvent(), filter);
```

## EntityUid vs NetEntity

В SS14 две системы идентификации сущностей:

| | EntityUid | NetEntity |
|---|---|---|
| Где используется | Локально в процессе | При передаче по сети |
| Стабильность | Разный на клиенте и сервере | Одинаковый везде |
| Хранение | В компонентах | Только для передачи |

### Конвертация

```csharp
// EntityUid → NetEntity (для отправки по сети)
var netEntity = GetNetEntity(uid);

// NetEntity → EntityUid (при получении из сети)
var uid = GetEntity(netEntity);
```

### Правила

- **Компоненты** хранят `EntityUid`, не `NetEntity`
- При сетевой синхронизации (`[AutoNetworkedField]`) конвертация происходит автоматически
- Для ручного networking используйте `GetNetEntity()`/`GetEntity()` при сериализации/десериализации

## Component Networking: сетевая синхронизация компонентов

### Автоматическая синхронизация

Это основной и рекомендуемый способ. Подробно описан в скилле **SS14 ECS Components**:

```csharp
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MyComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Value = 1f;
}
```

### Ключевой момент: Dirty()

После изменения данных компонента в коде **обязательно** вызывайте `Dirty()`:

```csharp
comp.Value = newValue;
Dirty(uid, comp); // Без этого клиент НЕ получит обновление!
```

`Dirty()` помечает компонент как изменённый. PVS-система включит его в следующий `GameState` для отправки клиентам.

> **⚠️ Частая ошибка:** забыть `Dirty()` после изменения компонента. Данные изменятся на сервере, но клиент не получит обновление, что приведёт к рассинхронизации.

### SendOnlyToOwner

Для данных, которые должен видеть только «владелец» сущности (например, инвентарь):

```csharp
[DataField, AutoNetworkedField]
[Access(Other = AccessPermissions.ReadWrite)]
public int SecretValue
{
    get => _secretValue;
    set => _secretValue = value;
}
```

### SessionSpecific

Для данных, которые отличаются для разных клиентов (например, видимость замаскированного персонажа).

### NetworkedComponent только для Shared

`[NetworkedComponent]` можно ставить **только на компоненты в Shared-проекте**. Если поставить на компонент в Client или Server проекте, он молча не будет работать — без ошибок компиляции, но и без синхронизации.

### IRobustCloneable для ссылочных типов

Если сетевое поле содержит ссылочный тип (класс, коллекцию), тип должен реализовать `IRobustCloneable`, чтобы предикшн мог корректно сохранять и восстанавливать состояние:

```csharp
[DataField, AutoNetworkedField]
public List<string> Items = new(); // List<T> уже реализует IRobustCloneable
```

## Отладка сетевых проблем

### CVars для симуляции

Используйте в консоли клиента или сервера:

```
net.fakeloss 0.1       // Потеря 10% пакетов
net.fakelagmin 0.1     // Минимум 100мс задержки
net.fakelagrand 0.05   // + случайные 0-50мс
net.fakeduplicates 0.05 // 5% дублей
```

### net.predict

```
net.predict false  // Отключить клиентскую предикцию — видно реальную задержку
net.predict true   // Включить обратно
```

### Метрики Prometheus

Движок экспортирует метрики:
- `robust_net_sent_packets` / `robust_net_recv_packets`
- `robust_net_sent_bytes` / `robust_net_recv_bytes`
- `robust_net_resent_delay` / `robust_net_resent_hole`
- `robust_net_dropped`

## Связь с другими скиллами

- **SS14 ECS Components** — детали атрибутов `[NetworkedComponent]`, `[AutoGenerateComponentState]`, `[AutoNetworkedField]`
- **SS14 ECS Systems** — паттерны работы с сетевыми событиями из систем
- **SS14 ECS Entities** — `EntityUid` vs `NetEntity`, контейнеры и сетевая идентификация
- **SS14 Prediction** — как клиент использует полученные состояния для предсказания

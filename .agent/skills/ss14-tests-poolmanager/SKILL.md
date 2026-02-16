---
name: SS14 Tests PoolManager
description: Глубокий гайд по инфраструктуре интеграционных тестов SS14: PoolManager, жизненный цикл TestPair, синхронизация server/client, эмуляция действий и подводные камни.
---

# Инфраструктура интеграционных тестов SS14

## Когда использовать

Используй этот skill, когда нужно:
- понять, как реально работает `PoolManager` и переиспользование пар server/client;
- безопасно настраивать `PoolSettings` и `PairSettings`;
- корректно синхронизировать тики сервера и клиента;
- эмулировать действия игрока (ввод, UI, BUI, интеракции);
- разобрать типичные проблемы пула и flaky-поведение.

Если задача именно про проектирование и написание нового теста как продукта, используй отдельный skill `ss14-tests-authoring`. Здесь фокус только на инфраструктуре и механике выполнения.

Фильтр качества перед переносом паттерна в работу:
- не использовать примеры старше 2 лет;
- не использовать фрагменты, где есть TODO/проблемные комментарии по теме тестов;
- если сомневаешься, выбирай более свежий и более низкоуровневый пример из движка.

## Модель пула: что важно помнить 🙂

`PairSettings` задает политику жизни пары:
- `Destructive`: тест может «сломать» пару.
- `Fresh`: требовать новую пару.
- `Connected`: нужна ли активная сессия client↔server.
- `NoLoadTestPrototypes`: не загружать тестовые прототипы (дорого и не переиспользуемо).
- `Dirty`: запрещает fast-recycle.

Критичные вычисляемые свойства:

```csharp
public virtual bool MustNotBeReused => Destructive || NoLoadTestPrototypes;
public virtual bool MustBeNew => Fresh || NoLoadTestPrototypes;
```

Следствие:
- `MustBeNew == true` => пул создает новую пару;
- `MustNotBeReused == true` => пара после теста не возвращается в reuse.

## Как `PoolManager` выбирает и готовит пару

Алгоритм по сути такой:
1. Если `MustBeNew`, создается новая пара.
2. Иначе берется подходящая из пула.
3. Если `CanFastRecycle(...) == true`, применяется только `ApplySettings(...)`.
4. Иначе выполняется полный `RecycleInternal(...)`.
5. После выдачи пары прогоняются стабилизационные тики и синхронизация дельты тиков.

Практический вывод:
- даже после «быстрого» возврата нельзя предполагать моментальную стабильность;
- после критичных операций всегда уместны `RunTicksSync(...)` и `SyncTicks(...)`.

## Lifecycle пары и возврат в пул ⚠️

Правильный путь:
1. Взять пару.
2. Выполнить сценарий.
3. Вызвать `CleanReturnAsync()`.

Что делает `CleanReturnAsync()`:
- `Cleanup()` пары;
- очистка/возврат измененных CVars;
- проверка, что client/server живы (если тест не destructive);
- проверка runtime exceptions;
- перевод пары в `Ready` или полное уничтожение, если reuse запрещен.

Если не вызвать `CleanReturnAsync()`, сработает dirty-dispose через `DisposeAsync()`:
- пара уничтожается;
- появляется warning;
- деградирует производительность и стабильность тест-рана.

## Сервер/клиент: правильный контракт выполнения

У `IIntegrationInstance` есть строгая дисциплина:
- `WaitPost(...)` / `Post(...)`: только мутации.
- `WaitAssertion(...)` / `Assert(...)`: проверки и `Assert.That(...)`.
- `WaitIdleAsync()`: гарантирует, что очередь обработана.

Нельзя смешивать обязанности:
- ассерты внутри `Post(...)` дают шумные и плохо читаемые падения;
- мутации в `WaitAssertion(...)` ломают причинно-следственную модель теста.

## Специфика `PoolSettings` в контенте

Варианты настройки в контентных интеграционных тестах:
- `InLobby = true` автоматически требует connected-состояние.
- `DummyTicker` используется для более легкого режима без полного round-flow.
- `Dirty = true` нужен, если тест меняет состояние раунда, готовность, лобби-фазы и т.п.
- `AdminLogsEnabled` включай только когда тест реально проверяет админ-логи.

`CanFastRecycle` в контенте дополнительно учитывает:
- `DummyTicker`,
- `Map`,
- `InLobby`.

## Эмуляция действий игрока

### 1) Ввод (keybind)

В рабочем коде это делается через `ClientFullInputCmdMessage` и `InputSystem.HandleInputCommand(...)`.

```csharp
// Эмуляция нажатия keybind в клиентском инстансе.
var funcId = inputManager.NetworkBindMap.KeyFunctionID(ContentKeyFunctions.TryPullObject);
var msg = new ClientFullInputCmdMessage(client.Timing.CurTick, client.Timing.TickFraction, funcId)
{
    State = BoundKeyState.Down,
    Coordinates = client.EntMan.GetCoordinates(targetCoords),
    Uid = client.EntMan.GetEntity(targetNetEntity),
};

await client.WaitPost(() =>
{
    inputSystem.HandleInputCommand(clientSession, ContentKeyFunctions.TryPullObject, msg);
});
```

### 2) Клик по UI-контролу

Стандартно отправляют `Down` + `Up` через `GUIBoundKeyEventArgs` и `DoGuiEvent(...)`:

```csharp
// Нажали кнопку.
await client.DoGuiEvent(control, downArgs);
await pair.RunTicksSync(1);

// Отпустили кнопку.
await client.DoGuiEvent(control, upArgs);
await pair.RunTicksSync(1);
```

### 3) BUI-сообщение

После `SendMessage(...)` обычно дают дополнительные тики на round-trip client↔server:

```csharp
await client.WaitPost(() => bui.SendMessage(msg));
await pair.RunTicksSync(15); // запас на обработку обеими сторонами
```

### 4) Серверная интеракция напрямую

Для низкоуровневых interaction-сценариев используется `UserInteraction(...)` внутри `WaitPost(...)`, а потом ожидание последствий (do-after, смена target, синхронизация).

## Паттерны

- `await using var pair = await PoolManager.GetServerClient(settings);` + явный `await pair.CleanReturnAsync();`
- После критичных шагов: `RunTicksSync(...)` и при сравнении server/client еще `SyncTicks(targetDelta: ...)`.
- Разделение: `WaitPost` для действий, `WaitAssertion` для проверок.
- Для спецкейсов глобального состояния (например, регистрация tile defs): `Pool = false`.
- Для нестандартных прототипов: `ExtraPrototypes` или `[TestPrototypes]` вместо внешних mutable-зависимостей.

## Анти-паттерны

- Полагаться на `DisposeAsync()` и не делать `CleanReturnAsync()` 😬
- Смешивать мутацию и проверки в одном callback без дисциплины.
- Проверять server/client без выравнивания тиков и затем ловить «случайный» флак.
- Ставить `Dirty = true` «на всякий случай» в каждом тесте.
- Использовать `NoLoadTestPrototypes` без реальной необходимости (дорогой путь, нет переиспользования).

## Примеры из актуального кода

### Пример A: базовый borrow/return через пул

```csharp
[Test]
public async Task RoundScenario_Works()
{
    await using var pair = await PoolManager.GetServerClient(new PoolSettings
    {
        Connected = true,
        Dirty = true,
        DummyTicker = false,
        InLobby = true,
    });

    // Стабилизируем начальное состояние пары.
    await pair.RunTicksSync(5);
    await pair.SyncTicks(targetDelta: 1);

    await pair.Server.WaitAssertion(() =>
    {
        var ticker = pair.Server.System<GameTicker>();
        Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.PreRoundLobby));
    });

    // Явный clean-return обязателен для здоровья пула.
    await pair.CleanReturnAsync();
}
```

### Пример B: корректное разделение Post и Assertion

```csharp
EntityUid entity = default;

await server.WaitPost(() =>
{
    // Только действие: создаем сущность.
    entity = server.EntMan.SpawnEntity("MobHuman", spawnCoords);
});

await server.WaitAssertion(() =>
{
    // Только проверка: убеждаемся, что сущность существует.
    Assert.That(server.EntMan.EntityExists(entity), Is.True);
});
```

### Пример C: ручной server/client старт для спецкейса

```csharp
var serverOpts = new ServerIntegrationOptions
{
    Pool = false,              // изоляция, без reuse
    ExtraPrototypes = Prototypes,
};
var clientOpts = new ClientIntegrationOptions
{
    Pool = false,
    ExtraPrototypes = Prototypes,
};

var server = StartServer(serverOpts);
var client = StartClient(clientOpts);
await Task.WhenAll(server.WaitIdleAsync(), client.WaitIdleAsync());

Assert.DoesNotThrow(() => client.SetConnectTarget(server));
await client.WaitPost(() => client.ResolveDependency<IClientNetManager>().ClientConnect(null!, 0, null!));
```

## Практические примечания из документации

- Для ускорения тест-рана полезен `COMPlus_gcServer=1` (особенно заметно на integration tests).
- В integration-тестах user data хранится в in-memory FS и не персистится между запусками.

Используй это как operational guidance, но проверяй названия CVar и поведение по текущему коду.

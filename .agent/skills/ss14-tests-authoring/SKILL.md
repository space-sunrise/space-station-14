---
name: SS14 Tests Authoring
description: Практический workflow написания собственных unit/integration тестов в архитектуре Space Station 14: от выбора стратегии до стабильных assertions и обслуживания тестов.
---

# Написание своих тестов в SS14

## Когда использовать

Используй этот skill, когда нужно:
- спроектировать новый тестовый сценарий с нуля;
- выбрать тип теста (unit, server-only integration, server+client integration);
- оформить тест так, чтобы он был стабильным и не ломал пул;
- быстро ревьюить тест на anti-patterns до запуска CI.

Внутреннее устройство `PoolManager/TestPair` подробно разбирается в `ss14-tests-poolmanager`. Здесь упор на авторский workflow и качество тестов как артефакта.

## Короткое дерево решений

1. Проверяешь чистую логику без сети/тика/IoC?
- Да: unit-тест.
- Нет: integration-тест.

2. Нужна реальная синхронизация client↔server, UI/BUI, предикция или ввод?
- Да: server+client integration.
- Нет: server-only integration может быть проще и быстрее.

3. Тест меняет глобальное состояние, которое плохо переживает reuse?
- Да: `Dirty = true` или точечно `Pool = false` для изоляции.
- Нет: оставляй reuse, это сильно ускоряет ран.

## Базовый workflow автора 🧪

1. Зафиксируй инварианты:
- что должно измениться;
- что не должно измениться;
- на какой стороне (server/client) проверяется каждое условие.

2. Выбери минимальные настройки окружения:
- `Connected`, если нужен живой клиент;
- `DummyTicker = false`, если нужен реальный round-flow;
- `InLobby = true`, если проверяется логика лобби;
- `Dirty = true`, только если тест реально портит reuse-состояние.

3. Arrange:
- создай карту/сущности/компоненты в `WaitPost(...)`;
- при необходимости подключи тестовые прототипы (`[TestPrototypes]` или `ExtraPrototypes`).

4. Act:
- выполняй действие через систему/ввод/UI;
- после action дай нужные тики для обработки.

5. Assert:
- проверяй в `WaitAssertion(...)` или вне callback после синхронизации;
- для server+client всегда учитывай дельту тиков.

6. Cleanup:
- всегда `CleanReturnAsync()` для пар из пула;
- если поднимал отдельные server/client инстансы, корректно disconnect/shutdown.

## Шаблон 1: стандартный контентный integration-тест

```csharp
[Test]
public async Task MyScenario_Works()
{
    await using var pair = await PoolManager.GetServerClient(new PoolSettings
    {
        Connected = true,
        Dirty = false,
    });

    var server = pair.Server;
    EntityUid subject = default;

    await server.WaitPost(() =>
    {
        // Arrange: создаем тестовую сущность.
        subject = server.EntMan.SpawnEntity("MobHuman", MapCoordinates.Nullspace);
    });

    await pair.RunTicksSync(5);
    await pair.SyncTicks(targetDelta: 1);

    await server.WaitAssertion(() =>
    {
        // Assert: проверка после стабилизации.
        Assert.That(server.EntMan.EntityExists(subject), Is.True);
    });

    await pair.CleanReturnAsync();
}
```

## Шаблон 2: inline-прототипы внутри теста

```csharp
[TestPrototypes]
private const string Prototypes = @"
- type: entity
  id: MyTestTarget
  components:
  - type: Physics
";

[Test]
public async Task UsesInlinePrototype()
{
    await using var pair = await PoolManager.GetServerClient();

    await pair.Server.WaitAssertion(() =>
    {
        Assert.That(pair.Server.ProtoMan.HasIndex<EntityPrototype>("MyTestTarget"), Is.True);
    });

    await pair.CleanReturnAsync();
}
```

## Шаблон 3: изолированный спецкейс без пула

Используй, когда кейс затрагивает глобальные регистрации/глубокую изоляцию.

```csharp
var server = StartServer(new ServerIntegrationOptions
{
    Pool = false,
    ExtraPrototypes = Prototypes,
});
var client = StartClient(new ClientIntegrationOptions
{
    Pool = false,
    ExtraPrototypes = Prototypes,
});

await Task.WhenAll(server.WaitIdleAsync(), client.WaitIdleAsync());
Assert.DoesNotThrow(() => client.SetConnectTarget(server));
await client.WaitPost(() => client.ResolveDependency<IClientNetManager>().ClientConnect(null!, 0, null!));
```

## Паттерны хороших тестов 🙂

- Каждый тест проверяет 1 поведенческий контракт (а не «все сразу»).
- Arrange/Act/Assert визуально разделены.
- Все мутации обернуты в `WaitPost`, все проверки в `WaitAssertion`.
- Есть явная синхронизация тиков до cross-side assertions.
- Сложные сценарии опираются на helper-методы (ввод, do-after, UI) вместо копипасты.
- Название теста описывает наблюдаемое поведение (`X_WhenY_ShouldZ` или эквивалент).

## Анти-паттерны

- «Гигантский» тест с десятком независимых целей.
- Магические `RunTicks(123)` без объяснения, почему именно столько.
- `Dirty = true` и `Pool = false` по умолчанию без причины.
- Ассерты в `Post(...)` и мутации в `WaitAssertion(...)`.
- Опора на документацию вразрез с текущим кодом.
- Игнорирование cleanup и надежда на автоматический dispose 😬

## Примеры из актуального кода

### Пример A: сценарий, где нужны lobby/in-round переходы

```csharp
await using var pair = await PoolManager.GetServerClient(new PoolSettings
{
    Dirty = true,       // тест меняет фазу игры
    DummyTicker = false,
    Connected = true,
    InLobby = true,
});

var ticker = pair.Server.System<GameTicker>();
await pair.Server.WaitAssertion(() =>
{
    Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.PreRoundLobby));
});
```

### Пример B: проверка интеракции через системный вызов

```csharp
await server.WaitPost(() =>
{
    // Act: серверная интеракция пользователя с целью.
    interactionSystem.UserInteraction(user, server.Transform(target).Coordinates, target);
});

await server.WaitAssertion(() =>
{
    // Assert: ожидаемый обработчик действительно сработал.
    Assert.That(interactHand, Is.True);
});
```

### Пример C: UI/BUI шаги с учетом сетевого RTT

```csharp
await client.WaitPost(() => bui.SendMessage(message));

// Даем время на client->server->client обработку.
await pair.RunTicksSync(15);

await client.WaitAssertion(() =>
{
    Assert.That(expectedUiState, Is.True);
});
```

## Мини-чеклист перед PR

- Тест проходит локально минимум 3 прогона подряд.
- Нет прямой зависимости от порядка выполнения других тестов.
- Нет скрытой мутации глобального состояния без отката.
- Cleanup явный и корректный.
- Параметры окружения минимальны (ничего лишнего).

## Практические заметки из docs

- `COMPlus_gcServer=1` обычно ускоряет integration-раны.
- User data в integration-режиме in-memory и не сохраняется между запусками.

Используй это как operational note, но финальное решение всегда сверяй по актуальной реализации кода.

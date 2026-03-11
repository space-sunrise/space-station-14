---
name: SS14 Prediction
description: Architecture guide for client-side prediction in Space Station 14 — prediction loop, timing properties, predicted entities, state reconciliation, randomness, and common pitfalls
---

# Предикшн (Client-Side Prediction) в SS14

## Зачем нужна предикция

В онлайн-игре между нажатием кнопки и ответом сервера проходит время (RTT). Без предикции игрок бы нажимал «двигаться» и видел результат через 50–200 мс. Предикция решает это: клиент **сразу** применяет действие локально, а потом корректирует результат когда приходит серверное состояние.

### Цена предикции

- Код должен быть в **Shared** проекте (общий для клиента и сервера)
- Нужна специальная обработка побочных эффектов (звуки, попапы)
- Случайность требует детерминированного генератора
- Ссылочные типы в компонентах должны реализовать `IRobustCloneable`
- Возможны «мисПредикты» — моменты когда клиент предсказал неправильно

## Основной цикл предикции

Каждый кадр клиент выполняет следующую последовательность:

```
┌──────────────────────────────────────────────────────┐
│           ClientGameStateManager.ApplyGameState()     │
│                                                      │
│  1. ResetPredictedEntities()                         │
│     └─ Откатить все предсказанные изменения          │
│        к последнему подтверждённому серверному        │
│        состоянию                                     │
│                                                      │
│  2. ApplyGameState(curState, nextState)               │
│     └─ Применить новое серверное состояние            │
│     └─ Создать новые сущности                        │
│     └─ Обработать входы/выходы PVS                   │
│     └─ Удалить помеченные сущности                   │
│                                                      │
│  3. MergeImplicitData()                              │
│     └─ Создать «фейковые» начальные состояния        │
│        для новых сущностей (из прототипов)            │
│                                                      │
│  4. PredictTicks(predictionTarget)                   │
│     └─ Проиграть все кандидатные тики заново         │
│     └─ Применить pending-ввод и события              │
│     └─ Запустить EntitySystemManager.TickUpdate()    │
│                                                      │
│  5. TickUpdate (основной тик текущего кадра)          │
│     └─ Финальный тик = predictionTarget              │
└──────────────────────────────────────────────────────┘
```

### Расчёт predictionTarget

```
predictionTarget = LastProcessedTick + TargetBufferSize + lag_ticks + PredictTickBias
```

Где `lag_ticks = ceil(TickRate × ping / TimeScale)`. Клиент предсказывает на столько тиков вперёд, чтобы его ввод успел дойти до сервера и вернуться.

## ResetPredictedEntities: откат предсказаний

Перед применением нового серверного состояния **все предсказанные изменения откатываются**. Это происходит через `ClientDirtySystem`:

### Как отслеживаются изменения

- `ClientDirtySystem` подписывается на `EntityDirtied` событие
- Когда во время предикции (`InPrediction = true`) изменяется компонент **серверной** сущности (не клиентской), она добавляется в `DirtyEntities`
- Удалённые компоненты записываются в `RemovedComponents`

### Откат включает

1. **Удаление предсказанных сущностей** — все сущности с `PredictedSpawnComponent` удаляются и пересоздаются при повторном прогоне
2. **Восстановление состояний компонентов** — для каждой «грязной» сущности компоненты сбрасываются к последнему серверному состоянию через `ComponentHandleState`
3. **Удаление добавленных компонентов** — компоненты, добавленные во время предикции (с `CreationTick > LastRealTick`), удаляются
4. **Восстановление удалённых компонентов** — компоненты, удалённые во время предикции, пересоздаются с серверным состоянием
5. **Сброс контактов физики** — `PhysicsSystem.ResetContacts()`

## Свойства IGameTiming для предикции

### IsFirstTimePredicted

Возвращает `true` если текущий тик предсказывается **впервые**. При повторных прогонах (re-prediction) возвращает `false`.

**Критически важно для побочных эффектов:**

```csharp
// ✅ Правильно — звук играется один раз
if (_timing.IsFirstTimePredicted)
    _audio.PlayPvs(sound, uid);

// ✅ Более правильно - использовать Predicted методы
_audio.PlayPredicted(sound, source, receiver);
// source - источник звука
// receiver - сущность "клиента", которая сделала звук.
// По умолчанию клиент игрока, который спровоцировал звук продублирует звук(локально и от сервера). Чтобы это избежать передаем спроцировавшего звук игрока в метод
// Из-за этой особенности иногда Predicted метод может неправильно работать!

// ❌ Неправильно — звук будет играться при каждом re-prediction!
_audio.PlayPvs(sound, uid);
```

### InPrediction

`true` когда `CurTick > LastRealTick` **и** `ApplyingState = false`. Означает, что клиент сейчас выполняет предсказание.

### ApplyingState

`true` когда клиент применяет серверное состояние (между `ResetPredictedEntities` и началом `PredictTicks`). В этот момент нельзя создавать побочные эффекты.

### CurTick vs LastRealTick vs LastProcessedTick

- **CurTick** — текущий тик симуляции. Меняется при предикции
- **LastRealTick** — последний тик, подтверждённый сервером
- **LastProcessedTick** — последний тик, для которого было применено серверное состояние

## Предсказанные сущности

### Предсказанное создание (Spawn)

Используйте специальные методы для создания сущностей на клиенте:

```csharp
// Создание сущности с предикцией
var entity = PredictedSpawn(prototype, coordinates);
```

При применении серверного состояния:
1. Все сущности с `PredictedSpawnComponent` удаляются
2. Если сервер подтвердил создание, сущность появится из серверного состояния
3. При следующем прогоне предикции сущность создастся заново

### Предсказанное удаление

Удаление серверных сущностей на клиенте **не поддерживается напрямую**. `ClientDirtySystem` выдаст ошибку, если обнаружит удаление серверной сущности во время предикции:

```
// Это вызовет ошибку:
// "Predicting the deletion of a networked entity: ..."
QueueDel(serverEntity); // ❌ нельзя в предикции!
```

Вместо этого используйте паттерн «состояние как компонент» — добавляйте/удаляйте компоненты для изменения состояния сущности.

## Предсказанные звуки и попапы

### Звуки

```csharp
// ✅ Plays once: на клиенте при первом предикте, на сервере для остальных
_audio.PlayPredicted(sound, uid, user);

// ✅ Альтернатива с ручным контролем
if (_timing.IsFirstTimePredicted)
    _audio.PlayPvs(sound, uid);
```

### Попапы

```csharp
// ✅ Показывает один раз
_popup.PopupPredicted(message, uid, user, PopupType.Medium);

// ✅ Вариант только для локального игрока
if (_timing.IsFirstTimePredicted)
    _popup.PopupEntity(message, uid, user);
```

## Предсказанная случайность

Обычный `IRobustRandom` **не детерминирован** между прогонами предикции. Каждый раз при re-prediction он даст другие числа, что вызовет миспредикт.

### Решения

#### Проект Sunrise: RandomPredictedSystem

В проекте Sunrise используется своя система предсказуемого рандома `RandomPredictedSystem` с привязкой к `EntityUid`:

```csharp
// ✅ Seed привязан к EntityUid + текущему тику
var random = _randomPredicted.NextForEntity(uid, 0, 100);

// ✅ Случайный float
var value = _randomPredicted.NextFloatForEntity(uid, 0f, 1f);

// ✅ Проверка вероятности
if (_randomPredicted.ProbForEntity(uid, 0.3f))
    // 30% шанс

// ✅ Случайный элемент из списка
var item = _randomPredicted.PickForEntity(uid, myList);
```

Система создаёт `System.Random` с seed, основанным на `EntityUid` и текущем `CurTick`, поэтому при повторных прогонах предикции для того же тика результат будет одинаковым.

#### Ванильный подход: ручное создание Random

```csharp
// ✅ Детерминированный seed
var random = new System.Random((int)(uid.Id + _timing.CurTick.Value));
var result = random.Next(0, 100);
```

#### Что НЕ делать

```csharp
// ❌ IRobustRandom — разные результаты при каждом прогоне предикции
var result = _random.Next(0, 100);

// ❌ RobustRandom.NextFloat() — тоже недетерминирован
```

## Shared код для предикции

### Почему Shared

Предсказанные системы и компоненты **обязаны** находиться в `Content.Shared` (или `Robust.Shared`). Это потому что:

1. Код предикции выполняется **и на клиенте, и на сервере**
2. Клиент и сервер должны получить **одинаковый результат** при одинаковом вводе
3. Если код только на сервере, клиент не сможет его предсказать

### Partial class decomposition

Когда нужна серверная или клиентская специализация:

```csharp
// Content.Shared/MySystem.cs
public abstract partial class SharedMySystem : EntitySystem
{
    // Общая предикшн-логика
    protected void HandleAction(EntityUid uid, MyComponent comp)
    {
        comp.Value += 1;
        Dirty(uid, comp);
    }
}

// Content.Server/MySystem.cs
public sealed partial class MySystem : SharedMySystem
{
    // Серверная валидация, логирование, авторитетные действия
}

// Content.Client/MySystem.cs
public sealed partial class MySystem : SharedMySystem
{
    // Клиентские эффекты, UI
}
```

## Обработка состояний при предикции

### AutoGenerateComponentState

Атрибут `[AutoGenerateComponentState]` генерирует код для:
- Сериализации компонента в `IComponentState` (для отправки по сети)
- Десериализации из `ComponentHandleState` (для применения серверного состояния)
- Автоматического отката при `ResetPredictedEntities`

### Как работает откат

1. Компонент помечается как «грязный» во время предикции
2. При `ResetPredictedEntities` движок находит последнее серверное состояние для этого компонента
3. Генерирует `ComponentHandleState` и рейзит его как событие
4. Автосгенерированный обработчик восстанавливает все `[AutoNetworkedField]` поля
5. `LastModifiedTick` сбрасывается к `LastRealTick`

### NetSync

Поля с `[AutoNetworkedField]` можно дополнительно настроить через `[NetSync]` для управления направлением синхронизации.

## Чеклист предикции

При добавлении предикции к системе:

1. **Компонент в Shared** — с `[NetworkedComponent]`, `[AutoGenerateComponentState]`, `[AutoNetworkedField]` на нужных полях
2. **Система в Shared** — базовый класс `SharedMySystem` с предикшн-логикой
3. **`Dirty()` после изменений** — каждое изменение компонента должно сопровождаться `Dirty(uid, comp)`
4. **Побочные эффекты за `IsFirstTimePredicted`** — звуки, попапы, визуальные эффекты
5. **Детерминированный рандом** — `RandomPredictedSystem` или seed-based `System.Random`
6. **`IRobustCloneable` для ссылочных типов** — коллекции, классы в сетевых полях
7. **Нет удаления серверных сущностей** — используйте компоненты-состояния вместо удаления
8. **Тестирование с задержкой** — `net.fakelagmin 0.2` + `net.fakelagrand 0.1`
9. **Тестирование с отключённой предикцией** — `net.predict false`

## Частые ошибки

### 1. Забыли `Dirty()`

```csharp
// ❌ Клиент не получит обновление
comp.Health -= damage;

// ✅ Правильно
comp.Health -= damage;
Dirty(uid, comp);
```

### 2. IRobustRandom в предикции

```csharp
// ❌ Разные результаты при каждом re-prediction
if (_random.Prob(0.5f))
    DoAction();

// ✅ Детерминированный рандом
if (_randomPredicted.ProbForEntity(uid, 0.5f))
    DoAction();
```

### 3. Побочные эффекты без IsFirstTimePredicted

```csharp
// ❌ Попап покажется множество раз
_popup.PopupEntity("Удар!", uid);

// ✅ Только один раз
if (_timing.IsFirstTimePredicted)
    _popup.PopupEntity("Удар!", uid);
```

### 4. [NetworkedComponent] на не-Shared компоненте

```csharp
// ❌ В Content.Client — молча не работает
[RegisterComponent, NetworkedComponent]
public sealed partial class MyClientComponent : Component { }

// ✅ В Content.Shared — работает корректно
[RegisterComponent, NetworkedComponent]
public sealed partial class MyComponent : Component { }
```

### 5. Удаление серверной сущности в предикции

```csharp
// ❌ Вызовет ошибку при реконсиляции
if (_timing.InPrediction)
    QueueDel(targetUid);

// ✅ Используйте компонент-состояние
RemComp<AliveComponent>(targetUid);
```

### 6. Изменение не-сетевых данных в предикции

```csharp
// ❌ Поле без [AutoNetworkedField] не откатится
comp.LocalCounter += 1; // Не сетевое, не откатится!

// ✅ Все предсказанные поля должны быть сетевыми
[AutoNetworkedField]
public int Counter;
```

## Тестирование предикции

### Основные CVars

```
// В консоли клиента
net.predict false          // Отключить предикцию — видна реальная задержка
net.predict true           // Включить обратно

// Симуляция задержки
net.fakelagmin 0.2         // 200мс минимальная задержка
net.fakelagrand 0.05       // +0-50мс случайной задержки

// Симуляция потерь пакетов
net.fakeloss 0.05          // 5% потери пакетов
```

### Что проверять

1. С **выключенной предикцией** (`net.predict false`) — правильно ли серверное поведение?
2. С **задержкой** — нет ли визуальных скачков (snap-back) при миспредикте?
3. С **потерей пакетов** — не ломается ли синхронизация?
4. Несколько **быстрых действий подряд** — корректно ли обрабатывается очередь предикции?

## Связь с другими скиллами

- **SS14 Netcode Architecture** — как работает сетевой стек, обеспечивающий предикцию
- **SS14 ECS Components** — атрибуты `[AutoGenerateComponentState]`, `[AutoNetworkedField]`, `Dirty()`
- **SS14 ECS Systems** — partial class decomposition для Shared/Client/Server систем
- **SS14 ECS Entities** — `EntityUid` vs `NetEntity`, жизненный цикл сущностей

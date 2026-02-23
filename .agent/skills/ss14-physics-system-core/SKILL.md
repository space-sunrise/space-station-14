---
name: SS14 Physics System Core
description: Глубокий практический гайд по PhysicsSystem в Space Station 14: устройство симуляции (broadphase, contacts, islands, solver), связь с Transform/Container/Anchoring, клиентская prediction-часть и рабочие паттерны применения. Используй, когда нужно понять как физика реально работает и где безопасно встраивать игровую логику.
---

# PhysicsSystem: Core

Этот skill покрывает архитектуру и ментальную модель PhysicsSystem 🙂
Полный каталог публичного API смотри в отдельном skill `SS14 Physics System API`.

## Ментальная модель

Physics в SS14 удобно мыслить как конвейер из 6 слоёв:

1. `PhysicsComponent`
- Состояние тела: `BodyType`, `CanCollide`, скорости, демпфинг, масса, сон и т.д.

2. `FixturesComponent`
- Набор физ. фигур тела (хитбоксы/сенсоры), их `layer/mask/hard`, material-параметры.

3. Broadphase
- Деревья прокси по картам/гридам, быстрый поиск потенциальных пар.

4. Contacts
- Узлы пар fixture-vs-fixture, lifecycle `Start/Touching/End`.

5. Islands + Solver
- Графы боди/контактов/джоинтов, решаемые пакетно и частично параллельно.

6. Синхронизация с Transform
- После solve позиция/ротация кладутся обратно в transform-дерево.

Ключевая идея:
- Игровая логика почти всегда должна менять физику через системные методы (`Set*`, `Apply*`, `RegenerateContacts`, `WakeBody`), а не прямыми полями компонента ✅

## Как реально идёт тик физики

Типичный шаг (с учётом substeps):

1. `PhysicsUpdateBeforeSolveEvent`.
2. Поиск новых потенциальных пар (`FindNewContacts`).
3. Обновление контактов (`CollideContacts`) и событий `Start/EndCollide`.
4. Solve islands (`Step` -> интеграция скоростей -> constraints -> позиции).
5. `PhysicsUpdateAfterSolveEvent`.
6. Финализация/lerp-данных на последнем substep.

Практический вывод:
- Всё, что должно подействовать "в этом же физическом шаге", нужно ставить до solve (обычно в системах-контроллерах или событиях before-solve).

## Связь с Transform, Anchoring и Containers

PhysicsSystem плотно связан с transform-иерархией:

- При parent-change пересчитывается поведение тела и сохраняется map-скорость.
- Anchor/unanchor меняет не только transform-статус, но и физический режим (через системный flow).
- В контейнере тело обычно переводится в `CanCollide = false`, скорости обнуляются.

Следствие:
- Любые операции "вставил в контейнер", "перекинул parent", "заанкорил/разанкорил" нельзя считать только визуальными; это полноценный физический transition ⚠️

## Клиент и сервер

Общее:
- И сервер, и клиент используют общий физический слой и одинаковые ключевые инварианты.

Различия:
- Сервер авторитетен.
- Клиент дополнительно управляет prediction-флагом сущностей и аккуратно пересчитывает touching-состояние контактов при ресете состояния, чтобы уменьшить mispredict-эффекты.

## Короткое дерево решений

1. Нужен разовый толчок/рывок?
- `ApplyLinearImpulse` / `ApplyAngularImpulse`.

2. Нужна постоянная тяга/момент?
- `ApplyForce` / `ApplyTorque`.

3. Нужно переключить физический режим объекта?
- `SetBodyType` (+ при необходимости `SetCanCollide`, `SetFixedRotation`, `SetSleepingAllowed`).

4. Нужно поменять коллизионное поведение без смены типа тела?
- `SetCanCollide` или fixture-API (`SetCollisionMask/Layer`, `SetHard`).

5. Нужна пересборка актуальных контактов после резкой смены коллизии/фикстур?
- `RegenerateContacts`.

6. Нужна геометрия и дистанции по реальным хитбоксам?
- `TryGetNearest*`, `GetWorldAABB`, `GetHardAABB`, ray/intersection query API.

## Оптимизация при использовании PhysicsSystem 🙂

### 1) Компонентные оптимизации (самые дешевые и эффективные)

- `CollisionWake`: автоматически выключает `CanCollide` для спящих тел на гриде (если нет джоинтов/критических контактов), и включает обратно при пробуждении.
- `CollideOnAnchor`: привязывает `CanCollide` к состоянию anchor/unanchor.
- Эти два подхода обычно лучше ручного "дергания" `SetCanCollide` в 100 местах.

```yaml
# Подпольная/трубная сущность: коллизия автоматически синхронизируется с anchoring.
components:
- type: Physics
  canCollide: false
- type: CollideOnAnchor
```

```yaml
# Массовые мелкие предметы: collision wake экономит апдейты на гриде.
components:
- type: Physics
- type: CollisionWake
```

### 2) Сон/пробуждение и частые апдейты скорости

- Для объектов с частыми velocity-апдейтами используй `SetLinearVelocity(..., wakeBody: false)`, если не нужно принудительное пробуждение каждый тик.
- Включай `SetSleepingAllowed(true)` там, где тело может стабильно "уснуть".
- Зови `WakeBody` только перед реальным действием (толчок, резкий маневр, включение механизма).

```csharp
// Контроллер обновляет velocity каждый тик, но лишний wake на каждом шаге не нужен.
PhysicsSystem.SetAngularVelocity(uid, angularVelocity);
PhysicsSystem.SetLinearVelocity(uid, velocity, wakeBody: false);
```

### 3) Дешевые query/контакт-паттерны

- Для контактной логики используй `GetContacts`/`ContactEnumerator` (минимум аллокаций).
- Для проверок пересечений, где не нужна идеальная точность, используй `approximate = true`.
- Ray/query фильтруй через маску заранее, а не пост-фильтрацией огромного результата.

```csharp
// Быстрый broadphase-скрининг: где подходит, оставляй approximate = true.
var touching = _physics.GetEntitiesIntersectingBody(uid, collisionMask, approximate: true);
```

### 4) Изменения фикстур и коллизии делай пачкой

- Плохо: менять `layer/mask/hard` в цикле и после каждого изменения пересобирать контакты.
- Хорошо: применить пачку изменений и сделать один re-sync.

```csharp
// После серии правок collision-профиля делаем один пересчет контактов.
_physics.SetCollisionMask(uid, id, fixture, newMask, manager: fixtures, body: body);
_physics.SetCollisionLayer(uid, id, fixture, newLayer, manager: fixtures, body: body);
_broadphase.RegenerateContacts((uid, body, fixtures, xform));
```

### 5) Используй `hard: false` для триггерных механик

- Лужи, ловушки, "ауры", сенсоры: оставляй contact-события, но не добавляй solver-нагрузку hard-столкновений.
- Смысл: логика срабатывает, но физический "упор" не считается.

```yaml
# Триггерная фикстура: contact есть, hard-столкновения нет.
fixtures:
  sensor:
    hard: false
    mask: [ItemMask]
    layer: [SlipLayer]
```

### Паттерны оптимизации

- Включать `CollisionWake` для массовых динамических предметов.
- Использовать `CollideOnAnchor` для подполов/труб/якорных систем.
- На частом апдейте скоростей избегать лишнего `WakeBody`.
- Держать маски узкими (без лишних битов), чтобы резать число потенциальных пар в broadphase.
- Использовать `hard: false` для чисто триггерных collision-слоев.

### Анти-паттерны оптимизации

- Глобально отключать sleep или массово держать тела принудительно awake без причины.
- Давать слишком широкие `mask` (или `AllMask`) обычным сущностям.
- Постоянно менять collision-профиль тела в апдейте контроллера.
- Использовать только точные/дорогие query там, где хватает approximate-проверки.
- Делать вручную то, что уже покрывается `CollisionWake`/`CollideOnAnchor`.

## Примеры из кода

### Пример 1: физический шаг (до solve -> contacts -> solve -> после solve)

```csharp
var updateBeforeSolve = new PhysicsUpdateBeforeSolveEvent(prediction, frameTime);
RaiseLocalEvent(ref updateBeforeSolve);

_broadphase.FindNewContacts();
CollideContacts();
Step(frameTime, prediction);

var updateAfterSolve = new PhysicsUpdateAfterSolveEvent(prediction, frameTime);
RaiseLocalEvent(ref updateAfterSolve);
```

### Пример 2: корректный перевод сущности в контейнерный режим

```csharp
// При вставке в контейнер гасим динамику и выключаем столкновения.
_physics.SetLinearVelocity(entity, Vector2.Zero, false, body: physics);
_physics.SetAngularVelocity(entity, 0, false, body: physics);
_physics.SetCanCollide(entity, false, false, body: physics);
```

### Пример 3: conveyor-контроллер через контактный итератор

```csharp
var contacts = PhysicsSystem.GetContacts(conveyorUid);
while (contacts.MoveNext(out var contact))
{
    var other = contact.OtherEnt(conveyorUid);
    if (_conveyedQuery.HasComp(other))
        PhysicsSystem.WakeBody(other);
}
```

### Пример 4: предсказуемая кинематика без лишнего "разбуживания"

```csharp
// Для conveyor-движения не всегда нужно будить тело на каждом апдейте.
PhysicsSystem.SetAngularVelocity(uid, angularVelocity);
PhysicsSystem.SetLinearVelocity(uid, velocity, wakeBody: false);
```

### Пример 5: re-sync контактов после изменения физического режима

```csharp
_physics.SetBodyType(uid, BodyType.Dynamic, fixtures, body, xform);
_physics.SetCanCollide(uid, true, manager: fixtures, body: body);
_broadphase.RegenerateContacts((uid, body, fixtures, xform));
```

### Пример 6: range-check по реальным хитбоксам

```csharp
var xfA = new Transform(worldPosA, worldRotA);
var xfB = new Transform(targetPos.Position, targetRot);

if (_physics.TryGetNearest(origin, other, out _, out _, out var distance, xfA, xfB, fixtureA, fixtureB))
{
    if (distance <= range)
        return true;
}
```

### Пример 7: runtime переключение "летает/не летает" через статус+сон

```csharp
_physics.SetBodyStatus(target, targetPhysics, BodyStatus.InAir, false);
_physics.SetSleepingAllowed(target, targetPhysics, false);
_physics.WakeBody(target, body: targetPhysics);
```

## Паттерны 🙂

- Думай связкой `BodyType + CanCollide + Fixtures`, а не одним флагом.
- После существенной смены fixture/collision-параметров выполняй `RegenerateContacts`.
- Для сложной геометрии используй `TryGetNearest*`, а не дистанцию между центрами.
- Для контактной логики используй `GetContacts`/`GetContactingEntities`, а не ручной обход чужих структур.
- Учитывай parent/container/anchor как физические операции, а не только transform-операции.
- На клиенте управляем prediction через `UpdateIsPredicted` flow, не через ручное дергание полей.

## Анти-паттерны

- Прямо мутировать поля `PhysicsComponent`/`Fixture` в игровом коде.
- Ожидать, что `SetBodyStatus` сам изменит solver-поведение как `SetBodyType`.
- Менять `layer/mask/hard` и забывать про пересинхронизацию контактов.
- Применять "центр-центр" distance для объектов со сложными фикстурами.
- Держать объект в контейнере с включённой коллизией и ненулевой скоростью.

## Мини-чеклист перед изменениями

- Изменение действительно должно идти через `SharedPhysicsSystem` API?
- Нужен ли `WakeBody` после изменения?
- Нужно ли после правки fixture/collision сделать `RegenerateContacts`?
- Не перепутаны ли `BodyType` и `BodyStatus`?
- Не ломается ли поведение в контейнерах/при reparent/при anchor-state смене?
- Для клиентского кейса учтён prediction-flow?

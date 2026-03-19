---
name: ss14-virtual-controller-core
description: Разбирает архитектуру VirtualController в Space Station 14: цикл UpdateBeforeSolve/UpdateAfterSolve, порядок через UpdatesBefore/UpdatesAfter, prediction-семантику и связь с low-level физикой Box2D (substeps, contacts, solver). Используй, когда нужно глубоко понять систему перед расширением или отладкой.
---

# VirtualController: Архитектура и Цикл

Используй этот skill как архитектурный playbook по VirtualController 🙂
Держи фокус на свежем коде и проверяй актуальность через `git log`/`blame` (cutoff: `2024-02-20`).

## Что загружать в первую очередь

1. `references/fresh-pattern-catalog.md` — паттерны с явным статусом свежести (`Использовать` / `Ограниченно`) ✅
2. `references/rejected-snippets.md` — зоны, которые нельзя брать как эталон ⚠️
3. `references/docs-context.md` — как безопасно использовать docs

## Источник правды

1. Кодовая база — основной источник истины.
2. Документация — вторичный слой (термины, intent, диагностика).
3. Любой участок старше двух лет или с TODO/проблемным комментарием по теме — не поднимать в эталонные правила.

## Ментальная модель VirtualController

1. `VirtualController` — это `EntitySystem`, который подписывается на `PhysicsUpdateBeforeSolveEvent` и `PhysicsUpdateAfterSolveEvent`.
2. Порядок вызова контроллера задается через `UpdatesBefore` / `UpdatesAfter`.
3. Хуки `UpdateBeforeSolve` и `UpdateAfterSolve` вызываются на каждом physics substep.
4. Параметр `prediction` сообщает, выполняется ли предикт-симуляция на клиенте.
5. `frameTime` — время одного substep, а не всего тика.
6. Производительность контроллеров мониторится отдельно через встроенные histograms (`BeforeMonitor` / `AfterMonitor`).

## Схема слоев

1. Engine-слой:
`VirtualController`, `SharedPhysicsSystem`, события before/after solve, solver/contact pipeline, special-casing `KinematicController`.
2. Shared-слой:
`SharedMoverController`, `TileFrictionController`, `SharedConveyorController`, `ClimbSystem`.
3. Server-слой:
`PullController`, `RandomWalkController`, `ChasingWalkSystem`, `ChaoticJumpSystem`, `MoverController`, `ConveyorController`.
4. Client-слой:
`MoverController`, `ConveyorController`-stub для prediction/network presence, prediction hooks через `UpdateIsPredictedEvent`.

## Паттерны

1. Добавляй `UpdatesBefore` / `UpdatesAfter` до `base.Initialize()` — иначе хук-порядок зафиксируется неверно.
2. В `UpdateBeforeSolve` делай ранние `continue` по `prediction`/`body.Predict`, чтобы не ломать предикт.
3. Используй `AwakeBodies` как основной набор для per-step физической логики.
4. Для `KinematicController` применяй ручной damping через helper-методы движения и затем `SetLinearVelocity`/`SetAngularVelocity`.
5. Для тяжелых вычислений внутри контроллера отделяй фазу расчета (parallel job) от фазы применения результатов (main thread).
6. Для relay-управления используй API-методы `SetRelay`/`RemoveRelay`, а не ручное добавление/удаление компонентов.
7. Для container-eject сценариев, где сущность должна сразу оказаться «снаружи», используй `ForciblySetClimbing` как безопасный post-eject переход.
8. Держи `UpdateBeforeSolve` детерминированным: минимум скрытого состояния, минимум nondeterministic источников.
9. Для контроля регрессий проверяй physics-мониторы контроллеров до/после изменения.
10. Для систем движения сохраняй инвариант: mob movement и tile friction должны идти в согласованном порядке.

## Анти-паттерны

1. Настраивать `UpdatesBefore/UpdatesAfter` после `base.Initialize()`.
2. Копировать TODO-heavy участки как эталон архитектуры.
3. Писать контроллер, который зависит от «одного вызова за тик» и игнорирует substeps.
4. Выполнять дорогие lookup/alloc операции без cache-query в горячем physics loop.
5. Обходить relay API прямыми `RemComp/EnsureComp` в gameplay-коде.
6. Считать пустой клиентский `ConveyorController` «лишним» и удалять его из схемы предикта.
7. Пытаться использовать старые pre-cutoff фрагменты как базу для новых решений.
8. Лечить mispredict «магическими guard'ами» вместо исправления источника рассинхрона.
9. Мешать transform-движение и физические импульсы без четкой причинно-следственной модели.
10. Игнорировать warning-комментарии о хрупкой логике (`slop`, `hack`, `temporary`).

## Low-level Box2D/Physics Internals

1. Симуляция идет через `SimulateWorld`: каждый substep поднимает before/after события, затем broadphase/contact/solve.
2. Контроллеры выполняются до solver-а (`UpdateBeforeSolve`) и после solver-а (`UpdateAfterSolve`) на каждом substep.
3. `AwakeBodies` — ключевой runtime-набор тел, участвующих в активной симуляции и контроллерах.
4. Генерация и пересчет контактов построены на broadphase overlap + narrowphase manifold update.
5. Для `KinematicController` есть отдельные ветки в solver/contact logic, поэтому его нельзя трактовать как обычный `Dynamic`.
6. Любая логика, меняющая скорости/импульсы в контроллерах, должна учитывать prediction и режим тела.

## Примеры из кода

### 1) Подписка VirtualController на before/after solve

```csharp
public override void Initialize()
{
    base.Initialize();

    var updatesBefore = UpdatesBefore.ToArray();
    var updatesAfter = UpdatesAfter.ToArray();

    // Контроллер получает оба physics-хука в одном order-графе.
    SubscribeLocalEvent<PhysicsUpdateBeforeSolveEvent>(OnBeforeSolve, updatesBefore, updatesAfter);
    SubscribeLocalEvent<PhysicsUpdateAfterSolveEvent>(OnAfterSolve, updatesBefore, updatesAfter);
}
```

### 2) Physics substeps с вызовом контроллеров

```csharp
for (int i = 0; i < _substeps; i++)
{
    var before = new PhysicsUpdateBeforeSolveEvent(prediction, frameTime);
    RaiseLocalEvent(ref before);

    _broadphase.FindNewContacts();
    CollideContacts();
    Step(frameTime, prediction);

    var after = new PhysicsUpdateAfterSolveEvent(prediction, frameTime);
    RaiseLocalEvent(ref after);
}
```

### 3) Явный order между mover/friction/conveyor

```csharp
public override void Initialize()
{
    UpdatesBefore.Add(typeof(TileFrictionController));
    base.Initialize();
}

public override void Initialize()
{
    UpdatesAfter.Add(typeof(SharedMoverController));
    base.Initialize();
}
```

### 4) Ручной damping для KinematicController

```csharp
if (body.BodyType == BodyType.KinematicController)
{
    var velocity = body.LinearVelocity;
    var angVelocity = body.AngularVelocity;

    // Для этого типа тела friction/solver-путь другой, поэтому демпфируем вручную.
    _mover.Friction(0f, frameTime, friction, ref velocity);
    _mover.Friction(0f, frameTime, friction, ref angVelocity);

    PhysicsSystem.SetLinearVelocity(uid, velocity, body: body);
    PhysicsSystem.SetAngularVelocity(uid, angVelocity, body: body);
}
```

### 5) Pull-контроллер с обратным импульсом в особом режиме

```csharp
var impulse = accel * physics.Mass * frameTime;
PhysicsSystem.ApplyLinearImpulse(pullableEnt, impulse, body: physics);

// В weightless/blocked сценарии добавляем парный импульс, чтобы не терять физический баланс.
if (_gravity.IsWeightless(puller) && pullerXform.Comp.GridUid == null || !_actionBlockerSystem.CanMove(puller))
{
    PhysicsSystem.WakeBody(puller);
    PhysicsSystem.ApplyLinearImpulse(puller, -impulse);
}
```

### 6) Chasing-контроллер принудительно держит тело «в воздухе»

```csharp
var delta = targetPos - selfPos;
var speed = delta.Length() > 0 ? delta.Normalized() * component.Speed : Vector2.Zero;

_physics.SetLinearVelocity(uid, speed);
// Специальный режим для поведения сущности (например, tesla-подобные объекты).
_physics.SetBodyStatus(uid, physics, BodyStatus.InAir);
```

### 7) Chaotic jump с raycast-safe смещением

```csharp
var ray = new CollisionRay(startPos, direction.ToVec(), component.CollisionMask);
var hit = _physics.IntersectRay(mapId, ray, range, uid, returnOnFirstHit: false).FirstOrNull();

if (hit != null)
{
    // Сдвиг от точки столкновения, чтобы не телепортироваться прямо внутрь коллизии.
    targetPos = hit.Value.HitPos - new Vector2((float)Math.Cos(direction), (float)Math.Sin(direction));
}

_transform.SetWorldPosition(uid, targetPos);
```

## Правило расширения

1. Новые контроллеры проектируй как детерминированные substep-проходы с явным prediction-gating.
2. Порядок контроллеров фиксируй в `Initialize()` до `base.Initialize()`.
3. Любой заимствованный фрагмент сначала сверяй с `rejected-snippets`.
4. Любое performance-изменение подтверждай профилированием controller histograms.

Думай о VirtualController как о physics orchestration-слое, а не как о «еще одном Update()» 😅

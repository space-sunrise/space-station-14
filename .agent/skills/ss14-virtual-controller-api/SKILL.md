---
name: ss14-virtual-controller-api
description: Дает практический каталог API вокруг VirtualController в Space Station 14: выбор хуков и order, physics mutators, relay/movement/climb сценарии, prediction-ограничения и безопасные паттерны применения.
---

# VirtualController: API-Практика

Используй этот skill, когда нужно быстро выбрать корректный метод и не получить physics/prediction-регресс 😎
Ориентир свежести: `git log`/`blame` с cutoff `2024-02-20`.

## Что загружать

1. `references/fresh-pattern-catalog.md` — реестр методов с маркировкой свежести (`Использовать` / `Ограниченно`).
2. `references/rejected-snippets.md` — рискованные места и misuse-практики.
3. `references/docs-context.md` — как читать docs без конфликта с кодом.

## Быстрый выбор API

1. Нужен контроллер на шаг физики: унаследуйся от `VirtualController` и реализуй `UpdateBeforeSolve`/`UpdateAfterSolve`.
2. Нужен порядок между контроллерами: добавляй `UpdatesBefore`/`UpdatesAfter` до `base.Initialize()`.
3. Нужен control over velocity/impulse: используй `SetLinearVelocity`, `ApplyLinearImpulse`, `SetBodyStatus`, damping setters.
4. Нужен relay управления сущностью: используй `SetRelay`/`RemoveRelay`.
5. Нужна ручная mobility-математика: `Friction`, `Accelerate`, `GetWishDir`/`SetWishDir`.
6. Нужна связка контейнер/перемещение через «вылезание»: `ForciblySetClimbing`.
7. Нужен безопасный climb pipeline: `CanVault` -> `TryClimb`.
8. Нужен predicted-контроль на клиенте: обработчики `UpdateIsPredictedEvent`.

## API по группам

### 1) Контракт VirtualController

1. `Initialize()` — регистрирует before/after solve hooks и метрики.
2. `UpdateBeforeSolve(bool prediction, float frameTime)` — pre-solver логика.
3. `UpdateAfterSolve(bool prediction, float frameTime)` — post-solver логика.
4. `BeforeMonitor` / `AfterMonitor` — измерение стоимости конкретного контроллера.

### 2) Ordering API

1. `UpdatesBefore.Add(typeof(...))` — этот контроллер идет раньше цели.
2. `UpdatesAfter.Add(typeof(...))` — этот контроллер идет позже цели.
3. Оба списка должны быть настроены до `base.Initialize()`.

### 3) Physics mutators

1. `SetLinearVelocity(...)` — безопасная установка линейной скорости.
2. `ApplyLinearImpulse(...)` — импульс для динамики/подтягивания.
3. `SetBodyStatus(..., BodyStatus.InAir/OnGround)` — режим тела для спец-механик.
4. `SetLinearDamping(...)` / `SetAngularDamping(...)` — runtime демпфирование.
5. `SetBodyType(...)` — осознанная смена режима тела.

### 4) Relay API

1. `SetRelay(uid, relayEntity)` — создать/обновить relay-связку.
2. `RemoveRelay(uid)` — снять relay и привести prediction-состояние в консистентное.
3. Клиентские prediction hooks (`UpdateIsPredictedEvent`) должны учитывать relay-цепочку.

### 5) Movement helper API

1. `GetWishDir(...)` / `SetWishDir(...)` — управление желаемым направлением.
2. `Friction(...)` (vector/scalar) — контролируемое затухание скорости.
3. `Accelerate(...)` — ограниченное ускорение к wish-направлению.
4. `ResetCamera(...)` / `GetParentGridAngle(...)` — корректная работа угла/ориентации.

### 6) Climb API

1. `CanVault(...)` — предвалидация climb/vault.
2. `TryClimb(...)` — запуск процедуры climb с do-after и проверками.
3. `ForciblySetClimbing(...)` — принудительный безопасный выход на поверхность.

### 7) Conveyor и Pull практические точки

1. Конвейер: `CanRun(...)`, `UpdateBeforeSolve(...)`, комбинация conveyor-вектора с `wishDir`.
2. Pull: импульс к цели + settle shutdown + обратный импульс в особых условиях.

## Необычные кейсы применения

1. Relay-пилотирование: мехи, station-eye, пилотируемая одежда, proxy-control объекты в спец-режимах.
2. Container-eject пайплайн: после извлечения сущности применять `ForciblySetClimbing`, чтобы избежать залипания.
3. Клиентский prediction gating: для relay target и pullable через `UpdateIsPredictedEvent`.

## Паттерны

1. Настраивай порядок контроллера (`UpdatesBefore/After`) до `base.Initialize()`.
2. В `UpdateBeforeSolve` фильтруй неподходящие тела (`prediction`, `body.Predict`, статические/спящие кейсы).
3. Для `KinematicController` используй helper-фрикцию + явную установку скоростей.
4. Для relay-сценариев используй только `SetRelay`/`RemoveRelay`.
5. Для gravity/space pull-сценариев учитывай парный импульс, если это требуется механикой.
6. Для climb-flow всегда делай `CanVault` перед `TryClimb`.
7. Для ejection из контейнера/крио/сканера применяй `ForciblySetClimbing` как post-action шаг.
8. Для conveyor-логики считай направление отдельно, применяй результат после parallel-части.
9. Для client prediction корректируй `IsPredicted/BlockPrediction` через соответствующие события.
10. Используй `SetBodyStatus` только в явно обоснованных special-механиках.
11. Для API, вызываемого часто, кэшируй queries и избегай лишних аллокаций.
12. Проверяй реальную свежесть примера перед повторным использованием.

## Анти-паттерны

1. Добавлять order-зависимости после `base.Initialize()`.
2. Дергать `SetBodyStatus(InAir)` как универсальную «заплатку».
3. Менять relay вручную через прямые `RemComp/EnsureComp` без lifecycle-методов.
4. В predicted-контроллере использовать nondeterministic логику без защит.
5. Копировать TODO-heavy куски из pull/contact/solver как «best practice».
6. Опираться на пустой клиентский conveyor-класс как на источник поведения.
7. Использовать pre-cutoff legacy фрагменты в роли эталона.
8. Миксовать transform-сдвиги и импульсы без явной модели порядка.
9. Пропускать проверки `CanVault` и сразу форсировать climb-flow.
10. Игнорировать update-события prediction при relay/pullable сценариях.
11. Пытаться «лечить» mispredict только визуальными guard'ами.
12. Не проверять последствия API-изменения на server/shared/client одновременно.

## Примеры из кода

### 1) Базовый шаблон контроллера

```csharp
public sealed class ExampleController : VirtualController
{
    public override void Initialize()
    {
        UpdatesAfter.Add(typeof(SomeOtherController));
        base.Initialize();
    }

    public override void UpdateBeforeSolve(bool prediction, float frameTime)
    {
        // Здесь только детерминированная pre-solver логика.
    }
}
```

### 2) Явный ordering API

```csharp
public override void Initialize()
{
    UpdatesBefore.Add(typeof(TileFrictionController));
    UpdatesAfter.Add(typeof(SharedMoverController));
    // Важно: порядок задаем до base.Initialize().
    base.Initialize();
}
```

### 3) Relay lifecycle через API, а не вручную

```csharp
// Подключаем управление пилота к прокси-сущности.
_mover.SetRelay(pilotUid, proxyUid);

// Снимаем relay корректно, с обновлением prediction-состояния.
_mover.RemoveRelay(pilotUid);
```

### 4) Movement math helper: friction + accelerate

```csharp
var velocity = body.LinearVelocity;

// Сначала затухание, потом разгон к target-направлению.
_mover.Friction(minimumFrictionSpeed, frameTime, friction, ref velocity);
SharedMoverController.Accelerate(ref velocity, targetDir, accel, frameTime);

PhysicsSystem.SetLinearVelocity(uid, velocity, body: body);
```

### 5) Physics mutators в pull-механике

```csharp
var impulse = accel * physics.Mass * frameTime;
PhysicsSystem.WakeBody(targetUid, body: physics);
PhysicsSystem.ApplyLinearImpulse(targetUid, impulse, body: physics);
```

### 6) Безопасный climb pipeline

```csharp
if (_climb.CanVault(climbable, user, target, out _) &&
    _climb.TryClimb(user, target, climbableUid, out var doAfter, climbable))
{
    // Climb стартовал штатно, doAfter хранится для продолжения.
    component.DoAfterId = doAfter;
}
```

### 7) Container eject -> ForciblySetClimbing

```csharp
_container.Remove(containedUid, bodyContainer);

// После извлечения переводим сущность в корректное состояние "снаружи".
_climb.ForciblySetClimbing(containedUid, containerOwnerUid);
```

### 8) Prediction hook для pullable на клиенте

```csharp
private void OnUpdatePullablePredicted(Entity<PullableComponent> ent, ref UpdateIsPredictedEvent args)
{
    if (ent.Comp.Puller == _playerManager.LocalEntity)
        args.IsPredicted = true;
    else if (ent.Comp.Puller != null)
        args.BlockPrediction = true;
}
```

### 9) Conveyor + wish direction

```csharp
var targetDir = conveyorDir;
var wishDir = _mover.GetWishDir(entityUid);

if (Vector2.Dot(wishDir, targetDir) > 0f)
    targetDir += wishDir;

SharedMoverController.Accelerate(ref velocity, targetDir, 20f, frameTime);
```

## Правило применения

1. Сначала выбирай методы из `fresh-pattern-catalog.md`.
2. Любой legacy/TODO-фрагмент сначала сверяй с `rejected-snippets.md`.
3. Если сценарий затрагивает prediction, проверяй server/shared/client поведение одновременно.
4. При расхождении docs и кода всегда закрепляй решение в пользу кода.

Держи API-слой предсказуемым: правильный метод и правильный lifecycle важнее «короткого» вызова 🙂

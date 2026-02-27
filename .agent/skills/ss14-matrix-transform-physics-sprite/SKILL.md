---
name: ss14-matrix-transform-physics-sprite
description: Узкоспециализированный навык по матричным преобразованиям в SS14: world/grid/local/screen конверсии, рендер-матрицы Sprite, физические запросы в broadphase-space, клиентские и серверные цепочки Transform/Physics/Sprite.
---

# Матрицы для Transform/Physics/Sprite в SS14

Используй этот skill, когда задача упирается в матрицы и конверсию координатных пространств 🙂
Фокус только на матричной части `Transform/Physics/Sprite` без дублирования общих тем из других skill.

## Когда применять

Применяй skill, если нужно:

- переводить данные между `world/grid/local/screen`;
- корректно строить/инвертировать матрицы для физики и рендера;
- конвертировать bounds (`TransformBox`) для broadphase, AO/FOV, docking, UI-навигации;
- разруливать клиент/сервер различия в матричных цепочках.

## Ментальная модель матриц

1. Разделяй пространства строго: `world` -> `entity/grid local` -> `sprite/layer local` -> `screen`.
2. Используй парные операции: `CreateTransform` для прямого хода, `CreateInverseTransform` для обратного.
3. Для физики сначала выбирай корректный `reference frame` (обычно broadphase/grid), потом конвертируй позицию и угол.
4. Для рендера учитывай модификаторы спрайта (`NoRotation`, `SnapCardinals`, стратегия слоя), а уже потом вычисляй draw-matrix.
5. Для направлений обнуляй трансляцию матрицы (`M31/M32 = 0`), иначе ты искажаешь вектор ⚠️
6. Работай через системные методы (`SharedTransformSystem`, `SharedPhysicsSystem`, `SpriteSystem`), а не обходные ручные вычисления.

## Паттерны

1. Используй `Matrix3Helpers.CreateTransform(...)` и `CreateInverseTransform(...)` как каноническую пару прямого/обратного перехода.
2. В физике получай относительный трансформ через `SharedPhysicsSystem.GetRelativePhysicsTransform(...)` вместо ручного вычитания parent-цепочки.
3. Пересобирай `SpriteComponent.LocalMatrix` при изменении `Scale/Rotation/Offset` через `SpriteSystem.SetScale/SetRotation/SetOffset`.
4. В рендере спрайтов готовь отдельные матрицы под стратегии слоя (`Default`, `NoRotation`, `SnapToCardinals`, `UseSpriteStrategy`).
5. В кликах делай цепочку `world -> entity local -> sprite local -> layer local` через инверсии матриц.
6. В docking-композиции применяй формулу `inverse(stationDock) * gridDock`, затем `TransformBox` для AABB проверки.
7. В UI-навигации радаров и карт делай явную цепочку `grid/world -> shuttle -> view`.
8. В AO/FOV перед query по деревьям/тайлам переводи world-bounds в локальное пространство дерева/сетки через `GetInvWorldMatrix(...).TransformBox(...)`.
9. В FTL/проксимити расширяй и объединяй world-AABB через `GetWorldMatrix(...).TransformBox(...)`, а не смешивай world/local боксы напрямую.
10. Для векторов направления (лучи, рикошет) применяй матрицу без трансляции (`M31/M32 = 0`) и нормализуй после преобразования.
11. Для смешанного клиент/сервер кода держи единый геометрический смысл: одинаковая математика, но разные точки применения.
12. В сложных циклах сначала вычисляй базовые матрицы один раз, потом только переиспользуй их в итерациях ✅

## Анти-паттерны

1. Смешивать `world`, `grid local`, `entity local`, `screen` в одном выражении без явного перехода.
2. Преобразовывать направления полной матрицей с трансляцией (получишь неверную геометрию).
3. Игнорировать `NoRotation` и `SnapCardinals` при расчете экранной матрицы спрайта.
4. Обходить системные API и вручную собирать parent-цепочку матриц там, где есть готовые методы.
5. Использовать устаревшие компонентные обертки как основной API вместо системных методов.
6. Инвертировать матрицы в tight-loop без кэширования, если входные параметры в итерации не меняются.
7. Выполнять broadphase/query в world-space, когда ожидается локальное пространство дерева/сетки.
8. Клеить слойные `Sprite`-матрицы и физические трансформы без понимания их разных систем отсчета.
9. Тянуть в этот skill общие темы UI/архитектуры без матричного содержания 🙃

## Клиент/Сервер

- `Client`: клики, рендер спрайтов, AO/FOV, навигационные UI-виджеты; здесь чаще всего нужны цепочки `world -> local -> screen`.
- `Server`: docking/FTL/физические проверки; здесь критичны корректные относительные трансформы и `TransformBox` для collision/query.
- `Shared`: базовые матричные хелперы и унификация геометрии (`CreateTransform`, `CreateInverseTransform`, relative-physics transform).
- Держи инвариант: математика совпадает между слоями, расходится только контекст применения.

## Примеры из кода

### 1) Движок: `Matrix3Helpers.CreateTransform` + `CreateInverseTransform`

```csharp
// Прямой переход: локальное -> world.
var worldMatrix = Matrix3Helpers.CreateTransform(position, angle, scale);

// Обратный переход: world -> локальное пространство той же сущности.
var invWorldMatrix = Matrix3Helpers.CreateInverseTransform(position, angle, scale);

// Пример: точку из мира переводим в local.
var localPoint = Vector2.Transform(worldPoint, invWorldMatrix);
```

### 2) Движок: `SharedPhysicsSystem.GetRelativePhysicsTransform(...)`

```csharp
// Получаем broadphase-ориентированный transform, а не считаем руками через parent-цепочку.
var (_, broadphaseRot, _, broadphaseInv) = _transform.GetWorldPositionRotationMatrixWithInv(relativeXform);

// Позиция и угол в локальном пространстве broadphase.
var localPos = Vector2.Transform(worldTransform.Position, broadphaseInv);
var localRot = worldTransform.Quaternion2D.Angle - broadphaseRot;
var localTransform = new Transform(localPos, localRot);
```

### 3) Движок: `SpriteSystem.SetScale/SetRotation/SetOffset`

```csharp
// После смены scale/rotation/offset всегда пересобирай LocalMatrix.
sprite.Comp.scale = newScale;
sprite.Comp.LocalMatrix = Matrix3Helpers.CreateTransform(
    in sprite.Comp.offset,
    in sprite.Comp.rotation,
    in sprite.Comp.scale);

// Так слойный рендер получает согласованную матрицу без "дрейфа".
```

### 4) Движок: `SpriteSystem.Render` и layer strategies

```csharp
// Базовая матрица с учетом no-rotation / snap-cardinals.
var entityMatrix = Matrix3Helpers.CreateTransform(
    worldPosition,
    sprite.Comp.NoRotation ? -eyeRotation : worldRotation - cardinal);

// Для granular rendering заранее считаем отдельные матрицы и выбираем по стратегии слоя.
var transformDefault = Matrix3x2.Multiply(sprite.Comp.LocalMatrix,
    Matrix3Helpers.CreateTransform(worldPosition, worldRotation));
var transformNoRot = Matrix3x2.Multiply(sprite.Comp.LocalMatrix,
    Matrix3Helpers.CreateTransform(worldPosition, -eyeRotation));
```

### 5) Upstream (клиент): `ClickableSystem.CheckClick`

```csharp
// 1) Инвертируем спрайтовую локальную матрицу.
Matrix3x2.Invert(sprite.LocalMatrix, out var invSpriteMatrix);

// 2) Строим inverse-transform сущности с учетом no-rotation/snap-cardinals.
var entityInv = Matrix3Helpers.CreateInverseTransform(spritePos, correctedRotation);

// 3) Переводим world-click в local-space спрайта и дальше в layer-space.
var localPos = Vector2.Transform(Vector2.Transform(worldPos, entityInv), invSpriteMatrix);
```

### 6) Upstream (сервер): `DockingSystem.CanDock`

```csharp
// Матрица стыковки: переводим grid-dock в систему shuttle-dock.
var stationDockMatrix = Matrix3Helpers.CreateInverseTransform(stationDockPos, shuttleDockAngle);
var gridDockMatrix = Matrix3Helpers.CreateTransform(gridDockLocalPos, gridDockAngle);
var dockingMatrix = Matrix3x2.Multiply(stationDockMatrix, gridDockMatrix);

// Сразу проверяем новый AABB шаттла в целевом референсе.
var dockedAabb = dockingMatrix.TransformBox(shuttleAabb);
```

### 7) Upstream (клиент UI): `ShuttleNavControl.Draw`

```csharp
// Цепочка матриц: shuttle-local -> world -> view.
var posMatrix = Matrix3Helpers.CreateTransform(selectedCoordinates.Position, selectedRotation);
var shuttleToWorld = Matrix3x2.Multiply(posMatrix, controlledEntityWorldMatrix);
Matrix3x2.Invert(shuttleToWorld, out var worldToShuttle);

// Для каждой сетки строим world -> shuttle -> view.
var gridToView = Matrix3x2.Multiply(curGridToWorld, worldToShuttle) * shuttleToView;
```

### 8) Upstream (клиент): `AmbientOcclusionOverlay.Draw`

```csharp
// AO-рендеринг идет в render-target, поэтому нужен world -> texture matrix.
var invMatrix = renderTarget.GetWorldToLocalMatrix(viewportEye, scale);

// Берем world matrix сущности/сетки и домножаем на invMatrix цели.
var worldMatrix = xformSystem.GetWorldMatrix(entry.Transform);
var worldToTexture = Matrix3x2.Multiply(worldMatrix, invMatrix);

// Дальше рисуем геометрию уже в корректном texture-space.
worldHandle.SetTransform(worldToTexture);
```

### 9) Fork-unique (клиент): `FieldOfViewSetAlphaOverlay.Draw`

```csharp
// Для каждой component-tree сначала переводим worldBounds в локальные координаты дерева.
var boundsLocalToTree = _xform.GetInvWorldMatrix(treeUid).TransformBox(worldBounds);

// Потом query по AABB выполняется уже в правильном пространстве.
treeComp.Tree.QueryAabb(ref state, QueryCallback, boundsLocalToTree, true);
```

### 10) Fork-unique (shared/server): `HitscanRicochetSystem.OnRicochetPierce`

```csharp
// Позицию попадания переводим полной inverse-матрицей.
var invMatrix = _transform.GetInvWorldMatrix(ent.Owner);
var localFrom = Vector2.Transform(worldHitPos, invMatrix);

// Направление переводим матрицей БЕЗ трансляции.
var invNoTrans = invMatrix;
invNoTrans.M31 = 0f;
invNoTrans.M32 = 0f;
var localDir = Vector2.Transform(worldDir, invNoTrans).Normalized();

// После вычисления отражения возвращаем направление обратно в world-space,
// опять же без трансляции.
```

---

Используй этот skill как узкий матричный playbook: бери только свежие и чистые участки, а сомнительные/старые кейсы отправляй в `rejected`.

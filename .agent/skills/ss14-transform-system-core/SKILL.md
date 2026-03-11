---
name: SS14 Transform System Core
description: Глубокий практический гайд по TransformSystem в Space Station 14: координатная модель (EntityCoordinates/MapCoordinates), иерархия parent-grid-map, безопасное перемещение, привязка/отвязка и client/server паттерны. Используй при телепортах, переносе сущностей, контейнерах, якорении и spatial-оптимизациях.
---

# TransformSystem: Core

Этот skill покрывает именно архитектуру и рабочие приёмы TransformSystem 🙂
Полный каталог API смотри в отдельном skill `SS14 Transform System API`.

## Ментальная модель

Transform в SS14 = дерево пространств:

1. `ParentUid`: непосредственный родитель в иерархии.
2. `GridUid`: грид, на котором находится сущность (или `null`).
3. `MapUid` / `MapID`: карта, на которой находится сущность.
4. `EntityCoordinates`: локальные координаты относительно `ParentUid`.
5. `MapCoordinates`: мировые координаты внутри карты.

Ключевая идея:
- сначала думай, в каком пространстве находятся текущие координаты;
- потом выбирай API, которое явно переводит или сохраняет это пространство.

## Схема перемещения

Базовый поток при перемещении через `SetCoordinates(...)`:

1. При необходимости снять anchor (`Unanchor`), если перемещение это допускает.
2. Обновить локальную позицию/ротацию и, при смене родителя, пересчитать map/grid принадлежность.
3. Поднять события движения (`MoveEvent`, `EntParentChangedMessage`) и обновить spatial-подсистемы.
4. Выполнить post-step traversal, чтобы сущность оказалась на корректном grid/map parent.

Практическое следствие:
- не смешивай прямое мутирование `TransformComponent` и системные методы;
- системные методы держат инварианты дерева, broadphase и сетевой state ✅

## Быстрый выбор API

1. Нужно сдвинуть в локальном пространстве без смены parent?
- `SetLocalPosition`, `SetLocalRotation`, `SetLocalPositionRotation`.
2. Нужно поставить в мировую точку/угол?
- `SetWorldPosition`, `SetWorldRotation`, `SetWorldPositionRotation`, либо `SetMapCoordinates`.
3. Нужно перекинуть между parent-space (контейнер/сущность/грид/карта)?
- `SetCoordinates`, затем при необходимости `AttachToGridOrMap`.
4. Нужно "положить рядом" с учётом контейнеров?
- `DropNextTo` или `PlaceNextTo`.
5. Нужно корректно сравнить дистанцию между разными parent-space?
- `InRange`, а не ручное вычитание локальных векторов.

## Паттерны

- Делать явную нормализацию после телепорта: `SetCoordinates` -> `AttachToGridOrMap`.
- Для смены системы координат использовать `ToMapCoordinates`/`ToCoordinates`.
- Для overlay/визуализации брать пары матриц через `GetWorldPositionRotationMatrixWithInv`.
- Для tile-логики использовать helper-методы (`GetGridOrMapTilePosition`, `TryGetGridTilePosition`).
- Для контейнерно-устойчивого дропа использовать `DropNextTo`.

## Анти-паттерны

- Менять transform-поля напрямую, обходя `SharedTransformSystem` ⚠️
- Сравнивать локальные позиции разных parent-деревьев как будто это одно пространство.
- Телепортировать через `SetCoordinates` и забывать перепривязку к реальному гриду.
- Использовать low-level `SetGridId` в игровом коде без строгой причины.
- Логировать позицию без fallback через `TryGetMapOrGridCoordinates`.

## Примеры из кода

### Пример 1: безопасный дэш с нормализацией parent-space

```csharp
// 1) Переносим сущность в целевые EntityCoordinates (из способности).
_transform.SetCoordinates(user, xform, args.Target);

// 2) Нормализуем parent к фактическому grid/map в целевой точке.
_transform.AttachToGridOrMap(user, xform);
```

### Пример 2: "вживление" снаряда через SetParent

```csharp
// Останавливаем физику и делаем снаряд статичным.
_physics.SetLinearVelocity(projectile, Vector2.Zero, body: body);
_physics.SetBodyType(projectile, BodyType.Static, body: body);

// Перепривязываем снаряд к цели.
_transform.SetParent(projectile, projectileXform, target);

// Локальный оффсет применяем уже после parent-change.
_transform.SetLocalPosition(
    projectile,
    projectileXform.LocalPosition + rotation.RotateVec(embedOffset),
    projectileXform);
```

### Пример 3: overlay-рендер через world+inv matrix

```csharp
var (_, _, worldMatrix, invWorldMatrix) =
    _transform.GetWorldPositionRotationMatrixWithInv(gridXform, xforms);

// Переводим bounds камеры в локальные координаты грида.
var localBounds = invWorldMatrix.TransformBox(worldBounds).Enlarged(grid.TileSize * 2);

// Рисуем в локальном пространстве грида.
drawHandle.SetTransform(worldMatrix);
```

### Пример 4: якорение/разъякорение только системными методами

```csharp
if (!xform.Anchored)
    _transform.AnchorEntity(uid, xform);

// ...игровая логика...

if (xform.Anchored)
    _transform.Unanchor(uid, xform);
```

### Пример 5: направление pop-up через mover-coordinates

```csharp
// MoverCoordinates даёт оперативные координаты в grid/map-терминах.
var moverCoords = _transform.GetMoverCoordinates(observer);

// На их основе выбираем сторону подписи.
var horizontalDir = moverCoords.X <= popupOrigin.X ? 1f : -1f;
```

## Серверные и клиентские usage-ориентиры

- Серверные паттерны: anchor/unanchor, swap/drop/place, безопасный телепорт, перенос в контейнеры и обратно.
- Клиентские паттерны: матричные преобразования для overlay, UI-позиционирование через mover coords, tile-проверки через helper-методы.
- Общий принцип: server-authoritative состояние + клиентское вычисление отображения без нарушения transform-инвариантов.

## Мини-чеклист перед изменениями

- Ясно, в каком пространстве находятся входные координаты.
- Для смены пространства используются `ToMapCoordinates`/`ToCoordinates`.
- После телепорта выполнена нормализация `AttachToGridOrMap` (если это нужно по логике).
- Anchor-логика реализована через `AnchorEntity`/`Unanchor`.
- Нет прямого мутирования устаревших component-сеттеров.

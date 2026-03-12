---
name: SS14 Transform System API
description: Полный справочник API SharedTransformSystem в Space Station 14: разбор всех публичных семейств методов, выбор перегрузок, ограничения и практические паттерны применения на сервере и клиенте. Используй, когда нужно точно выбрать метод TransformSystem и избежать ошибок пространства координат.
---

# TransformSystem: API

Этот skill — полный каталог публичного API `SharedTransformSystem` 🙂
Для общей архитектуры и схемы работы сначала прочитай `SS14 Transform System Core`.

## Как читать этот каталог

- Для каждого семейства сначала выбрать пространство (`EntityCoordinates` или `MapCoordinates`).
- Затем выбрать перегрузку: `uid`-перегрузка или перегрузка с уже резолвленным `TransformComponent`.
- Для bulk-циклов отдавать приоритет перегрузкам, куда можно передать уже имеющиеся компоненты/queries.

## 1) Lifecycle и события

- `Initialize()`
Инициализация системных подписок Transform.

- `MoveEventHandler` + `OnGlobalMoveEvent`
Глобальный C#-event на движение после локального `MoveEvent`.

- `TransformStartupEvent`
Событие старта transform-компонента.

- `ActivateLerp(EntityUid uid, TransformComponent xform)`
Virtual hook для интерполяции на клиенте. Для контент-кода обычно не нужен.

## 2) Координатные преобразования и расстояния

- `IsValid(EntityCoordinates coordinates)`
Проверка валидности координат.

- `WithEntityId(EntityCoordinates coordinates, EntityUid entity)`
Перевод координат в пространство другой сущности.

- `ToMapCoordinates(...)`
Перегрузки:
`ToMapCoordinates(EntityCoordinates coordinates, bool logError = true)`
`ToMapCoordinates(NetCoordinates coordinates)`

- `ToWorldPosition(...)`
Перегрузки:
`ToWorldPosition(EntityCoordinates coordinates, bool logError = true)`
`ToWorldPosition(NetCoordinates coordinates)`

- `ToCoordinates(...)`
Перегрузки:
`ToCoordinates(Entity<TransformComponent?> entity, MapCoordinates coordinates)`
`ToCoordinates(MapCoordinates coordinates)`

- `GetGrid(...)`
Перегрузки:
`GetGrid(EntityCoordinates coordinates)`
`GetGrid(Entity<TransformComponent?> entity)`

- `GetMapId(...)`
Перегрузки:
`GetMapId(EntityCoordinates coordinates)`
`GetMapId(Entity<TransformComponent?> entity)`

- `GetMap(...)`
Перегрузки:
`GetMap(EntityCoordinates coordinates)`
`GetMap(Entity<TransformComponent?> entity)`

- `InRange(...)`
Перегрузки:
`InRange(EntityCoordinates coordA, EntityCoordinates coordB, float range)`
`InRange(Entity<TransformComponent?> entA, Entity<TransformComponent?> entB, float range)`

## 3) Mover и tile helper API

- `GetMoverCoordinates(...)`
Перегрузки:
`GetMoverCoordinates(EntityUid uid)`
`GetMoverCoordinates(EntityUid uid, TransformComponent xform)`
`GetMoverCoordinates(EntityCoordinates coordinates, EntityQuery<TransformComponent> xformQuery)`
`GetMoverCoordinates(EntityCoordinates coordinates)`

- `GetMoverCoordinateRotation(EntityUid uid, TransformComponent xform)`
Mover-координаты + world rotation.

- `GetGridOrMapTilePosition(EntityUid uid, TransformComponent? xform = null)`
Tile-позиция на гриде или карте.

- `GetGridTilePositionOrDefault(Entity<TransformComponent?> entity, MapGridComponent? grid = null)`
Tile-позиция на гриде, либо `Vector2i.Zero`.

- `TryGetGridTilePosition(Entity<TransformComponent?> entity, out Vector2i indices, MapGridComponent? grid = null)`
Безопасный `Try`-вариант.

## 4) Иерархия, parent, anchoring

- `AnchorEntity(...)`
Перегрузки:
`AnchorEntity(EntityUid uid, TransformComponent xform, EntityUid gridUid, MapGridComponent grid, Vector2i tileIndices)` (obsolete)
`AnchorEntity(Entity<TransformComponent> entity, Entity<MapGridComponent> grid, Vector2i tileIndices)`
`AnchorEntity(EntityUid uid, TransformComponent xform, MapGridComponent grid)` (obsolete)
`AnchorEntity(EntityUid uid)`
`AnchorEntity(EntityUid uid, TransformComponent xform)`
`AnchorEntity(Entity<TransformComponent> entity, Entity<MapGridComponent>? grid = null)`

- `Unanchor(...)`
Перегрузки:
`Unanchor(EntityUid uid)`
`Unanchor(EntityUid uid, TransformComponent xform, bool setPhysics = true)`

- `ContainsEntity(EntityUid parent, Entity<TransformComponent?> child)`
Проверка вложенности в transform-дереве.

- `IsParentOf(TransformComponent parent, EntityUid child)`
Быстрая проверка родителя по children-набору.

- `SetGridId(...)`
Перегрузки:
`SetGridId(EntityUid uid, TransformComponent xform, EntityUid? gridId, EntityQuery<TransformComponent>? xformQuery = null)`
`SetGridId(Entity<TransformComponent, MetaDataComponent?> ent, EntityUid? gridId)`
Низкоуровневый API; обычно не использовать в контент-коде.

- `ReparentChildren(...)`
Перегрузки:
`ReparentChildren(EntityUid oldUid, EntityUid uid)`
`ReparentChildren(EntityUid oldUid, EntityUid uid, EntityQuery<TransformComponent> xformQuery)`

- `GetParent(...)`
Перегрузки:
`GetParent(EntityUid uid)`
`GetParent(TransformComponent xform)`

- `GetParentUid(EntityUid uid)`

- `SetParent(...)`
Перегрузки:
`SetParent(EntityUid uid, EntityUid parent)`
`SetParent(EntityUid uid, TransformComponent xform, EntityUid parent, TransformComponent? parentXform = null)`
`SetParent(EntityUid uid, TransformComponent xform, EntityUid parent, EntityQuery<TransformComponent> xformQuery, TransformComponent? parentXform = null)`

## 5) Локальные мутации

- `SetLocalPosition(...)`
Перегрузки:
`SetLocalPosition(TransformComponent xform, Vector2 value)` (obsolete)
`SetLocalPosition(EntityUid uid, Vector2 value, TransformComponent? xform = null)`

- `SetLocalPositionNoLerp(...)`
Перегрузки:
`SetLocalPositionNoLerp(TransformComponent xform, Vector2 value)` (obsolete)
`SetLocalPositionNoLerp(EntityUid uid, Vector2 value, TransformComponent? xform = null)`

- `SetLocalRotationNoLerp(EntityUid uid, Angle value, TransformComponent? xform = null)`

- `SetLocalRotation(...)`
Перегрузки:
`SetLocalRotation(EntityUid uid, Angle value, TransformComponent? xform = null)`
`SetLocalRotation(TransformComponent xform, Angle value)` (obsolete)

- `SetCoordinates(...)`
Перегрузки:
`SetCoordinates(EntityUid uid, EntityCoordinates value)`
`SetCoordinates(Entity<TransformComponent, MetaDataComponent> entity, EntityCoordinates value, Angle? rotation = null, bool unanchor = true, TransformComponent? newParent = null, TransformComponent? oldParent = null)`
`SetCoordinates(EntityUid uid, TransformComponent xform, EntityCoordinates value, Angle? rotation = null, bool unanchor = true, TransformComponent? newParent = null, TransformComponent? oldParent = null)`

- `SetLocalPositionRotation(...)`
Перегрузки:
`SetLocalPositionRotation(TransformComponent xform, Vector2 pos, Angle rot)` (obsolete)
`SetLocalPositionRotation(EntityUid uid, Vector2 pos, Angle rot, TransformComponent? xform = null)`

## 6) World-позиция, ротация, map-координаты

- `GetWorldMatrix(...)`
Перегрузки: uid / component / uid+query / component+query.

- `GetWorldPosition(...)`
Перегрузки: uid / component / uid+query / component+query.

- `GetMapCoordinates(...)`
Перегрузки:
`GetMapCoordinates(EntityUid entity, TransformComponent? xform = null)`
`GetMapCoordinates(TransformComponent xform)`
`GetMapCoordinates(Entity<TransformComponent> entity)`

- `SetMapCoordinates(...)`
Перегрузки:
`SetMapCoordinates(EntityUid entity, MapCoordinates coordinates)`
`SetMapCoordinates(Entity<TransformComponent> entity, MapCoordinates coordinates)`

- `GetWorldPositionRotation(...)`
Перегрузки: uid / component / component+query.

- `GetRelativePositionRotation(...)`
Перегрузки:
`GetRelativePositionRotation(TransformComponent component, EntityUid relative, EntityQuery<TransformComponent> query)` (obsolete)
`GetRelativePositionRotation(TransformComponent component, EntityUid relative)`

- `GetRelativePosition(...)`
Перегрузки:
`GetRelativePosition(TransformComponent component, EntityUid relative, EntityQuery<TransformComponent> query)` (obsolete)
`GetRelativePosition(TransformComponent component, EntityUid relative)`

- `SetWorldPosition(...)`
Перегрузки:
`SetWorldPosition(EntityUid uid, Vector2 worldPos)`
`SetWorldPosition(TransformComponent component, Vector2 worldPos)` (obsolete)
`SetWorldPosition(Entity<TransformComponent> entity, Vector2 worldPos)`

- `GetWorldRotation(...)`
Перегрузки: uid / component / uid+query / component+query.

- `SetWorldRotationNoLerp(Entity<TransformComponent?> entity, Angle angle)`

- `SetWorldRotation(...)`
Перегрузки: uid / component / uid+query / component+query.

- `SetWorldPositionRotation(EntityUid uid, Vector2 worldPos, Angle worldRot, TransformComponent? component = null)`

## 7) Batch-математика и matrix bundle API

- `GetInvWorldMatrix(...)`
Перегрузки: uid / component / uid+query / component+query.

- `GetWorldPositionRotationMatrix(...)`
Перегрузки: uid / component / uid+query / component+query.

- `GetWorldPositionRotationInvMatrix(...)`
Перегрузки: uid / component / uid+query / component+query.

- `GetWorldPositionRotationMatrixWithInv(...)`
Перегрузки: uid / component / uid+query / component+query.

Использовать эти методы, когда нужно сразу несколько производных (`pos+rot+matrix`) без лишнего повторного прохода по иерархии.

## 8) Attach/Detach и "размещение рядом"

- `AttachToGridOrMap(EntityUid uid, TransformComponent? xform = null)`
Нормализация parent к фактическому гриду или карте.

- `TryGetMapOrGridCoordinates(EntityUid uid, out EntityCoordinates? coordinates, TransformComponent? xform = null)`
Безопасное получение текущих grid/map-координат.

- `DetachParentToNull(EntityUid uid, TransformComponent xform)` (obsolete alias)

- `DetachEntity(...)`
Перегрузки:
`DetachEntity(EntityUid uid, TransformComponent? xform = null)`
`DetachEntity(Entity<TransformComponent?> ent)`
`DetachEntity(EntityUid uid, TransformComponent xform, MetaDataComponent meta, TransformComponent? oldXform, bool terminating = false)`

- `DropNextTo(Entity<TransformComponent?> entity, Entity<TransformComponent?> target)`
С учётом контейнеров, иначе рядом в мире.

- `PlaceNextTo(Entity<TransformComponent?> entity, Entity<TransformComponent?> target)`
С тем же parent или контейнером цели.

- `SwapPositions(Entity<TransformComponent?> entity1, Entity<TransformComponent?> entity2)`
Свап позиций/контейнеров с защитой от некорректного parent-loop.

## 9) Legacy и ограничения

- Перегрузки с пометкой `obsolete` оставлены для совместимости: в новом коде предпочитать uid/entity-варианты.
- `SetGridId` и `ActivateLerp` считать low-level API.
- Прямые устаревшие property-setter'ы на `TransformComponent` не использовать для новой логики.

## Паттерны

- В горячих циклах резолвить `TransformComponent` один раз и передавать в перегрузки.
- Для одновременного изменения позиции и угла использовать `SetLocalPositionRotation`.
- Для рендера и spatial-culling использовать bundle-методы матриц.
- После перемещений из контейнеров и сложных parent-операций выполнять `AttachToGridOrMap`, если требуется нормализация.
- Для дропа/спавна рядом использовать `DropNextTo` вместо ручного `SetParent` + `SetCoordinates`.

## Анти-паттерны

- Игнорировать `Try`-результат (`TryGetMapOrGridCoordinates`, `TryGetGridTilePosition`).
- Миксовать локальные и мировые координаты без `ToMapCoordinates`/`ToCoordinates`.
- Использовать `SetGridId` как "обычный" способ перемещения.
- Пытаться вручную поддерживать иерархию контейнеров вместо `DropNextTo/PlaceNextTo`.

## Примеры из кода

### Пример 1: телепорт с корректным выбором parent (grid или map)

```csharp
_transform.AttachToGridOrMap(entity, transform);

if (_map.TryFindGridAt(mapId, position, out var gridUid, out _))
{
    // Переводим world -> local grid и ставим в grid-space.
    var gridPos = Vector2.Transform(position, _transform.GetInvWorldMatrix(gridUid));
    _transform.SetCoordinates(entity, transform, new EntityCoordinates(gridUid, gridPos));
}
else
{
    // Фоллбек в map-space.
    _transform.SetWorldPosition((entity, transform), position);
    _transform.SetParent(entity, transform, mapEntity);
}
```

### Пример 2: spawn fallback через DropNextTo

```csharp
var uid = Spawn(protoName, overrides, doMapInit);

// Если контейнерный insert не удался, безопасно "уронить" рядом.
_xforms.DropNextTo(uid, target);
```

### Пример 3: tile-safe проверка через TryGetGridTilePosition

```csharp
if (args.Grid is {} grid
    && _transform.TryGetGridTilePosition(uid, out var tile)
    && _atmosphere.IsTileAirBlockedCached(grid, tile))
{
    return; // Устройство стоит в заблокированной tile.
}
```

### Пример 4: GetGridTilePositionOrDefault для атмосферных расчётов

```csharp
var indices = _transform.GetGridTilePositionOrDefault((uid, transform));
var tileMix = _atmosphere.GetTileMixture(transform.GridUid, null, indices, true);
```

### Пример 5: смена пространства через WithEntityId

```csharp
// Конвертируем координаты в grid-space, снапим и возвращаем обратно.
var localPos = _transform.WithEntityId(coords, gridUid).Position;
var snappedGrid = new EntityCoordinates(gridUid, snappedLocalPos);
var backToOriginal = _transform.WithEntityId(snappedGrid, coords.EntityId);
```

### Пример 6: обмен позициями сущностей

```csharp
// Возвращает false, если swap невозможен (например, parent-loop риск).
if (!_transform.SwapPositions(first, second))
    return;
```

## Мини-чеклист выбора метода

- Нужен перевод между пространствами?
`ToMapCoordinates` / `ToCoordinates` / `WithEntityId`.
- Нужна локальная правка?
`SetLocal*`.
- Нужна world/map правка?
`SetWorld*` или `SetMapCoordinates`.
- Нужна смена parent/иерархии?
`SetParent` / `SetCoordinates` / `AttachToGridOrMap`.
- Нужен контейнерно-безопасный drop/place/swap?
`DropNextTo` / `PlaceNextTo` / `SwapPositions` ✅

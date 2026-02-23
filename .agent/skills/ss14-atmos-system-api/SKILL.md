---
name: ss14-atmos-system-api
description: Дает полный практический разбор API AtmosSystem в Space Station 14: какие методы для чего использовать, какие из них свежие и безопасные, какие являются legacy/ограниченными, и как правильно комбинировать вызовы в gameplay, устройcтвах и map-level логике.
---

# AtmosSystem: API-практика

Используй этот skill, когда нужно быстро выбрать правильный метод AtmosSystem и применить его без регрессий 🙂

## Что загружать

1. `references/fresh-pattern-catalog.md` — полный API-реестр с пометкой свежести.
2. `references/rejected-snippets.md` — методы/сценарии с ограничениями и TODO.
3. `references/docs-context.md` — связь API-решений с документацией и design intent.

## Быстрый выбор метода

1. Нужно получить газ вокруг сущности: `GetContainingMixture(...)`.
2. Нужен газ конкретного тайла: `GetTileMixture(...)` или пакетно `GetTileMixtures(...)`.
3. Нужна проверка блокировки воздуха для runtime-логики: `IsTileAirBlockedCached(...)`.
4. Нужны строго актуальные данные сразу после изменений: `IsTileAirBlocked(...)`.
5. Нужно зажечь/подогреть атмосферу: `HotspotExpose(...)`.
6. Нужна регистрация в баротравме: `TryAddDeltaPressureEntity(...)` / `TryRemoveDeltaPressureEntity(...)`.
7. Нужна map-level атмосфера: `SetMapAtmosphere(...)`, `SetMapGasMixture(...)`, `SetMapSpace(...)`.
8. Нужна математика переноса газа: `FractionToEqualizePressure(...)` + `MolesToPressureThreshold(...)`.

## Паттерны

1. Делай атмосферные pre-check через `IsTileSpace + IsTileAirBlockedCached`.
2. Для огня/взрывов пользуйся `HotspotExpose`, не меняй hotspot вручную.
3. Для массовых тайловых запросов используй batch-вызов `GetTileMixtures`.
4. Для device-flow сначала оценивай целевое количество молей, затем переноси объем, затем `Merge`.
5. Для DeltaPressure держи lifecycle-симметрию: init/add, shutdown/remove, grid-changed remove+add.
6. После геометрических изменений всегда отправляй `InvalidateTile`.
7. Map atmosphere меняй через immutable путь (`SetMap*`), а не через ручную мутацию map-mixture.

## Анти-паттерны

1. Полагаться на `GetAdjacentTileMixtures(..., includeBlocked, excite)` как на полностью рабочий API.
2. Использовать `SetSimulatedGrid` как рабочий переключатель симуляции.
3. Дергать `IsTileAirBlocked` в tight-loop, когда достаточно cached-версии.
4. Обходить API и напрямую ковырять коллекции `GridAtmosphereComponent` из gameplay-систем.
5. Строить новую бизнес-логику на старых LINDA/Superconductivity public-методах без изоляции.

## Способы оптимизации API-вызовов

1. Предпочитай `IsTileAirBlockedCached(...)` для частых проверок, а `IsTileAirBlocked(...)` оставляй для редких «сразу после инвалидации».
2. Там, где проверяется несколько тайлов, используй `GetTileMixtures(...)` вместо серии `GetTileMixture(...)`.
3. Не ставь `excite: true` «по привычке»: это добавляет тайлы в активную обработку и дергает визуалы.
4. После изменения мира используй `InvalidateTile(...)` и дай `Revalidate` сделать тяжелую часть работы.
5. Для DeltaPressure используй lifecycle API (`TryAdd.../TryRemove...`) вместо ручных структур и линейных поисков.
6. Для map-level изменений обновляй состояние пакетно (`SetMapAtmosphere` или `SetMapGasMixture/SetMapSpace`), а не серией разрозненных операций.
7. В трубных устройствах сначала считай моли/целевое давление, потом делай один перенос+merge, не цепочку мелких переносов.

## Примеры из кода

### 1) Регулятор давления: корректный расчет переноса

```csharp
// 1) Сколько молей нужно убрать, чтобы inlet не превышал threshold.
var deltaMolesToPressureThreshold = AtmosphereSystem.MolesToPressureThreshold(inlet.Air, threshold);

// 2) Сколько молей достаточно, чтобы не перевернуть градиент давлений.
var deltaMolesToEqualize = _atmosphere.FractionToEqualizePressure(inlet.Air, outlet.Air) * inlet.Air.TotalMoles;

// 3) Берем минимум и переносим соответствующий объем.
var deltaMoles = Math.Min(deltaMolesToPressureThreshold, deltaMolesToEqualize);
var removed = inlet.Air.RemoveVolume(volumeToTransfer);
_atmosphere.Merge(outlet.Air, removed);
```

### 2) Lifecycle DeltaPressure API

```csharp
// Init: если энтити на гриде, добавляем в обработку.
_atmosphereSystem.TryAddDeltaPressureEntity(gridUid, ent);

// GridChanged: remove из старого, add в новый.
_atmosphereSystem.TryRemoveDeltaPressureEntity(oldGrid, ent);
_atmosphereSystem.TryAddDeltaPressureEntity(newGrid, ent);

// Shutdown: гарантированная очистка.
_atmosphereSystem.TryRemoveDeltaPressureEntity(currentGrid, ent);
```

### 3) Интеграция взрыва с Atmos API

```csharp
// Взрыв не меняет тайл вручную, а передает тепло через canonical API.
if (temperature != null)
{
    _atmosphere.HotspotExpose(gridUid, tile, temperature.Value, intensity, causeUid, soh: true);
}
```

### 4) Инвалидация тайла после изменения airtight

```csharp
// После смены блокеров воздуха помечаем тайл для revalidate-стадии.
_explosionSystem.UpdateAirtightMap(grid, pos, grid);
_atmosphereSystem.InvalidateTile(grid.Owner, pos);
```

### 5) Безопасный отбор тайла для спавна

```csharp
// Отбрасываем космос и полностью блокированные тайлы.
if (_atmosphere.IsTileSpace(gridUid, mapUid, tile)
    || _atmosphere.IsTileAirBlockedCached(gridUid, tile))
{
    continue;
}
```

## Практическое правило

1. По умолчанию используй только методы со статусом `Fresh-Use`.
2. Методы со статусом `Legacy-Compat` используй только для совместимости и с тестами.
3. Методы со статусом `Risk/TODO` не используй как опору новых правил/API-оберток ⚠️
4. Любую оптимизацию API подтверждай профилированием, а не только «на глаз».

Держи API слой тонким и предсказуемым: Atmos «прощает» мало, если обойти контракт 😅

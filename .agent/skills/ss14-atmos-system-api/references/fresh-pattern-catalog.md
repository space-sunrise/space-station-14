# API Registry (AtmosSystem)

Статусы:
- `Fresh-Use` — можно использовать как основной современный API.
- `Legacy-Compat` — существует и работает, но база старее cutoff.
- `Risk/TODO` — есть явные ограничения, не брать как опору нового кода.
- `Internal` — публично доступно, но предназначено для узкой внутренней логики.

## 1) Query / Tile API

| Метод | Назначение | Дата по blame | Статус |
|---|---|---|---|
| `GetContainingMixture(ent, ...)` (оба overload) | Газ среды для сущности | 2024-03-30 | Risk/TODO |
| `HasAtmosphere(gridUid)` | Проверка наличия атмос-компонента у грида | 2025-11-10 | Fresh-Use |
| `SetSimulatedGrid(gridUid, simulated)` | Событийное переключение simulated-состояния | 2022-07-04 | Risk/TODO |
| `IsSimulatedGrid(gridUid)` | Проверка simulated через event | 2022-07-04 | Legacy-Compat |
| `GetAllMixtures(gridUid, excite)` | Итерация всех смесей грида | 2022-07-04 | Legacy-Compat |
| `InvalidateTile(grid, tile)` | Пометка тайла на revalidate | 2024-03-24 | Fresh-Use |
| `GetTileMixtures(grid, map, tiles, excite)` | Batch-доступ к смесям тайлов | 2024-03-30 | Fresh-Use |
| `GetTileMixture(entity, excite)` | Смесь тайла по сущности | 2025-11-10 | Fresh-Use |
| `GetTileMixture(grid, map, tile, excite)` | Смесь конкретного тайла | 2024-03-30 | Fresh-Use |
| `ReactTile(grid, tile)` | Ручной event-trigger реакции тайла | 2022-07-04 | Legacy-Compat |
| `IsTileAirBlocked(grid, tile, dirs)` | On-the-fly airtight check | 2025-11-10 | Fresh-Use |
| `IsTileAirBlockedCached(grid, tile, dirs)` | Cached airtight check | 2025-12-23 | Fresh-Use |
| `IsTileSpace(grid, map, tile)` | Проверка space-поведения тайла | 2024-03-28 | Fresh-Use |
| `IsTileMixtureProbablySafe(grid, map, tile)` | Pressure+temperature safety check | 2024-03-28 | Fresh-Use |
| `GetTileHeatCapacity(grid, map, tile)` | Heat capacity тайла | 2024-03-28 | Fresh-Use |
| `GetAdjacentTileMixtures(grid, tile, includeBlocked, excite)` | Соседние смеси | 2024-03-28 | Risk/TODO |

## 2) Fire / Hotspot API

| Метод | Назначение | Дата по blame | Статус |
|---|---|---|---|
| `HotspotExpose(grid, tile, temp, volume, spark, soh)` | Инициировать/усилить hotspot на тайле | 2025-11-10 | Fresh-Use |
| `HotspotExpose(tile, temp, volume, spark, soh)` | То же через `TileAtmosphere` | 2025-11-10 | Fresh-Use |
| `HotspotExtinguish(grid, tile)` | Потушить hotspot | 2022-07-04 | Legacy-Compat |
| `IsHotspotActive(grid, tile)` | Проверить активность hotspot | 2022-07-04 | Legacy-Compat |

## 3) Registration / Device API

| Метод | Назначение | Дата по blame | Статус |
|---|---|---|---|
| `AddPipeNet(grid, pipeNet)` | Регистрация pipe-net | 2024-03-28 | Fresh-Use |
| `RemovePipeNet(grid, pipeNet)` | Удаление pipe-net | 2024-03-28 | Fresh-Use |
| `AddAtmosDevice(grid, device)` | Регистрация atmos-device | 2024-03-28 | Fresh-Use |
| `RemoveAtmosDevice(grid, device)` | Удаление atmos-device | 2024-03-28 | Fresh-Use |
| `TryAddDeltaPressureEntity(grid, ent)` | Добавить в delta-pressure список | 2025-09-03 | Fresh-Use |
| `TryRemoveDeltaPressureEntity(grid, ent)` | Удалить из delta-pressure списка | 2025-09-03 | Fresh-Use |
| `IsDeltaPressureEntityInList(grid, ent)` | Проверка membership delta-pressure | 2025-09-03 | Fresh-Use |

## 4) Map API

| Метод | Назначение | Дата по blame | Статус |
|---|---|---|---|
| `SetMapAtmosphere(map, space, mixture)` | Пакетно обновить map atmosphere | 2024-03-24 | Fresh-Use |
| `SetMapGasMixture(map, mixture, ..., updateTiles)` | Обновить map смесь (immutable путь) | 2024-03-24 | Fresh-Use |
| `SetMapSpace(map, space, ..., updateTiles)` | Обновить map space flag | 2024-03-24 | Fresh-Use |
| `RefreshAllGridMapAtmospheres(map)` | Принудительный refresh map-atmos тайлов | 2024-03-24 | Fresh-Use |

## 5) Gas Math / Transfer API

| Метод | Назначение | Дата по blame | Статус |
|---|---|---|---|
| `GetHeatCapacity(mixture, applyScaling)` | Теплоемкость смеси | 2023-12-15 | Legacy-Compat |
| `PumpSpeedup()` | Коэффициент ускорения насосов | 2023-12-11 | Legacy-Compat |
| `GetThermalEnergy(...)` (оба overload) | Тепловая энергия | 2021-06-23 / 2021-07-23 | Legacy-Compat |
| `AddHeat(mixture, dQ)` | Внести/убрать тепло | 2023-08-06 | Legacy-Compat |
| `Merge(receiver, giver)` | Слить смеси | 2021-06-23 | Legacy-Compat |
| `DivideInto(source, receivers)` | Разделить смесь по получателям | 2022-06-03 | Legacy-Compat |
| `ReleaseGasTo(mix, output, pressure)` | Выпустить газ до pressure target | 2021-06-23 | Legacy-Compat |
| `PumpGasTo(mix, output, pressure)` | Накачать газ до pressure target | 2021-06-23 | Legacy-Compat |
| `ScrubInto(mix, dst, filterGases)` | Фильтрация выбранных газов | 2021-06-23 | Legacy-Compat |
| `FractionToEqualizePressure(m1, m2)` | Доля переноса для выравнивания давления | 2025-07-03 | Fresh-Use |
| `MolesToPressureThreshold(mix, targetP)` | Моли до порога давления | 2025-07-03 | Fresh-Use |
| `IsMixtureProbablySafe(mix)` | Safe-check смеси | 2022-07-04 | Legacy-Compat |
| `CompareExchange(TileAtmosphere, TileAtmosphere)` | Сравнение обмена (tile archived) | 2024-09-30 | Fresh-Use |
| `CompareExchange(GasMixture, GasMixture)` | Сравнение обмена (mixture) | 2022-07-04 | Legacy-Compat |
| `React(mix, holder)` | Запуск реакций газа | 2021-07-12 | Legacy-Compat |
| `AddMolsToMixture(mix, span)` | Безопасное добавление молей с clamp | 2025-12-24 | Fresh-Use |

## 6) Utility / Processing / Internal Public API

| Метод/тип | Назначение | Дата по blame | Статус |
|---|---|---|---|
| `InvalidateVisuals(grid, tile)` | Invalidate gas overlay tile | 2024-03-30 | Fresh-Use |
| `QueueTileTrim(atmos, tile)` | Очередь trim отключенных map-тайлов | 2024-03-24 | Internal |
| `RealAtmosTime()` | Эффективное время между полными проходами | 2023-09-09 | Legacy-Compat |
| `InvalidateAllTiles(entity)` | Полная инвалидация тайлов грида | 2024-03-24 | Internal |
| `GetTileRef(tile)` | Получение `TileRef` из `TileAtmosphere` | 2023-12-15 | Legacy-Compat |
| `RebuildGridAtmosphere(ent)` | Ручная пересборка атмоса грида | 2025-12-23 | Internal |
| `RunProcessingStage(...)` | Benchmark helper | 2025-09-03 | Internal |
| `RunProcessingFull(...)` | Benchmark helper | 2025-10-31 | Internal |
| `SetAtmosphereSimulation(...)` | Benchmark helper для simulate flag | 2025-10-31 | Internal |
| `AirtightData` | Структура cached airtight метаданных | 2025-11-02 | Fresh-Use |

## 7) Legacy Low-Level Public (не брать для нового gameplay API)

| Метод | Назначение | Дата по blame | Статус |
|---|---|---|---|
| `ExperiencePressureDifference(...)` | Логика high-pressure воздействия | 2022-02-20 | Legacy-Compat |
| `GetHeatCapacityArchived(...)` | LINDA helper | 2022-07-04 | Legacy-Compat |
| `Share(...)` | LINDA gas share | 2022-07-04 | Legacy-Compat |
| `TemperatureShare(...)` (оба overload) | LINDA temperature share | 2022-07-04 | Legacy-Compat |
| `ConsiderSuperconductivity(...)` (оба overload) | Superconduction pre-check | 2021-07-20 | Legacy-Compat |
| `FinishSuperconduction(...)` (оба overload) | Завершение superconduction | 2021-07-20 | Legacy-Compat |
| `NeighborConductWithSource(...)` | Передача тепла соседям | 2021-07-20 | Legacy-Compat |
| `RadiateToSpace(tile)` | Излучение в космос | 2021-07-20 | Legacy-Compat |

## Примечание

`Legacy-Compat` не означает «сломано». Это означает: не использовать как основной шаблон нового API-слоя без дополнительных тестов и проверки фактического поведения на свежих потребителях.

## Optimization Recipes

1. `spawn/check tile`: `IsTileSpace(...) + IsTileAirBlockedCached(...)` -> при необходимости `GetTileMixture(...)`.
2. `multi-tile scan`: `GetTileMixtures(...)` + локальная фильтрация -> только для кандидатов `excite: true`.
3. `world change`: изменить объект/тайл -> `InvalidateTile(...)` -> дождаться `Revalidate`, не форсить полный пересчет.
4. `delta pressure entities`: всегда `TryAddDeltaPressureEntity(...) / TryRemoveDeltaPressureEntity(...)`, не вести отдельный user-side список.
5. `map atmosphere update`: пакетно `SetMapAtmosphere(...)` или `SetMapGasMixture(...) + SetMapSpace(...)` вместо точечных обходов.

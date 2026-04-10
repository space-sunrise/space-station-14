# API Registry (AtmosSystem)

Statuses:
- `Fresh-Use` - can be used as the main modern API.
- `Legacy-Compat` - exists and works, but the database is older than cutoff.
- `Risk/TODO` - there are obvious restrictions, do not use it as a support for new code.
- `Internal` - publicly available, but intended for narrow internal logic.

## 1) Query / Tile API

| Method | Destination | Date by blame | Status |
|---|---|---|---|
| `GetContainingMixture(ent, ...)` (both overload) | Medium gas for essence | 2024-03-30 | Risk/TODO |
| `HasAtmosphere(gridUid)` | Checking whether the grid has an Atmos component | 2025-11-10 | Fresh-Use |
| `SetSimulatedGrid(gridUid, simulated)` | Event switching of simulated state | 2022-07-04 | Risk/TODO |
| `IsSimulatedGrid(gridUid)` | Checking simulated via event | 2022-07-04 | Legacy-Compat |
| `GetAllMixtures(gridUid, excite)` | Iterate all grid mixtures | 2022-07-04 | Legacy-Compat |
| `InvalidateTile(grid, tile)` | Marking a tile on revalidate | 2024-03-24 | Fresh-Use |
| `GetTileMixtures(grid, map, tiles, excite)` | Batch access to tile mixtures | 2024-03-30 | Fresh-Use |
| `GetTileMixture(entity, excite)` | Tile mix by essence | 2025-11-10 | Fresh-Use |
| `GetTileMixture(grid, map, tile, excite)` | Mixture of a specific tile | 2024-03-30 | Fresh-Use |
| `ReactTile(grid, tile)` | Manual event-trigger tile reaction | 2022-07-04 | Legacy-Compat |
| `IsTileAirBlocked(grid, tile, dirs)` | On-the-fly airtight check | 2025-11-10 | Fresh-Use |
| `IsTileAirBlockedCached(grid, tile, dirs)` | Cached airtight check | 2025-12-23 | Fresh-Use |
| `IsTileSpace(grid, map, tile)` | Checking the space behavior of a tile | 2024-03-28 | Fresh-Use |
| `IsTileMixtureProbablySafe(grid, map, tile)` | Pressure+temperature safety check | 2024-03-28 | Fresh-Use |
| `GetTileHeatCapacity(grid, map, tile)` | Heat capacity tile | 2024-03-28 | Fresh-Use |
| `GetAdjacentTileMixtures(grid, tile, includeBlocked, excite)` | Adjacent mixtures | 2024-03-28 | Risk/TODO |

## 2) Fire / Hotspot API

| Method | Destination | Date by blame | Status |
|---|---|---|---|
| `HotspotExpose(grid, tile, temp, volume, spark, soh)` | Initiate/strengthen hotspot on a tile | 2025-11-10 | Fresh-Use |
| `HotspotExpose(tile, temp, volume, spark, soh)` | The same via `TileAtmosphere` | 2025-11-10 | Fresh-Use |
| `HotspotExtinguish(grid, tile)` | Extinguish hotspot | 2022-07-04 | Legacy-Compat |
| `IsHotspotActive(grid, tile)` | Check hotspot activity | 2022-07-04 | Legacy-Compat |

## 3) Registration / Device API

| Method | Destination | Date by blame | Status |
|---|---|---|---|
| `AddPipeNet(grid, pipeNet)` | Registration pipe-net | 2024-03-28 | Fresh-Use |
| `RemovePipeNet(grid, pipeNet)` | Removing pipe-net | 2024-03-28 | Fresh-Use |
| `AddAtmosDevice(grid, device)` | Registration atmos-device | 2024-03-28 | Fresh-Use |
| `RemoveAtmosDevice(grid, device)` | Removing atmos-device | 2024-03-28 | Fresh-Use |
| `TryAddDeltaPressureEntity(grid, ent)` | Add to delta-pressure list | 2025-09-03 | Fresh-Use |
| `TryRemoveDeltaPressureEntity(grid, ent)` | Remove from delta-pressure list | 2025-09-03 | Fresh-Use |
| `IsDeltaPressureEntityInList(grid, ent)` | Checking membership delta-pressure | 2025-09-03 | Fresh-Use |

## 4) Map API

| Method | Destination | Date by blame | Status |
|---|---|---|---|
| `SetMapAtmosphere(map, space, mixture)` | Batch update map atmosphere | 2024-03-24 | Fresh-Use |
| `SetMapGasMixture(map, mixture, ..., updateTiles)` | Update map mixture (immutable path) | 2024-03-24 | Fresh-Use |
| `SetMapSpace(map, space, ..., updateTiles)` | Update map space flag | 2024-03-24 | Fresh-Use |
| `RefreshAllGridMapAtmospheres(map)` | Forced refresh map-atmos tiles | 2024-03-24 | Fresh-Use |

## 5) Gas Math / Transfer API

| Method | Destination | Date by blame | Status |
|---|---|---|---|
| `GetHeatCapacity(mixture, applyScaling)` | Heat capacity of the mixture | 2023-12-15 | Legacy-Compat |
| `PumpSpeedup()` | Pump acceleration coefficient | 2023-12-11 | Legacy-Compat |
| `GetThermalEnergy(...)` (both overload) | Thermal energy | 2021-06-23 / 2021-07-23 | Legacy-Compat |
| `AddHeat(mixture, dQ)` | Add/remove heat | 2023-08-06 | Legacy-Compat |
| `Merge(receiver, giver)` | Drain mixtures | 2021-06-23 | Legacy-Compat |
| `DivideInto(source, receivers)` | Divide mixture among recipients | 2022-06-03 | Legacy-Compat |
| `ReleaseGasTo(mix, output, pressure)` | Release gas to pressure target | 2021-06-23 | Legacy-Compat |
| `PumpGasTo(mix, output, pressure)` | Pump gas to pressure target | 2021-06-23 | Legacy-Compat |
| `ScrubInto(mix, dst, filterGases)` | Filtration of selected gases | 2021-06-23 | Legacy-Compat |
| `FractionToEqualizePressure(m1, m2)` | Transfer fraction for pressure equalization | 2025-07-03 | Fresh-Use |
| `MolesToPressureThreshold(mix, targetP)` | Moths to pressure threshold | 2025-07-03 | Fresh-Use |
| `IsMixtureProbablySafe(mix)` | Safe-check mixtures | 2022-07-04 | Legacy-Compat |
| `CompareExchange(TileAtmosphere, TileAtmosphere)` | Exchange comparison (tile archived) | 2024-09-30 | Fresh-Use |
| `CompareExchange(GasMixture, GasMixture)` | Exchange comparison (mixture) | 2022-07-04 | Legacy-Compat |
| `React(mix, holder)` | Triggering gas reactions | 2021-07-12 | Legacy-Compat |
| `AddMolsToMixture(mix, span)` | Safely adding moles with clamp | 2025-12-24 | Fresh-Use |

## 6) Utility / Processing / Internal Public API

| Method/type | Destination | Date by blame | Status |
|---|---|---|---|
| `InvalidateVisuals(grid, tile)` | Invalidate gas overlay tile | 2024-03-30 | Fresh-Use |
| `QueueTileTrim(atmos, tile)` | Trim queue of disabled map tiles | 2024-03-24 | Internal |
| `RealAtmosTime()` | Effective time between complete passes | 2023-09-09 | Legacy-Compat |
| `InvalidateAllTiles(entity)` | Complete invalidation of grid tiles | 2024-03-24 | Internal |
| `GetTileRef(tile)` | Getting `TileRef` from `TileAtmosphere` | 2023-12-15 | Legacy-Compat |
| `RebuildGridAtmosphere(ent)` | Manual reassembly of the Atmos grid | 2025-12-23 | Internal |
| `RunProcessingStage(...)` | Benchmark helper | 2025-09-03 | Internal |
| `RunProcessingFull(...)` | Benchmark helper | 2025-10-31 | Internal |
| `SetAtmosphereSimulation(...)` | Benchmark helper for simulate flag | 2025-10-31 | Internal |
| `AirtightData` | Structure of cached airtight metadata | 2025-11-02 | Fresh-Use |

## 7) Legacy Low-Level Public (do not take for the new gameplay API)

| Method | Destination | Date by blame | Status |
|---|---|---|---|
| `ExperiencePressureDifference(...)` | Logic of high-pressure effects | 2022-02-20 | Legacy-Compat |
| `GetHeatCapacityArchived(...)` | LINDA helper | 2022-07-04 | Legacy-Compat |
| `Share(...)` | LINDA gas share | 2022-07-04 | Legacy-Compat |
| `TemperatureShare(...)` (both overload) | LINDA temperature share | 2022-07-04 | Legacy-Compat |
| `ConsiderSuperconductivity(...)` (both overload) | Superconduction pre-check | 2021-07-20 | Legacy-Compat |
| `FinishSuperconduction(...)` (both overload) | Completion of superconduction | 2021-07-20 | Legacy-Compat |
| `NeighborConductWithSource(...)` | Transferring heat to neighbors | 2021-07-20 | Legacy-Compat |
| `RadiateToSpace(tile)` | Radiation into space | 2021-07-20 | Legacy-Compat |

## Note

`Legacy-Compat` does not mean "broken". This means: do not use a new API layer as the main template without additional tests and checking the actual behavior on fresh consumers.

## Optimization Recipes

1. `spawn/check tile`: `IsTileSpace(...) + IsTileAirBlockedCached(...)` -> if necessary `GetTileMixture(...)`.
2. `multi-tile scan`: `GetTileMixtures(...)` + local filtering -> only for `excite: true` candidates.
3. `world change`: change object/tile -> `InvalidateTile(...)` -> wait for `Revalidate`, do not force a full recalculation.
4. `delta pressure entities`: always `TryAddDeltaPressureEntity(...) / TryRemoveDeltaPressureEntity(...)`, do not maintain a separate user-side list.
5. `map atmosphere update`: batch `SetMapAtmosphere(...)` or `SetMapGasMixture(...) + SetMapSpace(...)` instead of point traversals.

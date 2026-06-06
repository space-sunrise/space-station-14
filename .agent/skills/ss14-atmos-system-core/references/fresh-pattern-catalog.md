# Fresh Pattern Catalog (Atmos Core)

| Class/method | Pattern | Why is it useful | Layer | Date by blame | Status |
|---|---|---|---|---|---|
| `AtmosphereSystem.ProcessAtmosphere(...)` | Explicit state machine by stage with `Return/Continue/Finished` | Controlled processing and pause by time-budget | Server | 2025-10-31 | Use |
| `AtmosphereSystem.ProcessDeltaPressure(...)` | Separate stage with cursor, batches and post-pass damage | Stable operation with large lists of entities | Server | 2025-09-03 | Use |
| `AtmosphereSystem.TryAddDeltaPressureEntity(...)` | Registration in list+lookup with component-state synchronization | O(1) membership and correct lifecycle | Server | 2025-09-03 | Use |
| `AtmosphereSystem.TryRemoveDeltaPressureEntity(...)` | Swap-remove + cursor/lookup adjustment | Quick removal without holes in indexing | Server | 2025-09-03 | Use |
| `AtmosphereSystem.IsDeltaPressureEntityInList(...)` | List/dict invariant via assert | Early detection of structure desynchronization | Server | 2025-09-03 | Use |
| `AtmosphereSystem.InvalidateTile(...)` | Tile Invalidation Instead of Immediate Heavy Revaluation | Cheap and safe way to update the atmosphere | Server | 2024-03-24 | Use |
| `AtmosphereSystem.GetTileMixtures(...)` | Batch query for mixtures based on an array of tiles | Reduces overhead for multiple tile-queries | Server | 2024-03-30 | Use |
| `AtmosphereSystem.IsTileAirBlockedCached(...)` | Cached airtight-check for runtime | Fast pre-check without rebuilding airtight data | Server | 2025-12-23 | Use |
| `AtmosphereSystem.ProcessRevalidate(...)` | Queue invalidated tiles + update tile data/adjacent/air/visuals | Centralizes consistency restoration | Server | 2024-03-24 | Use |
| `AtmosphereSystem.SetMapAtmosphere/SetMapGasMixture/SetMapSpace(...)` | Map mixture as immutable + bulk refresh map-tiles | Map-level consistent behavior | Server | 2024-03-24 | Use |
| `AtmosphereSystem.InvalidateVisuals(...)` | Explicit invalidation overlay after important tile changes | Decoupling gas physics and rendering | Server | 2024-03-30 | Use |
| `AirtightSystem.InvalidatePosition(...)` | Integration of airtight+explosion map+atmos-invalidation | Single channel response to air blockers | Server | 2024-03-24 / 2025-09-29 | Use |
| `SharedAtmosphereSystem.Initialize(...)` | Centralized loading `GasPrototype` with fail logging | Unified shared gas database for client/server | Shared | 2025-11-21 | Use |
| `SharedAtmosphereSystem.OnMaskToggled(...)` | Event synchronization of breath tool and internals | Correct shared breathing logic | Shared | 2025-05-02 | Use |
| `GasTileOverlaySystem.OnHandleState(...)` | Single merge full/delta states of chunks | Stable client overlay synchronization | Client | 2024-05-24 | Use |
| `GasTileOverlay.Draw(...)` (`GetWorldPositionRotationMatrixWithInv`) | world/inv transform to draw grid-local overlay | Correct binding of gas overlay to grids | Client | 2025-02-10 / 2024-06-02 | Use |
| `Explosion processing -> HotspotExpose(...)` | Explosion warms the atmosphere through an API, not a workaround | Proper integration of external systems with Atmos | Server | 2025-12-15 | Use |
| `GameRuleSystem utility` (`IsTileSpace` + `IsTileAirBlockedCached`) | Filtering spawn tiles through atmospheric restrictions | Fast and practical pre-check for gameplay | Server | 2024-03-28 / 2026-01-27 | Use |

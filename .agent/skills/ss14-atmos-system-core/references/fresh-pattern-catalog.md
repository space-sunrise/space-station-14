# Fresh Pattern Catalog (Atmos Core)

| Класс/метод | Паттерн | Почему полезно | Слой | Дата по blame | Статус |
|---|---|---|---|---|---|
| `AtmosphereSystem.ProcessAtmosphere(...)` | Явный state machine по стадиям с `Return/Continue/Finished` | Контролируемая обработка и пауза по time-budget | Server | 2025-10-31 | Использовать |
| `AtmosphereSystem.ProcessDeltaPressure(...)` | Отдельная стадия с курсором, батчами и post-pass урона | Стабильная работа при больших списках энтити | Server | 2025-09-03 | Использовать |
| `AtmosphereSystem.TryAddDeltaPressureEntity(...)` | Регистрация в list+lookup с синхронизацией component-state | O(1) membership и корректный lifecycle | Server | 2025-09-03 | Использовать |
| `AtmosphereSystem.TryRemoveDeltaPressureEntity(...)` | Swap-remove + корректировка cursor/lookup | Быстрое удаление без дыр в индексации | Server | 2025-09-03 | Использовать |
| `AtmosphereSystem.IsDeltaPressureEntityInList(...)` | Инвариант list/dict через assert | Раннее обнаружение рассинхрона структур | Server | 2025-09-03 | Использовать |
| `AtmosphereSystem.InvalidateTile(...)` | Инвалидация тайла вместо немедленной тяжелой переоценки | Дешевый и безопасный путь обновления атмоса | Server | 2024-03-24 | Использовать |
| `AtmosphereSystem.GetTileMixtures(...)` | Batch-запрос смесей по массиву тайлов | Снижает накладные расходы на множественные tile-query | Server | 2024-03-30 | Использовать |
| `AtmosphereSystem.IsTileAirBlockedCached(...)` | Cached airtight-check для runtime | Быстрый pre-check без пересборки airtight-данных | Server | 2025-12-23 | Использовать |
| `AtmosphereSystem.ProcessRevalidate(...)` | Очередь invalidated tiles + обновление tile data/adjacent/air/visuals | Централизует восстановление консистентности | Server | 2024-03-24 | Использовать |
| `AtmosphereSystem.SetMapAtmosphere/SetMapGasMixture/SetMapSpace(...)` | Map mixture как immutable + массовый refresh map-tiles | Согласованное map-level поведение | Server | 2024-03-24 | Использовать |
| `AtmosphereSystem.InvalidateVisuals(...)` | Явная invalidation overlay после важных изменений тайла | Развязка физики газа и рендера | Server | 2024-03-30 | Использовать |
| `AirtightSystem.InvalidatePosition(...)` | Интеграция airtight+explosion map+атмос-инвалидации | Единый канал реакции на блокеры воздуха | Server | 2024-03-24 / 2025-09-29 | Использовать |
| `SharedAtmosphereSystem.Initialize(...)` | Централизованная загрузка `GasPrototype` с fail-логированием | Единая shared база газов для client/server | Shared | 2025-11-21 | Использовать |
| `SharedAtmosphereSystem.OnMaskToggled(...)` | Событийная синхронизация breath tool и internals | Корректная shared-логика дыхания | Shared | 2025-05-02 | Использовать |
| `GasTileOverlaySystem.OnHandleState(...)` | Единый merge full/delta состояния чанков | Стабильная клиентская синхронизация overlay | Client | 2024-05-24 | Использовать |
| `GasTileOverlay.Draw(...)` (`GetWorldPositionRotationMatrixWithInv`) | world/inv transform для рисования grid-local overlay | Корректная привязка gas overlay к сеткам | Client | 2025-02-10 / 2024-06-02 | Использовать |
| `Explosion processing -> HotspotExpose(...)` | Эксплозия греет атмосферу через API, а не обходной код | Правильная интеграция внешних систем с Atmos | Server | 2025-12-15 | Использовать |
| `GameRuleSystem utility` (`IsTileSpace` + `IsTileAirBlockedCached`) | Фильтрация spawn-тайлов через атмосферные ограничения | Быстрый и практичный pre-check для gameplay | Server | 2024-03-28 / 2026-01-27 | Использовать |

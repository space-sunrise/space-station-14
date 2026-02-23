# Rejected Snippets (Atmos API)

| Метод/сценарий | Почему отклонен как шаблон | Сигнал |
|---|---|---|
| `GetAdjacentTileMixtures(..., includeBlocked, excite)` | Параметры `includeBlocked` и `excite` сейчас не отрабатываются как ожидается | TODO в методе |
| `SetSimulatedGrid(...)` | В текущем состоянии фактически не подтвержден рабочий путь через subscribers | TODO о том, что subscribers нет |
| `GetContainingMixture(...)` (container chain) | Нет рекурсивного подъема по parent-контейнерам, риск неверной среды | TODO о recursive iterate |
| `ReactTile(...)` как опора новой логики | Старый event-wrapper, слабая практическая ценность для нового API-слоя | База 2022 |
| `Firelock` pressure-кейс на `GetTileMixtures(...)` | Сильно специализированный и исторический код; не переносить как общий API-паттерн | Основной блок 2022 |
| Legacy transfer API (`Merge/ReleaseGasTo/PumpGasTo/ScrubInto`) как «новый стиль» | Методы рабочие, но кодовая база историческая; нужен защитный слой и тесты | База 2021-2023 |
| Прямая мутация map mixture без `SetMap*` | Ломает ожидаемую immutable семантику и refresh map-тайлов | Нарушение контракта Map API |
| `RealAtmosTime()` как строгий инвариант в новых подсистемах | Старый helper, не должен быть единственным источником временной модели | База 2023 |
| Client `AtmosphereSystem.OnMapHandleState` как референс «современного» client-api | Слишком старый участок для нового стандарта API-поведения | 2023-06-28 |
| Большой draw-loop gas overlay как API-пример | Есть TODO и старые блоки; использовать только точечные свежие части | TODO + 2022-2023 |
| DeltaPressure internal SIMD TODO-блок | Текущий вариант рабочий, но рядом зафиксировано ограничение по batching | TODO о batch operations |

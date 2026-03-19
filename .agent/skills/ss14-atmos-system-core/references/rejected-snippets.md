# Rejected Snippets (Atmos Core)

| Зона | Что найдено | Почему не брать как эталон | Сигнал |
|---|---|---|---|
| `SetSimulatedGrid(...)` | Событийный вызов без фактических подписчиков | Метод присутствует, но практической ценности в текущем коде не подтверждено | TODO о нулевых subscribers + база 2022 |
| `GetAdjacentTileMixtures(...)` | `includeBlocked` и `excite` не обрабатываются | Поведение параметров не соответствует ожиданиям API | TODO в самом методе |
| `GetContainingMixture(...)` (container chain) | Нет рекурсивного прохода по parent-контейнерам | Вложенные контейнеры могут дать неполную картину среды | TODO о recursive iterate |
| `Grid split -> overlay immediate update` | При split заметен визуальный flicker до следующего обновления overlay | Есть незакрытая техническая дыра в момент split | TODO про force update overlay |
| `GridFixTileVacuum(...)` фрагмент remove+merge | Внутри комментарий о сомнительной операции remove/re-add | Ненадежный участок для копирования паттерна | TODO с явным сомнением автора |
| `Firelock` specialized pressure logic | Использует атмосферный API, но основная реализация старше cutoff | Можно изучать как legacy-поведение, но не как современный стиль | Основной блок 2022 |
| `GasTileOverlay.Draw(...)` большая часть цикла | Основной draw-loop старый, рядом TODO по callback | Не использовать как образец нового client-кода | База 2022-2023 + TODO |
| `SharedGasTileOverlaySystem` fire-color TODO | Логика нуждается в дальнейшей стабилизации для dirty/tolerance | Риск неправильных обновлений по мелким колебаниям температуры | TODO о fire color / tolerance |
| `AtmosphereSystem.Gases`: `Merge/ReleaseGasTo/PumpGasTo/ScrubInto` | Ключевые реализации исторические | Работают, но не использовать как стиль новой архитектуры без изоляции/тестов | Основные строки 2021-2023 |
| `LINDA/Superconductivity` public методы | Публичные low-level функции с очень старой базой | Держать как legacy-совместимость, не как опорный шаблон | 2021-2022 |
| `RealAtmosTime()` | Полезный helper, но базовая реализация старше cutoff | Использовать осторожно и не строить на нем новые инварианты без проверки | База 2023 |

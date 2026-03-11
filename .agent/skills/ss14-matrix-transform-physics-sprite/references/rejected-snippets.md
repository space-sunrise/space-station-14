# Rejected Snippets

| Класс/метод | Что найдено | Причина отбраковки | Дата/сигнал |
|---|---|---|---|
| `TransformComponent.GetWorldPositionRotationMatrixWithInv(EntityQuery<TransformComponent>)` | Внутренний проход по parent-цепочке с ручной сборкой world/inv-world матриц | Строки в ключевом блоке старше cutoff; не использовать как современный эталон | 2021-11-26, 2022-01-25, 2022-02-02 |
| `ChunkingSystem` (блок с `GetInvWorldMatrix(...).TransformBox(...)`) | Конверсия bounds в локал chunk-tree | Строка матричного перехода старше cutoff | 2023-11-28 |
| `SharedBroadphaseSystem` (grid-vs-grid chunk block) | Матричные `TransformBox` при chunk-пересечениях | Рядом с матричным участком есть проблемный TODO, плюс часть строк старше cutoff | TODO: `AddPair ... O(n)` + строки 2023-05-28 |
| `SpriteComponent.Layer.GetLayerDrawMatrix(...)` | Поворотная layer-matrix для RSI-направлений | Явный TODO/комментарий о лишней matrix transformation в этом участке | TODO RENDERING + remark про unnecessary matrix transformation |
| `StationAiVisionSystem` (блок `invMatrix.TransformBox(worldBounds)`) | Локализация AABB для vision-query | Рядом находится TODO в том же матричном фрагменте | TODO о параллельном запуске рядом с TransformBox |
| `_Sunrise` кейс с `WorldMatrix.TransformBox(LocalAABB)` для сетки | Рабочий world-AABB переход | Отброшено как дубликат уже покрытого upstream-паттерна FTL/proximity AABB | Дублирование паттерна, уникальной матричной идеи нет |
| Старые component-обертки `SpriteComponent`/`TransformComponent` с `[Obsolete]` | Проксирование к системным методам | Не брать в примеры: риск закрепить устаревший стиль API и смешать слой ответственности | Сигнал: `[Obsolete]` в матричных обертках |

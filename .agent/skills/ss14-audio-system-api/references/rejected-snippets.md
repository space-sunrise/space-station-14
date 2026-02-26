# Rejected Snippets (Audio API)

| API/зона | Проблема | Почему опасно для новых правил | Сигнал |
|---|---|---|---|
| `LoadStream<T>` (server implementation) | Пустой TODO-body | Нельзя строить серверный stream API поверх заглушки | TODO remove |
| `PlayEntity(..., filter, uid, ...)` (server) | TODO про custom range и лишние PVS overrides | Возможны неверные ожидания по дальности/фильтрам | TODO AUDIO |
| `PlayPvs(..., EntityCoordinates ...)` (server) | TODO про transform mess/оптимизацию | Риск копировать временную реализацию | TODO Transform |
| `CalculateAudioPosition(...)` | TODO о переносе clamp в source-layer | Нестабильная зона границы между shared и OpenAL source | TODO clamp |
| `AudioMessage` (replay/network payload) | TODO про bandwidth-intensive формат | Не подходит как база для нового net-format аудио | TODO bandwidth |
| `ProcessStream(...)` (client) | Прямой комментарий «replicate old behaviour for now» | Внутренняя логика не считается финальной архитектурой | TODO в коде |
| `GetAttenuationGain(...)` helper | Часть режимов может бросать `NotImplementedException` | Нельзя считать helper универсальным API | TODO implement |
| `CreateAudioSource(...)` | TODO по индексированию буферов | Потенциально хрупкое сопоставление buffer-id | TODO в коде |
| `DisposeAllAudio(...)` | TODO о корректной последовательности stop/dispose | Нужна осторожность при рефакторинге shutdown | TODO в коде |
| `SharedAudioSystem.Effects` core методы | Вся зона старше cutoff | Использовать только как compat-базу, не как «современный стиль» | Основной код 2023 |
| `SetGain/SetVolume/Stop` | Старые строки и историческая логика | Для новых правил использовать только с тестовым подтверждением | 2023 |
| `AmbientSound` sampler-алгоритм | Известно неполное/временное состояние выбора звуков | Не копировать как reference API-подход | TODO в заголовке |
| `Форк-подсистема AudioEffectsManager` с таймерным workaround | Гонка и нестрогий lifecycle control | Не переносить как общий способ attach эффектов | Явный race-condition comment |
| Docs tutorial «bikehorn» | Явно outdated и частично опирается на старые подходы | Только исторический контекст, не API-эталон | outdated marker |

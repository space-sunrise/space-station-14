# Fresh Pattern Catalog (Audio API)

| API | Назначение | Свежесть | Рекомендация | Комментарий |
|---|---|---|---|---|
| `ResolveSound(...)` | Разрешить `SoundSpecifier` в детерминированный `ResolvedSoundSpecifier` | 2025-02-22 | Fresh-Use | База для реплеев и единообразного выбора трека из коллекции |
| `GetAudioPath(...)` | Получить конкретный путь из resolved specifier | 2025-02-22 | Fresh-Use | Удобно для валидации и логов |
| `GetAudioLength(...)` | Длина аудио для seek/timed logic | 2025-02-22 | Fresh-Use | Используй перед `SetPlaybackPosition` |
| `PlayGlobal(...)` (resolved/filter) | Непозиционный звук для аудитории | 2025-02-22 | Fresh-Use | Основной серверный broadcast API |
| `PlayEntity(...)` (resolved/filter, uid) | Позиционный звук от сущности | 2025-02-22 | Fresh-Use | Используй для привязки к движущемуся источнику |
| `PlayStatic(...)` (resolved/filter, coords) | Позиционный звук в координатах | 2025-02-22 | Fresh-Use | Для world-point SFX |
| `PlayPvs(...)` (uid/coords) | Shorthand под PVS | 2025-02-22 | Fresh-Use | Уменьшает boilerplate фильтра |
| `PlayGlobal/PlayEntity/PlayStatic(..., recipient)` | Точечная отправка конкретному игроку | 2025-02-22 | Fresh-Use | Полезно для приватных UI/feedback |
| `PlayPredicted(sound, source, user, ...)` | Предсказанный positional звук от entity | Mixed (client old signature + server old impl) | Mixed-Use | Рабочий контракт, но реализация имеет legacy участки |
| `PlayPredicted(sound, coords, user, ...)` | Предсказанный positional звук от coords | Mixed | Mixed-Use | Использовать, но учитывать legacy-сигналы |
| `PlayLocal(...)` | Локальный predicted alias | 2024-09-12 | Mixed-Use | По сути проксирует в predicted-flow |
| `SetState(...)` | Управление lifecycle потока | 2024-04-17 | Fresh-Use | Правильный способ pause/resume/stop |
| `SetPlaybackPosition(...)` | Seek c таймлайном и pause-offset | 2024-04-17 | Fresh-Use | Рабочий runtime control |
| `SetMapAudio(...)` | Map-wide режим источника | 2024-05-01 | Fresh-Use | Для музыки/таймеров уровня карты |
| `SetGridAudio(...)` | Grid-centered режим источника | 2024-05-29 | Fresh-Use | Для крупных сеток и FTL-звуков |
| `SetGain(...)` | Линейный gain-контроль | 2023-11-28 | Legacy-Compat | Использовать осторожно, зона старая |
| `SetVolume(...)` | dB-контроль громкости | 2023-11-28 | Legacy-Compat | Приоритетно через стабильные setter API, не вручную |
| `Stop(...)` | Остановка и удаление audio entity | 2023-11-27 | Legacy-Compat | Базовый API, но исторически старый |
| `IsPlaying(...)` | Проверка активного state | 2024-04-17 | Fresh-Use | Удобно для stateful gameplay |
| `PlayGlobal/PlayEntity/PlayStatic(AudioStream, ...)` | Клиентский stream playback | 2025-02-22 | Fresh-Use | Включает свежие исправления positional-start |
| `LoadStream<T>(...)` | Подгрузка stream в уже созданную audio-сущность | Mixed | Mixed-Use | Клиентский путь рабочий, серверный путь помечен TODO |
| `CreateEffect()/CreateAuxiliary()` | Создание EFX сущностей | Shared old / OpenAL runtime fresh | Mixed-Use | API стабилен, но shared-слой старый |
| `SetEffectPreset(...)` | Применение reverb preset в effect component | 2023-11-27 | Legacy-Compat | Работает, но как зона старого shared-кода |
| `SetEffect(...)` + `SetAuxiliary(...)` | Сборка цепочки `effect -> aux -> source` | 2023-11-27 / 2025-08-18 runtime | Mixed-Use | Использовать вместе с проверкой EFX |
| `ProcessStreamOverride` | Полная замена stream processing | 2025-11-30 | Fresh-Use | Только один подписчик |
| `GetOcclusionOverride` | Кастомная окклюзия | 2025-11-30 | Fresh-Use | Только один подписчик |
| `TryAudioLimit/RemoveAudioLimit` | Ограничение конкурентных одинаковых звуков | 2024-03-14 | Fresh-Use | Важный anti-spam механизм |

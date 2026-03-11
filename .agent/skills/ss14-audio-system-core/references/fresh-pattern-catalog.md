# Fresh Pattern Catalog (Audio Core)

| Класс/метод | Паттерн | Почему полезно | Слой | Дата по blame | Статус |
|---|---|---|---|---|---|
| `SharedAudioSystem.SetPlaybackPosition(...)` | Сдвиг времени с учетом pause/start и despawn lifetime | Корректный seek без рассинхрона тайминга | Shared | 2024-04-17 | Использовать |
| `SharedAudioSystem.SetMapAudio(...)` | Пометка map-звука как global + undetachable | Надежный map-wide звук | Shared | 2024-05-01 | Использовать |
| `SharedAudioSystem.SetGridAudio(...)` | Автонастройка grid-position/range + `NoOcclusion` | Стабильный звук для больших сеток | Shared | 2024-05-29 | Использовать |
| `SharedAudioSystem.SetState(...)` | Явная state-машина `Playing/Paused/Stopped` | Безопасное управление циклом звука | Shared | 2024-04-17 | Использовать |
| `SharedAudioSystem.SetupAudio(...)` | Единая фабрика audio-entity + timed despawn + variation | Предсказуемый жизненный цикл | Shared | 2025-02-22 | Использовать |
| `SharedAudioSystem.ResolveSound(...)` + `GetAudioPath(...)` | Детеминированный resolve path/collection | Одинаковое воспроизведение между клиентами | Shared | 2025-02-22 | Использовать |
| `AudioSystem.OnAudioState(...)` | Переприменение параметров и seek-логика после state update | Клиентский источник держится в sync | Client | 2025-02-26 | Использовать |
| `AudioSystem.SetupSource(...)` | Offset-aware start, end-buffer guard, первичная инициализация source | Меньше клипов и ложных стартов | Client | 2024-03-16 | Использовать |
| `AudioSystem.ProcessStreamOverride` | Single-target hook для полной замены stream processing | Расширяемость без форка ядра | Client | 2025-11-30 | Использовать |
| `AudioSystem.GetOcclusionOverride` | Single-target hook для custom occlusion | Кастомная акустика карт/режимов | Client | 2025-11-30 | Использовать |
| `AudioSystem.PlayEntity(AudioStream...)` + immediate `ProcessStream(...)` | Немедленный spatial update после старта stream | Убирает «кривой первый тик» positional-аудио | Client | 2025-09-02 | Использовать |
| `AudioSystem.PlayStatic(AudioStream...)` + immediate `ProcessStream(...)` | То же для static stream | Стабильный старт статичных потоков | Client | 2025-09-02 | Использовать |
| `AudioSystem.Limits.TryAudioLimit/RemoveAudioLimit` | Лимит concurrent по ключу звука | Защита от source-budget starvation | Client | 2024-03-14 | Использовать |
| `BaseAudioSource` NaN gain guard | Явная защита от NaN в gain | Предотвращает взрыв громкости/ошибки AL | Client/OpenAL | 2025-02-26 | Использовать |
| `BaseAudioSource.SetAuxiliary(...)` + `SetOcclusionEfx(...)` | EFX send + lowpass для окклюзии | Реальная фильтрация, не только громкость | Client/OpenAL | 2025-08-18 | Использовать |
| `AudioEffect` (`EaxReverb*` bridge) | Полный маппинг reverb-полей в EFX | Тонкая настройка пространства звучания | Client/OpenAL | 2025-08-18 | Использовать |
| `AuxiliaryAudio.SetEffect(...)` | Привязка/снятие effect на slot | Чистое управление EFX-цепочкой | Client/OpenAL | 2025-08-18 | Использовать |
| `AudioManager` reload hooks (`/Audio`, `*.ogg/*.wav`) | Горячая перезагрузка аудио-ресурсов | Быстрее итерации и тесты | Client/OpenAL | 2025-03-08 | Использовать |
| `AudioManager` extension parsing | Нормальная регистрация ALC/AL extensions | Корректный feature-detection | Client/OpenAL | 2025-06-21 | Использовать |
| `Server AudioSystem.SetMapAudio/SetGridAudio` + global override | PVS override для map/grid special-аудио | Звук слышат нужные клиенты независимо от позиции | Server | 2024-05-01 / 2024-05-29 | Использовать |
| `Shuttle FTL` (`SetGridAudio`, `SetPlaybackPosition`) | Grid-аудио в фазах FTL + клип-continue на переходе | Плавный переходный саунд | Server/Gameplay | 2024-05-29 | Использовать |
| `Salvage countdown` (`SetMapAudio`) | Музыка эвакуации как map-аудио | Гарантированное map-wide оповещение | Server/Gameplay | 2024-05-06 | Использовать |
| `Jukebox` ping-compensated `SetPlaybackPosition` | Компенсация сетевой задержки в seek | Лучшая синхронизация для игроков | Server/Gameplay | 2024-04-17 | Использовать |
| `ServerGlobalSoundSystem.PlayGlobalOnStation(...)` | Явная станционная аудитория + PVS | Контролируемый global broadcast | Server/Gameplay | 2025-02-23 | Использовать |
| `SharedGasValveSystem.OnActivate(...)` + `PlayPredicted(...WithVariation)` | Предсказанный интеракционный SFX с вариацией | Отзывчивый UX и меньше «двойных» звуков | Shared/Gameplay | 2025-05-14 | Использовать |

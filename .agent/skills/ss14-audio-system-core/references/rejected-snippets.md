# Rejected Snippets (Audio Core)

| Зона | Что найдено | Почему не брать как эталон | Сигнал |
|---|---|---|---|
| `SharedAudioSystem.SetGain/SetVolume/Stop` | Базовые runtime-операции старой даты | Работают, но не использовать как «свежий стиль» без доп. проверки | Основные строки 2023 |
| `SharedAudioSystem.Effects` (`CreateAuxiliary/CreateEffect/SetAuxiliary/SetEffect`) | Базовая shared-реализация эффектов | Логика старая; свежий EFX-контекст живет в client OpenAL слое | База 2023 |
| `SharedAudioSystem.CalculateAudioPosition` | Прямой TODO про clamp в другом слое | Есть незакрытый дизайн-долг в точке позиционирования | TODO (2026-01-19) |
| `SharedAudioSystem.AudioMessage` | TODO про bandwidth-heavy сериализацию | Не строить новый сетевой формат аудио поверх текущего message-shape | TODO про bandwidth |
| `Server AudioSystem.PlayEntity` (filter branch) | TODO про custom range/PVS override | Прямая зона техдолга вокруг фильтрации дальности | TODO AUDIO |
| `Server AudioSystem.PlayPvs(coordinates)` | TODO про transform mess/optimization | Не использовать как reference для новой пространственной архитектуры | TODO Transform |
| `Server AudioSystem.LoadStream<T>` | Пустой TODO-метод | Не считать серверный stream-loading рабочим API | TODO remove |
| `Client AudioSystem.ProcessStream` | Комментарий про «replicate old behaviour for now» | Есть явный маркер временного решения | TODO в методе |
| `AudioManager.GetAttenuationGain` | Не все модели реализованы, бросает `NotImplementedException` | Нельзя полагаться на универсальность helper-а | TODO implement |
| `AudioManager.CreateAudioSource` | TODO про индексирование по ClydeHandle | Уязвимая точка индексации/сопоставления | TODO в комментарии |
| `AudioManager.DisposeAllAudio` | TODO про необходимость stop-прохода | Поведение shutdown стоит перепроверять при изменениях | TODO в комментарии |
| `AmbientSoundSystem` (client sampler) | TODO про неполный nearest-sound алгоритм | Не использовать как шаблон точного приоритезационного микширования | TODO в заголовке файла |
| `InteractionPopupSystem.SharedInteract` | TODO про attempt-event/paused accumulator + проблемный комментарий | Код рабочий, но не чистый baseline для новой архитектуры интеракций | TODO + suspicious comment |
| `AudioEffectsManagerSystem.TryAddEffect` | Таймер как race-condition workaround | Хрупкий подход; не переносить как общую практику | Явный комментарий о гонке |
| `Weather` старые участки поиска ближайшей «зоны погоды» | Части spatial-логики заметно старее свежих gain-правок | Брать только свежие куски регулировки громкости, не весь алгоритм целиком | Смешанный возраст строк |
| Docs: «bikehorn tutorial» | Страница явно помечена как outdated | Полезна только как исторический onboarding-контекст | `outdated` template marker |

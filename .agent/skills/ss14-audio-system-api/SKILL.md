---
name: ss14-audio-system-api
description: Дает практический каталог API AudioSystem в Space Station 14: когда использовать Play/Set/Stop/Resolve методы, как работать с predicted-аудио и фильтрами, и как применять OpenAL EFX (auxiliary/effect/preset) без типичных ошибок.
---

# AudioSystem: API-практика

Используй этот skill, когда нужно быстро выбрать корректный метод AudioSystem и применить его без регрессий 🙂
Ориентир свежести: `git blame` с cutoff `2024-02-19`.

## Что загружать

1. `references/fresh-pattern-catalog.md` — API-реестр с рекомендациями.
2. `references/rejected-snippets.md` — legacy/TODO/опасные сценарии.
3. `references/docs-context.md` — как безопасно использовать docs.

## Быстрый выбор API

1. Нужен звук «для всех вокруг источника»: `PlayPvs(sound, uid/coords, params)`.
2. Нужен звук «для конкретного получателя»: `PlayEntity/PlayGlobal/PlayStatic(..., recipient, ...)`.
3. Нужен predicted UX без дублей: `PlayPredicted(sound, source/coords, user, params)`.
4. Нужен runtime-контроль потока: `SetState`, `SetPlaybackPosition`, `Stop`, `IsPlaying`.
5. Нужен map/grid special sound: `SetMapAudio`, `SetGridAudio`.
6. Нужен эффект реверба/фильтра: `CreateEffect + CreateAuxiliary + SetEffectPreset + SetEffect + SetAuxiliary`.
7. Нужна настройка громкости/тона/дальности: `AudioParams.With*` и/или `SetVolume/SetGain`.

## API по группам

### 1) Resolve и утилиты

1. `ResolveSound(SoundSpecifier)` — закрепляет конкретный трек из collection.
2. `GetAudioPath(ResolvedSoundSpecifier?)` — получает реальный путь.
3. `GetAudioLength(...)` — длительность для seek/таймеров.
4. `GetAudioDistance(length)` — учитывает Z-offset.
5. `GainToVolume/VolumeToGain` — перевод между gain и dB.

### 2) Playback API (основа)

1. `PlayGlobal(...)` — непозиционный звук.
2. `PlayEntity(...)` — звук следует за entity.
3. `PlayStatic(...)` — звук в фиксированных координатах.
4. `PlayPvs(...)` — shorthand для PVS-аудитории.
5. Все группы имеют overload-ы под:
`SoundSpecifier`/`ResolvedSoundSpecifier`, `Filter`, `ICommonSession`, `EntityUid recipient`.

### 3) Predicted API

1. `PlayPredicted(sound, source, user, params)`.
2. `PlayPredicted(sound, coords, user, params)`.
3. `PlayLocal(...)` — локальный predicted-alias.

### 4) Контроль уже играющего потока

1. `SetState(stream, Playing/Paused/Stopped)`.
2. `SetPlaybackPosition(stream, seconds)`.
3. `SetVolume(stream, dB)` / `SetGain(stream, gain)`.
4. `Stop(stream)`.
5. `IsPlaying(stream)`.

### 5) Spatial-режимы карты/сетки

1. `SetMapAudio(audioEntity)` — map-wide звучание.
2. `SetGridAudio(audioEntity)` — grid-центр, tuned distance, `NoOcclusion`.

### 6) Stream API (client runtime)

1. `PlayGlobal(AudioStream, ...)`.
2. `PlayEntity(AudioStream, ...)`.
3. `PlayStatic(AudioStream, ...)`.
4. `LoadStream<T>(entity, stream)` — загрузка source из stream-type.

### 7) API эффектов OpenAL (EFX)

1. `CreateEffect()` — создает effect entity.
2. `CreateAuxiliary()` — создает auxiliary slot entity.
3. `SetEffectPreset(effect, presetProtoOrReverb)` — применяет набор параметров.
4. `SetEffect(aux, effect)` — цепляет effect к slot.
5. `SetAuxiliary(audio, aux)` — цепляет slot к источнику.
6. Shortcut: `SetEffect(audioUid, audioComp, presetId)`.

## Паттерны

1. Для интеракций игрока всегда передавай `user` и используй `PlayPredicted`.
2. Для long music/stateful SFX храни stream `EntityUid?` и управляй через `SetState/Stop`.
3. Для шатающихся/больших объектов (шаттлы, платформы) делай `PlayPvs -> SetGridAudio`.
4. Для map-событий (таймеры, глобальные фазы) делай `PlayPvs -> SetMapAudio`.
5. Для rich-acoustics сначала собери EFX-цепочку, потом подключай к активному stream.
6. Для разовых звуков с вариацией используй `AudioParams.WithVariation(...)`.
7. Для «тише/громче в рантайме» предпочитай `SetVolume`; для линейных коэффициентов — `SetGain`.

## Анти-паттерны

1. Дергать `PlayPvs` в predicted-коде там, где нужен `PlayPredicted`.
2. Использовать старые TODO-зоны как шаблон нового API.
3. Полагаться на server `LoadStream<T>` как на рабочий контракт.
4. Навешивать эффекты до готовности реального source без учета жизненного цикла.
5. Миксовать map/grid semantics вручную вместо `SetMapAudio/SetGridAudio`.
6. Игнорировать состояние `AudioState` и пытаться «лечить» поток только удалением сущности.

## OpenAL эффекты: практический минимум

1. Reverb параметры живут в `AudioEffectComponent` (`Density`, `Decay*`, `Echo*`, `Modulation*`, `HF/LF*`).
2. `AudioAuxiliaryComponent` держит link на effect.
3. `AudioComponent.Auxiliary` подключает slot к конкретному источнику.
4. Окклюзия в EFX-режиме использует lowpass; без EFX деградирует в gain-fallback.

## Примеры из кода

### 1) Predicted звук от интеракции

```csharp
// Пользователь уже слышит локально; сервер исключит его из повторного события.
_audio.PlayPredicted(ent.Comp.UseSound, ent.Owner, args.User, AudioParams.Default.WithVariation(0.25f));
```

### 2) Станционная музыка/ивент только нужной аудитории

```csharp
// Сервер сам формирует фильтр станции + PVS и рассылает network event.
var spec = _audio.ResolveSound(sound);
_globalSound.PlayGlobalOnStation(sourceUid, spec, AudioParams.Default.WithVolume(-8f));
```

### 3) Ping-aware seek для синхронного воспроизведения

```csharp
// Компенсация сетевой задержки при перемотке текущего трека.
var offset = session.Channel.Ping * 1.5f / 1000f;
Audio.SetPlaybackPosition(streamUid, requestedTime + offset);
```

### 4) Grid-аудио для FTL-фазы

```csharp
// Шаттл-саунд привязываем к grid-семантике, а не к локальной точке.
var audio = _audio.PlayPvs(startupSound, shuttleUid);
_audio.SetGridAudio(audio);
```

### 5) Полная EFX-цепочка

```csharp
// Создали effect + slot, применили preset и подключили к конкретному аудио.
var effect = _audio.CreateEffect();
var aux = _audio.CreateAuxiliary();

_audio.SetEffectPreset(effect.Entity, effect.Component, presetPrototype);
_audio.SetEffect(aux.Entity, aux.Component, effect.Entity);
_audio.SetAuxiliary(soundUid, soundComp, aux.Entity);
```

### 6) Управление состоянием потока

```csharp
// Пауза/возобновление/стоп — через state API.
Audio.SetState(streamUid, AudioState.Paused);
Audio.SetState(streamUid, AudioState.Playing);
Audio.SetState(streamUid, AudioState.Stopped);
```

## Правило применения

1. По умолчанию выбирай `Fresh-Use` методы из reference-каталога.
2. `Legacy-Compat` используй только при явной необходимости и с тестами.
3. `Risk/TODO` не используй как опорную точку для нового API-дизайна.
4. Любой API-рефакторинг аудио проверяй на prediction, PVS и EFX fallback одновременно.

Держи API слой предсказуемым: правильный overload и правильный recipient важнее, чем «короткий» вызов 😅

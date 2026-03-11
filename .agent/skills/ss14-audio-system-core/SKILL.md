---
name: ss14-audio-system-core
description: Объясняет архитектуру AudioSystem в Space Station 14 на уровнях shared/server/client и OpenAL: жизненный цикл аудио-сущности, фильтрация через PVS, окклюзия, стриминг и EFX-цепочка. Используй, когда нужно понять внутреннюю схему системы перед расширением или отладкой.
---

# AudioSystem: Архитектура и Внутренности

Используй этот skill как архитектурный playbook по AudioSystem 🙂
Держи фокус на свежем коде и проверяй актуальность через `git blame` (cutoff: `2024-02-19`).

## Что загружать в первую очередь

1. `references/fresh-pattern-catalog.md` — подтвержденные свежие паттерны ✅
2. `references/rejected-snippets.md` — старые и рискованные зоны, которые нельзя копировать ⚠️
3. `references/docs-context.md` — что брать из docs и где docs могут устаревать

## Источник правды

1. Кодовая база — основной источник истины.
2. Документация — вторичная: для терминов, intent и проверки гипотез.
3. Любой участок старше двух лет или с явными TODO/проблемными комментариями — не поднимать в правила как «эталон».

## Ментальная модель AudioSystem

1. `SharedAudioSystem` задает контракт: создание/остановка, состояние, позиционирование, маршрутизация по аудитории.
2. `AudioComponent` — сетевое состояние потока звука (state + params + фильтрация + привязка к auxiliary).
3. Сервер создает аудио-сущности и решает, кому их реплицировать (filter/PVS/override), но не воспроизводит реальный звук.
4. Клиент создает реальный `IAudioSource`, применяет `AudioParams`, обновляет позицию/окклюзию и управляет playback.
5. Для positional-звука ключевой цикл: вычислить позицию -> проверить дистанцию -> оценить окклюзию -> обновить gain/velocity.
6. Эффекты идут отдельной цепочкой: `AudioSource -> Auxiliary Slot -> AudioEffect (EAX Reverb)`.
7. При включенном EFX окклюзия работает через lowpass-фильтр; без EFX — через fallback по gain.
8. Для стримов важен «мгновенный процессинг после старта», чтобы звук не жил один тик с неверной позицией/окклюзией.

## Схема слоев

1. Shared слой:
`ResolveSound`, `SetupAudio`, `SetState`, `SetPlaybackPosition`, `SetMapAudio`, `SetGridAudio`, `Stop`.
2. Server слой:
выбор recipients, `PlayPvs/PlayEntity/PlayStatic/PlayGlobal`, PVS override для map/grid audio.
3. Client слой:
`FrameUpdate`, `ProcessStream`, `GetOcclusion`, audio-limit, stream playback.
4. OpenAL слой:
`AudioManager`, `BaseAudioSource`, `AudioEffect`, `AuxiliaryAudio`.

## Паттерны

1. Для локально-предсказанных интеракций используй `PlayPredicted(..., user)` вместо «чистого» `PlayPvs`.
2. Для map/grid-спецзвуков применяй `SetMapAudio`/`SetGridAudio`, а не ручную эмуляцию через random filters.
3. Для стримов, запущенных посреди тика, сразу пересчитывай spatial-состояние (позиция/окклюзия/velocity).
4. Для сложной кастомной акустики подключай `ProcessStreamOverride`/`GetOcclusionOverride` с одним подписчиком.
5. Для OpenAL-эффектов собирай связку: `CreateEffect -> CreateAuxiliary -> SetEffect -> SetAuxiliary`.
6. Для повторяющихся коротких SFX учитывай limit concurrent sound, иначе можно «съесть» source budget.
7. Для long-running музыки/ивент-звука храни stream `EntityUid?` и управляй состоянием через `SetState`/`Stop`.
8. Для глобальной станционной/карточной музыки формируй аудиторию через серверный фильтр, а не на клиенте.

## Анти-паттерны

1. Опираться на участки `LoadStream` в server-реализации как на рабочий API-путь.
2. Копировать старые зоны с TODO про range/PVS/Transform как шаблон новой логики.
3. Навешивать эффект через race-таймер без гарантий жизненного цикла source (хрупкая гонка 😬).
4. Мутировать громкость/gain в tight loop без guard-проверок и без понимания EFX fallback поведения.
5. Использовать одну и ту же реализацию для predicted и non-predicted кейсов без explicit user.
6. Считать docs «истиной поведения» там, где код уже ушел вперед.

## OpenAL/EFX: что важно понимать

1. `AudioManager` открывает девайс/контекст OpenAL и определяет поддержку `ALC_EXT_EFX`.
2. `AudioSource` управляет источником AL (`Gain`, `Pitch`, `Position`, `Velocity`, `SecOffset`).
3. `AuxiliaryAudio` — effect slot, в который цепляется `AudioEffect`.
4. `AudioEffect` маппит reverb-поля (`Density`, `Decay*`, `Echo*`, `Modulation*`, `HF/LF references`) в EFX параметры.
5. При EFX-окклюзии используется lowpass (`LowpassGain`, `LowpassGainHF`), иначе gain-фоллбек.

## Примеры из кода

### 1) Стрим + немедленный spatial-пересчет

```csharp
// После старта stream в середине тика сразу пересчитываем spatial,
// иначе один тик можно услышать звук в неверной позиции.
var playing = CreateAndStartPlayingStream(audioParams, specifier, stream);
SetCoordinates(playing.Entity, new EntityCoordinates(sourceEntity, Vector2.Zero));

ProcessStream(
    playing.Entity,
    playing.Component,
    Transform(playing.Entity),
    GetListenerCoordinates());
```

### 2) Grid-аудио для больших подвижных объектов

```csharp
// Помечаем звук как grid-audio: центр по сетке + расширенная дальность + без окклюзии.
var stream = PlayPvs(startupSound, shuttleUid);
SetGridAudio(stream);
```

### 3) Ping-компенсация позиции воспроизведения

```csharp
// Для синхрона музыкального потока учитываем сетевую задержку сессии.
var offset = session.Channel.Ping * 1.5f / 1000f;
SetPlaybackPosition(jukeboxStream, requestedSongTime + offset);
```

### 4) EFX-цепочка эффекта реверберации

```csharp
// Создаем effect + auxiliary, применяем preset и подключаем к источнику.
var effect = CreateEffect();
var aux = CreateAuxiliary();

SetEffectPreset(effect.Entity, effect.Component, presetProto);
SetEffect(aux.Entity, aux.Component, effect.Entity);
SetAuxiliary(audioUid, audioComp, aux.Entity);
```

### 5) Серверный predicted-flow без дублей для инициатора

```csharp
// Сервер шлет звук по PVS, но исключает инициатора,
// потому что инициатор уже предсказал локально.
var audio = PlayPvs(ResolveSound(sound), sourceUid, audioParams);
if (audio != null)
    audio.Value.Component.ExcludedEntity = user;
```

## Правило расширения

1. Новую механику сначала строй на свежих точках входа (`stream overloads`, `SetGridAudio/SetMapAudio`, override hooks).
2. Если вынужден трогать legacy-зону, сначала зафиксируй риск в `references/rejected-snippets.md`.
3. Любую аудио-оптимизацию подтверждай профилированием и прослушиванием в реальном раунде.
4. Для эффектов сначала проверяй поддержку EFX, потом проектируй fallback-поведение.

Думай об AudioSystem как о сетевом контракте + клиентском DSP-пайплайне, а не как о «проиграть файл» 🎧

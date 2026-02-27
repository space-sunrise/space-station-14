---
name: SS14 Graphics AnimationPlayer
description: Глубокий практический гайд по анимациям сущностей через AnimationPlayerSystem в SS14: lifecycle, API, типы треков, keyframes/interpolation/easing, события завершения, паттерны и анти-паттерны для production-кода.
---

# Анимации через AnimationPlayer в SS14

Этот skill покрывает только анимации сущностей через `AnimationPlayerSystem` 🙂
UI-анимации (`Control.PlayAnimation`) и shader/overlay-эффекты веди в отдельных skill.

## Когда использовать

Выбирай `AnimationPlayerSystem`, когда нужно:

- плавно менять свойства компонентов во времени (`SpriteComponent`, `TransformComponent`, `PointLightComponent` и т.д.);
- запускать flick-анимации RSI-состояний по слоям;
- синхронизировать визуал и звук в одном таймлайне;
- обрабатывать завершение/прерывание анимации через события.

Не используй его для статических визуальных состояний без времени: это зона обычного `SpriteSystem`/`GenericVisualizer`.

## Ментальная модель

1. Ты создаешь `Animation` (`Length` + список `AnimationTracks`).
2. Играешь ее через `AnimationPlayerSystem.Play(...)` под уникальным `key`.
3. Система каждый кадр двигает playback и применяет значения треков.
4. По окончании приходит `AnimationCompletedEvent` (`Finished = true`).
5. При ручной остановке `Stop(...)` тоже приходит `AnimationCompletedEvent`, но `Finished = false`.

Идея: анимация — это локальный timeline эффекта, а не сетевая бизнес-логика 🙂

## Важная граница клиент/сервер

- `AnimationPlayerComponent` — клиентская сущность визуала.
- На сервере этот компонент игнорируется фабрикой компонентов.
- Сервер сообщает состояние, клиент решает, когда и как визуально анимировать.

## API-разбор `AnimationPlayerSystem`

### Запуск

- `Play(EntityUid uid, Animation animation, string key)`
- `Play(Entity<AnimationPlayerComponent> ent, Animation animation, string key)`

Практика:

- используй стабильные константы ключей;
- перед `Play` проверяй `HasRunningAnimation`, если ключ может повториться.

### Проверка запущенных

- `HasRunningAnimation(EntityUid uid, string key)`
- `HasRunningAnimation(EntityUid uid, AnimationPlayerComponent? component, string key)`
- `HasRunningAnimation(AnimationPlayerComponent component, string key)`

### Остановка

- `Stop(Entity<AnimationPlayerComponent?> entity, string key)`
- `Stop(EntityUid uid, AnimationPlayerComponent? component, string key)`

Остановка триггерит completion-событие с `Finished = false`.

### События

- `AnimationStartedEvent`
- `AnimationCompletedEvent`
  - `Key`
  - `Finished` (естественное завершение или принудительное)

## Структура `Animation`

- `Length`: общая длина playback.
- `AnimationTracks`: несколько треков идут синхронно.

Правило: `Length` должен покрывать все нужные ключевые фазы треков.

## Типы треков и когда какой брать

### `AnimationTrackComponentProperty`

Меняет свойство компонента по keyframes.

Нужны поля:

- `ComponentType = typeof(...)`
- `Property = nameof(...)`
- `KeyFrames`
- опционально `InterpolationMode`

Используй для:

- `SpriteComponent.Scale/Offset/Color/Rotation`;
- `TransformComponent.LocalPosition`;
- `PointLightComponent.AnimatedRadius/AnimatedEnable/Rotation` и т.д.

### `AnimationTrackSpriteFlick`

Управляет RSI-state слоя во времени.

Нужны поля:

- `LayerKey` (обычно enum слоя);
- `KeyFrames` со `StateId`.

Используй, когда нужно проигрывать именно state-анимацию слоя.

### `AnimationTrackPlaySound`

Проигрывает звук на заданных keyframes.

Используй для точной синхронизации "визуал + звук" в одном timeline.

## KeyFrame и интерполяция

`AnimationTrackProperty.KeyFrame(value, keyTime, easing?)`:

- `keyTime` — это **дельта** от предыдущего keyframe, а не абсолютное время.
- `easing` применяется к переходу между предыдущим и текущим кадром.

`AnimationInterpolationMode`:

- `Linear`
- `Cubic`
- `Nearest`
- `Previous`

Для неподдерживаемых типов интерполяция фактически ведет себя как дискретный шаг (предыдущее значение).

## Практические примеры

### Пример 1: безопасный запуск по ключу

```csharp
private const string AnimKey = "rotating_light";

private void TryPlayRotation(EntityUid uid, AnimationPlayerComponent player, Animation anim)
{
    // Не допускаем конфликт ключей в PlayingAnimations.
    if (_anim.HasRunningAnimation(uid, player, AnimKey))
        return;

    _anim.Play((uid, player), anim, AnimKey);
}
```

### Пример 2: корректная остановка + cleanup

```csharp
private void StopFallAnimation(EntityUid uid, AnimationPlayerComponent player, SpriteComponent sprite, Vector2 originalScale)
{
    // Сначала возвращаем визуальные параметры в базовое состояние.
    _sprite.SetScale((uid, sprite), originalScale);

    // Потом останавливаем анимацию по ключу.
    _anim.Stop((uid, player), "chasm_fall");
}
```

### Пример 3: property-track с easing

```csharp
private static Animation BuildPickupAnim(Vector2 from, Vector2 to, Color startColor)
{
    return new Animation
    {
        Length = TimeSpan.FromMilliseconds(175),
        AnimationTracks =
        {
            new AnimationTrackComponentProperty
            {
                ComponentType = typeof(TransformComponent),
                Property = nameof(TransformComponent.LocalPosition),
                InterpolationMode = AnimationInterpolationMode.Linear,
                KeyFrames =
                {
                    // keyTime — дельта от предыдущего keyframe.
                    new AnimationTrackProperty.KeyFrame(from, 0f),
                    new AnimationTrackProperty.KeyFrame(to, 0.175f, Easings.OutQuad)
                }
            },
            new AnimationTrackComponentProperty
            {
                ComponentType = typeof(SpriteComponent),
                Property = nameof(SpriteComponent.Color),
                KeyFrames =
                {
                    new AnimationTrackProperty.KeyFrame(startColor, 0f),
                    new AnimationTrackProperty.KeyFrame(startColor.WithAlpha(0f), 0.175f, Easings.OutQuad)
                }
            }
        }
    };
}
```

### Пример 4: комбинированный трек (flick + свет)

```csharp
private static readonly Animation ProximityAnim = new()
{
    Length = TimeSpan.FromSeconds(0.6f),
    AnimationTracks =
    {
        new AnimationTrackSpriteFlick
        {
            LayerKey = ProximityTriggerVisualLayers.Base,
            KeyFrames = { new AnimationTrackSpriteFlick.KeyFrame("flashing", 0f) }
        },
        new AnimationTrackComponentProperty
        {
            ComponentType = typeof(PointLightComponent),
            Property = nameof(PointLightComponent.AnimatedRadius),
            InterpolationMode = AnimationInterpolationMode.Nearest,
            KeyFrames =
            {
                new AnimationTrackProperty.KeyFrame(0.1f, 0f),
                new AnimationTrackProperty.KeyFrame(3f, 0.1f),
                new AnimationTrackProperty.KeyFrame(0.1f, 0.5f)
            }
        }
    }
};
```

### Пример 5: ручной loop через `AnimationCompletedEvent`

```csharp
private void OnAnimationCompleted(EntityUid uid, RotatingLightComponent comp, AnimationCompletedEvent args)
{
    if (args.Key != "rotating_light")
        return;

    // Ре-лупаем только при естественном завершении.
    if (!args.Finished)
        return;

    if (!TryComp<AnimationPlayerComponent>(uid, out var player))
        return;

    _anim.Play((uid, player), BuildRotation(comp.Speed), "rotating_light");
}
```

### Пример 6: flick с бесконечной длиной + звук

```csharp
private Animation BuildPrimingAnimation(ResolvedSoundSpecifier? sound)
{
    var anim = new Animation
    {
        Length = TimeSpan.MaxValue,
        AnimationTracks =
        {
            new AnimationTrackSpriteFlick
            {
                LayerKey = TriggerVisualLayers.Base,
                KeyFrames = { new AnimationTrackSpriteFlick.KeyFrame("primed", 0f) }
            }
        }
    };

    if (sound != null)
    {
        anim.AnimationTracks.Add(new AnimationTrackPlaySound
        {
            KeyFrames = { new AnimationTrackPlaySound.KeyFrame(sound.Value, 0f) }
        });
    }

    return anim;
}
```

### Пример 7: анимация цвета без дублирования компонента

```csharp
private Animation BuildColorFlash(Color from, Color to, float seconds)
{
    return new Animation
    {
        Length = TimeSpan.FromSeconds(seconds),
        AnimationTracks =
        {
            new AnimationTrackComponentProperty
            {
                ComponentType = typeof(SpriteComponent),
                Property = nameof(SpriteComponent.Color),
                InterpolationMode = AnimationInterpolationMode.Linear,
                KeyFrames =
                {
                    new AnimationTrackProperty.KeyFrame(from, 0f),
                    new AnimationTrackProperty.KeyFrame(to, seconds)
                }
            }
        }
    };
}
```

### Пример 8: связка с appearance без спама

```csharp
private void UpdateTriggeredState(EntityUid uid, AnimationPlayerComponent player, ProximityTriggerVisuals state)
{
    switch (state)
    {
        case ProximityTriggerVisuals.Active:
            if (!_anim.HasRunningAnimation(uid, player, "proximity"))
                _anim.Play((uid, player), ProximityAnim, "proximity");
            break;
        case ProximityTriggerVisuals.Inactive:
        case ProximityTriggerVisuals.Off:
            _anim.Stop((uid, player), "proximity");
            break;
    }
}
```

## Паттерны 🙂

- Храни ключи анимаций как константы рядом с системой.
- Перед `Play` проверяй `HasRunningAnimation`, если ключ может повториться.
- Для повторов предпочитай `AnimationCompletedEvent` + `args.Finished`.
- На `ComponentShutdown/Remove` возвращай исходные визуальные значения (scale/offset/color).
- Комбинируй несколько треков в одном `Animation`, если им нужен общий таймлайн.
- Используй `nameof(...)` для `Property`, чтобы не ломаться при рефакторинге.

## Анти-паттерны ❌

- Играть `Play` повторно с тем же ключом без проверки (риск исключения по дублирующему ключу).
- Забывать cleanup после прерванной анимации.
- Пытаться анимировать свойства, которые не являются анимируемыми.
- Вкладывать бизнес-логику сервера в клиентскую animation-систему.
- Пересоздавать тяжелые анимации каждый кадр без необходимости.
- Путать entity-animation (`AnimationPlayerSystem`) и UI-animation (`Control.PlayAnimation`).

## Чеклист перед изменением ✅

- Есть ли стабильный `AnimationKey`?
- Есть ли guard на повторный `Play`?
- Проверен ли `Finished` в `AnimationCompletedEvent`?
- Возвращаются ли исходные визуальные параметры при shutdown/stop?
- `Length` действительно покрывает все переходы keyframes?
- Для `AnimationTrackComponentProperty` указан корректный `ComponentType` + `Property`?

## Типовые ошибки

- `keyTime` воспринимают как абсолютный timestamp, а не дельту.
- Анимацию останавливают, но забывают вернуть исходный `SpriteComponent` state.
- Используют `string`-литералы вместо `nameof(...)` и ломают анимацию при переименовании.
- Слушают `AnimationCompletedEvent`, но не фильтруют `args.Key`.
- Запускают анимацию на сущности без нужного компонента.
- Не различают `Finished = true` и `Finished = false`, из-за чего цикл запускается не там, где должен ⚠

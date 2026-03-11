---
name: SS14 Graphics SpriteSystem
description: Глубокий практический гайд по SpriteSystem в Space Station 14: lifecycle, полный API по группам методов, работа со слоями и layer-map, практические паттерны и анти-паттерны. Используй при разработке динамических спрайтов, visualizer-систем и рефакторинге устаревших SpriteComponent-вызовов.
---

# SpriteSystem в SS14

Этот skill покрывает только `SpriteSystem` и практику его применения в актуальной архитектуре SS14 🙂
Связанные темы (`GenericVisualizer`, `Appearance`, overlays, shaders, UI) веди в отдельных skill.

## Когда использовать

Используй `SpriteSystem`, когда тебе нужно:

- менять внешний вид сущности в рантайме на клиенте;
- управлять слоями, их видимостью, цветом, смещением, RSI/texture;
- динамически добавлять/удалять слои;
- использовать layer map для стабильной адресации слоев по ключу;
- делать точные визуальные апдейты из visualizer-систем.

Не тащи сюда логику, которую можно сделать только прототипом (статический `Sprite` без динамики), и не смешивай `SpriteSystem` с сетевой бизнес-логикой.

## Источник истины

- Устаревшие примеры на прямых вызовах `SpriteComponent` считай историческими.

## Ментальная модель SpriteSystem

1. Сервер и shared-слой решают состояние геймплея.
2. Клиент получает данные состояния (часто через appearance/visualizer).
3. Клиентская система вызывает `SpriteSystem` и меняет только визуальное представление.
4. Спрайт рендерится из набора слоев (`Layer`) с их параметрами.
5. Layer map дает стабильные ключи (`Enum`/`string`) вместо "магических" индексов.

Идея: визуал меняется локально, детерминированно и дешево по сети 🙂

## API-разбор (по группам)

### 1) Entity-level setters

Используй для свойств всего спрайта:

- `SetScale`, `SetRotation`, `SetOffset`
- `SetVisible`, `SetDrawDepth`, `SetColor`
- `SetBaseRsi`, `SetContainerOccluded`
- `SetSnapCardinals`, `SetGranularLayersRendering`

### 2) Layer CRUD

Создание/удаление слоев:

- `AddBlankLayer`
- `AddLayer` (из `Layer`, `SpriteSpecifier`, `PrototypeLayerData`)
- `AddRsiLayer`
- `AddTextureLayer`
- `RemoveLayer`
- `TryGetLayer`
- `LayerExists`

### 3) Layer map API

Ключевые операции маппинга:

- `LayerMapSet`, `LayerMapAdd`
- `LayerMapRemove`
- `LayerMapTryGet`, `LayerMapGet`
- `LayerMapReserve`

Поддерживаются ключи `Enum` и `string`. Для проектного кода обычно выбирай `Enum`.

### 4) Layer mutators (основной рабочий набор)

- `LayerSetData`
- `LayerSetSprite`
- `LayerSetTexture`
- `LayerSetRsiState`
- `LayerSetRsi`
- `LayerSetScale`
- `LayerSetRotation`
- `LayerSetOffset`
- `LayerSetVisible`
- `LayerSetColor`
- `LayerSetDirOffset`
- `LayerSetAnimationTime`
- `LayerSetAutoAnimated`
- `LayerSetRenderingStrategy`

### 5) Layer getters

- `LayerGetRsiState`
- `LayerGetEffectiveRsi`
- `LayerGetDirections`
- `LayerGetDirectionCount`

### 6) Bounds / Render / Helpers

- `GetLocalBounds` (для всего спрайта и отдельного слоя)
- `CalculateBounds`
- `RenderSprite`
- `GetFrame`
- `Frame0`, `RsiStateLike`
- `GetIcon`, `GetPrototypeIcon`, `GetPrototypeTextures`
- `GetFallbackState`, `GetFallbackTexture`
- `GetState`, `GetTexture`
- `GetSpriteWorldPosition`, `GetSpriteScreenCoordinates`

### 7) Служебные методы

- `ForceUpdate`
- `SetAutoAnimateSync` (для спрайта/слоя)
- `CopySprite`
- `QueueUpdateIsInert` / `QueueUpdateInert`

### Паттерн перегрузок

Большинство layer-методов имеют варианты:

- по индексу `int`;
- по ключу `Enum`;
- по ключу `string`;
- по объекту `Layer`.

Правило: в gameplay-коде предпочтительнее `Enum`-ключи через layer map.

## Обязательно знать про obsolete-обертки

В `SpriteComponent` есть много устаревших прокси-методов (`[Obsolete]`), которые перенаправляют к `SpriteSystem`.

Почему прямые вызовы `SpriteComponent` - анти-паттерн:

- размывают единый API обновления;
- усложняют рефакторинг и аудит визуальных изменений;
- ломают консистентность с современным ECS-стилем в проекте;
- увеличивают риск тихих регрессий при изменении движка ⚠

Коротко: новый код пишет через `SpriteSystem`, не через старые методы компонента.

## Практические примеры

### Пример 1: базовая настройка спрайта сущности

```csharp
public void ApplyMachineLook(EntityUid uid, SpriteComponent sprite, bool highlighted)
{
    // Меняем только визуальные свойства сущности.
    _sprite.SetVisible((uid, sprite), true);
    _sprite.SetDrawDepth((uid, sprite), (int)DrawDepth.Machines);
    _sprite.SetColor((uid, sprite), highlighted ? Color.Cyan : Color.White);
}
```

### Пример 2: резерв слоя по enum-ключу и заполнение данных

```csharp
private enum MachineLayerKey : byte
{
    Base,
    Status
}

public void SetStatusLayer(EntityUid uid, SpriteComponent sprite, PrototypeLayerData data)
{
    // Гарантируем наличие слоя под ключом.
    var index = _sprite.LayerMapReserve((uid, sprite), MachineLayerKey.Status);

    // Обновляем слой целиком через PrototypeLayerData.
    _sprite.LayerSetData((uid, sprite), index, data);
    _sprite.LayerSetVisible((uid, sprite), index, true);
}
```

### Пример 3: безопасное удаление слоя по ключу

```csharp
public void HideStatusLayer(EntityUid uid, SpriteComponent sprite)
{
    // Не считаем отсутствие слоя ошибкой: это нормальный idempotent-поток.
    _sprite.RemoveLayer((uid, sprite), MachineLayerKey.Status, logMissing: false);
}
```

### Пример 4: динамическая генерация и очистка текстовых слоев

```csharp
public void RebuildTextLayers(EntityUid uid, SpriteComponent sprite, IReadOnlyList<int> oldLayers, string text)
{
    // Сначала очищаем старые временные слои.
    foreach (var old in oldLayers)
        _sprite.RemoveLayer((uid, sprite), old, logMissing: false);

    var x = 0f;
    foreach (var ch in text)
    {
        // Для каждого символа создаем отдельный слой.
        var layer = _sprite.AddRsiLayer((uid, sprite), new RSI.StateId(ch.ToString()), _fontRsi);
        _sprite.LayerSetOffset((uid, sprite), layer, new Vector2(x, 0f));
        _sprite.LayerSetVisible((uid, sprite), layer, true);

        x += 0.5f; // Шаг между символами.
    }
}
```

### Пример 5: направленные смещения слоя

```csharp
public void ApplyDirectionalOffset(EntityUid uid, SpriteComponent sprite, Enum pipeLayerKey, DirectionOffset dirOffset)
{
    // Один и тот же слой выглядит по-разному в разных направлениях.
    _sprite.LayerSetDirOffset((uid, sprite), pipeLayerKey, dirOffset);
}
```

### Пример 6: битовая маска для группы индикаторных слоев

```csharp
[Flags]
public enum IndicatorBits : byte
{
    None = 0,
    Powered = 1 << 0,
    Charging = 1 << 1,
    Broken = 1 << 2
}

public void UpdateIndicators(EntityUid uid, SpriteComponent sprite, IndicatorBits bits)
{
    // Каждый флаг управляет своим слоем, без длинных if-цепочек по состояниям.
    _sprite.LayerSetVisible((uid, sprite), "powered",  (bits & IndicatorBits.Powered)  != 0);
    _sprite.LayerSetVisible((uid, sprite), "charging", (bits & IndicatorBits.Charging) != 0);
    _sprite.LayerSetVisible((uid, sprite), "broken",   (bits & IndicatorBits.Broken)   != 0);
}
```

### Пример 7: замена base RSI + draw depth в одном апдейте

```csharp
public void ApplyStorageVisualMode(EntityUid uid, SpriteComponent sprite, bool opened)
{
    // Меняем базовый RSI и глубину отрисовки как один логический апдейт.
    _sprite.SetBaseRsi((uid, sprite), opened ? _openedRsi : _closedRsi);
    _sprite.SetDrawDepth((uid, sprite), opened ? (int)DrawDepth.SmallObjects : (int)DrawDepth.Objects);
    _sprite.ForceUpdate(uid); // Подталкиваем немедленное визуальное обновление.
}
```

## Паттерны 🙂

- Используй `LayerMapReserve` + `Enum`-ключи для стабильной адресации слоев.
- Считай слой минимальной единицей визуального состояния, а не "весь спрайт сразу".
- Группируй изменения одного визуального события в одном методе.
- Для временных слоев всегда держи явный цикл очистки.
- Для сложных индикаторов используй битовые маски, а не каскады булевых полей.
- Для одинаковых визуальных сущностей копируй слойную конфигурацию через `CopySprite`.

## Анти-паттерны ❌

- Прямые устаревшие вызовы `SpriteComponent` вместо `SpriteSystem`.
- Работа с "магическими индексами" слоев без layer map.
- Смешивание gameplay-логики и визуального апдейта в одном методе.
- Частые полные пересборки всех слоев при изменении одного флага.
- Использование `string`-ключей там, где уже есть `Enum`.
- Отсутствие обработки `RemoveLayer(..., logMissing: false)` в idempotent-потоках.

## Чеклист перед изменением ✅

- Ты меняешь визуал через `SpriteSystem`, а не через obsolete API?
- Ключевые слои доступны по `Enum`/layer map?
- Для динамических слоев есть симметричная очистка?
- Локальные апдейты не пересобирают спрайт целиком без причины?
- Выбран подходящий метод: `LayerSetData` (bulk) или точечный mutator?
- Изменения не вносят сетевую бизнес-логику в клиентский визуальный код?

## Типовые ошибки

- Слой есть в прототипе, но не зарезервирован/не привязан к ожидаемому ключу.
- Значение состояния обновилось, но нужный слой остался невидимым.
- Использован неверный overload (индекс вместо ключа, или наоборот).
- Временные слои не очищаются и накапливаются между апдейтами.
- Неверный `DrawDepth` дает "пропадание" объекта за соседними сущностями.
- Пытаются лечить архитектурную проблему `ForceUpdate`, а не корректным потоком обновления.

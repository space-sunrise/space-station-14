---
name: SS14 Graphics GenericVisualizer Appearance
description: Практический и архитектурный гайд по связке AppearanceComponent, AppearanceSystem, VisualizerSystem и GenericVisualizer в SS14. Используй при проектировании сетевых визуальных состояний, YAML-визуализаций и клиентских visualizer-систем.
---

# GenericVisualizer и Appearance в SS14

Этот skill покрывает только pipeline `Appearance` + `VisualizerSystem` + `GenericVisualizer` 🙂
Низкоуровневый рендер и детальный `SpriteSystem` API разбирай в отдельном sprite-skill.

## Когда выбирать этот skill

Выбирай его, если задача про:

- репликацию визуального состояния с сервера на клиент;
- проектирование `Appearance`-ключей и payload-данных;
- выбор между `GenericVisualizer` и кастомным visualizer;
- YAML-описание визуализации через `visuals`;
- обработку `AppearanceChangeEvent`.

## Источник истины и актуальность

- При конфликте документации и кода приоритет у текущего кода.
- Документация используется как концептуальная опора, но API проверяется по реализации.
- Практические рекомендации и примеры опираются на свежие паттерны из актуального кода.

## Архитектура end-to-end

1. Серверная логика вычисляет визуальное состояние.
2. Сервер пишет данные в `AppearanceComponent` через `SharedAppearanceSystem.SetData(...)`.
3. Данные попадают в component state и сетево синхронизируются.
4. Клиентский `AppearanceSystem` принимает state, обновляет словарь appearance-данных, ставит апдейт в очередь.
5. Во время `FrameUpdate` поднимается `AppearanceChangeEvent`.
6. Клиентские visualizer-системы (`VisualizerSystem<T>`) применяют данные к спрайту.
7. Если достаточно простого маппинга значений на layer-data, это делает `GenericVisualizerSystem`.

Идея: сервер передает не "как рисовать", а "что визуально истинно"; клиент решает "как отрисовать" 🙂

## Enum-контракт: какие enum нужны и в каком порядке

В этой теме обычно участвуют **два разных enum-контракта**, иногда три:

1. `AppearanceKey enum` (shared)
   Это ключи словаря `AppearanceData`.
   Именно эти enum передаются в `SetData` / `TryGetData` / `RemoveData`.

2. `LayerKey enum` (клиент + прототипная визуализация)
   Это ключи слоев спрайта (layer map), по которым ты меняешь `visible/state/color/...`.
   Эти enum используются в `SpriteSystem` и/или в `GenericVisualizer` YAML как layer key.

3. `AppearanceValue enum` (опционально, shared)
   Это не ключ, а **значение** appearance-данных.
   Пример: `ChargeState.Empty/Medium/Full`.

Критически важно: `AppearanceKey enum` и `LayerKey enum` - это разные роли, их нельзя смешивать ⚠

### Порядок применения в коде и YAML

1. Описываешь `AppearanceKey enum` в shared-контракте.
2. Описываешь `LayerKey enum` для адресации слоев.
3. Сервер пишет значение: `SetData(uid, AppearanceKey.SomeKey, value)`.
4. Клиент:
   - либо custom visualizer: читает `AppearanceKey` через `TryGetData`, применяет к `LayerKey` через `SpriteSystem`;
   - либо GenericVisualizer: YAML маппит `AppearanceKey -> LayerKey -> AppearanceValueString -> PrototypeLayerData`.

### Шаблон соответствия

```yaml
visuals:
  enum.AppearanceKeyEnum.SomeKey:
    enum.LayerKeyEnum.TargetLayer:
      "AppearanceValueToString":
        state: some_state
        visible: true
```

```csharp
[Serializable, NetSerializable]
public enum LockerVisuals : byte // 1) AppearanceKey enum
{
    Open,
    ChargeState,
}

[Serializable, NetSerializable]
public enum LockerVisualLayers : byte // 2) LayerKey enum
{
    DoorOpen,
    DoorClosed,
    Indicator,
}

[Serializable, NetSerializable]
public enum LockerChargeState : byte // 3) AppearanceValue enum (опционально)
{
    Empty,
    Medium,
    Full,
}

// Сервер: только AppearanceKey + value
_appearance.SetData(uid, LockerVisuals.Open, true, appearance);
_appearance.SetData(uid, LockerVisuals.ChargeState, LockerChargeState.Full, appearance);

// Клиент (custom visualizer): читает AppearanceKey, применяет к LayerKey
if (_appearance.TryGetData(uid, LockerVisuals.Open, out bool open, args.Component))
{
    _sprite.LayerSetVisible((uid, args.Sprite), LockerVisualLayers.DoorOpen, open);
    _sprite.LayerSetVisible((uid, args.Sprite), LockerVisualLayers.DoorClosed, !open);
}
```

## API-разбор

## 1) `SharedAppearanceSystem`

Ключевые методы:

- `SetData(EntityUid, Enum, object, AppearanceComponent?)`
- `RemoveData(EntityUid, Enum, AppearanceComponent?)`
- `TryGetData<T>(...)`
- `TryGetData(..., out object?)`
- `CopyData(Entity<AppearanceComponent?> src, Entity<AppearanceComponent?> dest)`
- `AppendData(...)`
- `QueueUpdate(...)` (виртуальный, конкретная реализация зависит от стороны)

Важно:

- В актуальном API ключи appearance-данных - `Enum`.
- Значения appearance должны быть корректно клонируемыми/сериализуемыми.

## 2) Клиентский `AppearanceSystem`

Что важно знать:

- держит очередь обновлений appearance;
- в `FrameUpdate` вызывает изменение визуала только для актуальных сущностей;
- формирует и рассылает `AppearanceChangeEvent`;
- при приеме state сравнивает данные и обновляет словарь;
- при клонировании appearance-данных требует безопасные типы:
  - value type, или
  - `ICloneable`, или
  - тип, который сериализатор умеет копировать.

Если это не выполнено, возможны исключения при применении state ⚠

## 3) Серверный `AppearanceSystem`

- Выдает `AppearanceComponentState` для сетевой синхронизации.
- Не занимается клиентским применением слоев и не должен знать детали рендера.

## 4) `VisualizerSystem<T>`

Контракт:

- наследуешься от `VisualizerSystem<TVisualComponent>`;
- переопределяешь `OnAppearanceChange(..., ref AppearanceChangeEvent args)`;
- читаешь appearance-данные и применяешь визуальные изменения на клиенте.

## 5) `GenericVisualizerComponent`

Основная структура:

- `visuals`: вложенный словарь формата
  `AppearanceKey -> LayerKey -> AppearanceValueString -> PrototypeLayerData`.

Следствие:

- входные значения appearance нормализуются в строку;
- layer key может быть enum-reference или обычной строкой.

## 6) `GenericVisualizerSystem`

Алгоритм применения:

1. Идет по описанным `visuals`.
2. Для каждого appearance-ключа пытается прочитать текущее значение.
3. Преобразует значение в строку (`ToString()`).
4. Ищет соответствующий `PrototypeLayerData` в словаре вариантов.
5. Резервирует/получает слой по ключу.
6. Применяет `LayerSetData`.

Практический вывод:

- `GenericVisualizer` отлично подходит для чистого declarative-мэппинга;
- для сложных ветвлений, анимаций, взаимодействия с несколькими системами нужен кастомный visualizer.

## Decision Tree: GenericVisualizer или custom visualizer

1. Нужно только "значение X -> state/visible/color/shader/offset/..." без сложной логики?
   Используй `GenericVisualizer` ✅
2. Нужны таймеры, анимации, внешние системные зависимости или сложные вычисления?
   Пиши custom `VisualizerSystem<T>` ✅
3. Нужно динамически создавать/удалять много слоев в рантайме?
   Обычно custom visualizer ✅
4. Нужна композиция нескольких appearance-ключей с нетривиальными правилами?
   Обычно custom visualizer ✅

## Практические примеры

### Пример 1: сервер пишет и очищает appearance-флаг

```csharp
[NetSerializable, Serializable]
public enum LockerVisuals : byte
{
    Open,
}

private void UpdateLockerAppearance(EntityUid uid, AppearanceComponent appearance, bool open)
{
    // Сервер сообщает только фактическое визуальное состояние.
    _appearance.SetData(uid, LockerVisuals.Open, open, appearance);

    if (!open)
    {
        // При необходимости ключ можно явно убрать.
        _appearance.RemoveData(uid, LockerVisuals.Open, appearance);
    }
}
```

### Пример 2: клиентский visualizer читает typed-данные

```csharp
protected override void OnAppearanceChange(EntityUid uid, LockerVisualsComponent component, ref AppearanceChangeEvent args)
{
    if (args.Sprite == null)
        return;

    // Читаем AppearanceKey enum (LockerVisuals), а применяем LayerKey enum (LockerVisualLayers).
    if (_appearance.TryGetData(uid, LockerVisuals.Open, out bool open, args.Component))
    {
        _sprite.LayerSetVisible((uid, args.Sprite), LockerVisualLayers.DoorOpen, open);
        _sprite.LayerSetVisible((uid, args.Sprite), LockerVisualLayers.DoorClosed, !open);
    }
}
```

### Пример 3: сложный payload в appearance

```csharp
[Serializable, NetSerializable]
public sealed class ShowLayerData
{
    public string Key = string.Empty;   // Ключ целевого слоя.
    public bool Visible;                // Нужно ли показывать слой.
    public string? State;               // Необязательное состояние RSI.
}

private void PushLayerPayload(EntityUid uid, AppearanceComponent appearance, ShowLayerData data)
{
    // Сложный payload удобнее, чем набор разрозненных булевых флагов.
    _appearance.SetData(uid, MapperVisuals.LayerData, data, appearance);
}
```

### Пример 4: перенос визуальных данных между сущностями

```csharp
private void CopyAppearance(Entity<AppearanceComponent?> source, Entity<AppearanceComponent?> target)
{
    // Полная замена данных назначения.
    _appearance.CopyData(source, target);

    // Либо merge-режим (append) для частичного обогащения.
    // _appearance.AppendData(source, target);
}
```

### Пример 5: YAML-конфиг GenericVisualizer (bool -> слой)

```yaml
- type: GenericVisualizer
  visuals:
    enum.LockerVisuals.Open:
      enum.LockerVisualLayers.DoorOpen:
        "True":
          visible: true
      enum.LockerVisualLayers.DoorClosed:
        "True":
          visible: false
        "False":
          visible: true
```

### Пример 6: YAML-конфиг GenericVisualizer (enum value -> state/shader)

```yaml
- type: GenericVisualizer
  visuals:
    enum.PowerVisuals.ChargeState:
      enum.PowerVisualLayers.Indicator:
        "Empty":
          state: empty
          shader: unshaded
        "Medium":
          state: medium
        "Full":
          state: full
          color: "#99ff99"
```

### Пример 7: когда нужен custom visualizer вместо GenericVisualizer

```csharp
protected override void OnAppearanceChange(EntityUid uid, TriggerVisualsComponent component, ref AppearanceChangeEvent args)
{
    if (args.Sprite == null)
        return;

    if (!_appearance.TryGetData(uid, TriggerVisuals.Active, out bool active, args.Component))
        return;

    // Тут уже не просто маппинг: требуется запуск анимации и доп. логика.
    _sprite.LayerSetRsiState((uid, args.Sprite), TriggerLayers.Core, active ? "active" : "idle");
    _animation.Play(uid, "pulse", active);
}
```

## Паттерны 🙂

- Держи ключи appearance в shared-enum, чтобы сервер и клиент говорили на одном контракте.
- Передавай "узкие" и семантические payload-структуры, а не аморфный набор `object`.
- Используй `GenericVisualizer`, когда задача реально declarative.
- Для сложной логики используй custom `VisualizerSystem<T>` и явный `OnAppearanceChange`.
- Для миграций/трансформаций сущностей применяй `CopyData`/`AppendData`.
- Явно чисти устаревшие ключи через `RemoveData`, если они больше невалидны.

## Анти-паттерны ❌

- Передавать в appearance не-клонируемые/не-сериализуемые reference-объекты.
- Использовать строки вместо enum-ключей там, где можно задать shared enum.
- Передавать `LayerKey enum` в `SetData/TryGetData` вместо `AppearanceKey enum`.
- Пытаться запихнуть сложные ветвления и анимации в `GenericVisualizer`.
- Дублировать одинаковую визуальную логику в нескольких несвязанных visualizer-системах.
- Не удалять устаревшие appearance-ключи и получать "залипший" визуал.
- Смешивать в одном методе серверный расчет состояния и клиентское применение слоя.

## Чеклист перед изменением ✅

- Контракт ключей и payload-типов описан в shared-части?
- Значения appearance безопасны для clone/copy/state sync?
- Выбран правильный инструмент: `GenericVisualizer` или custom visualizer?
- Обновление/очистка ключей (`SetData`/`RemoveData`) симметричны?
- Клиентский visualizer идемпотентен и не зависит от случайного порядка событий?
- Визуальная логика не уехала на сервер и наоборот?

## Типовые ошибки

- В YAML-visuals значение ключа не совпадает со строкой `ToString()` реального enum/bool.
- Перепутаны enum-роли: `AppearanceKey` и `LayerKey` (самая частая ошибка при первом внедрении).
- Кастомный payload не сериализуется по сети и ломает применение state.
- Логика читает appearance-ключ, который никогда не выставляется сервером.
- Визуализатор обновляет не тот слой из-за несогласованных layer-key.
- `GenericVisualizer` выбран для сценария, где реально нужна анимация/таймеры.
- Миграция сущности скопировала внешность (`CopyData`), но забыли обновить зависимые клиентские компоненты.

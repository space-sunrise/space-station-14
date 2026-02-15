---
name: SS14 ECS Components
description: Architecture guide for Component in Space Station 14 — data containers, attributes, networking, state-as-component pattern, and marker components
---

# Component — компоненты в ECS

## Что такое Component

Компонент — это **чистый контейнер данных** без логики. Компоненты прикрепляются к сущностям (Entity) и определяют их свойства. Вся логика работы с данными компонента находится в соответствующей системе (EntitySystem).

**Главное правило: компоненты не содержат методов с логикой.** Они хранят только данные и конфигурацию.

## Базовая структура

```csharp
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MyComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Speed = 5f;

    [DataField]
    public SoundSpecifier? ActivationSound;
}
```

## Обязательные атрибуты класса

### `[RegisterComponent]`

Регистрирует компонент в движке. Обязателен для всех компонентов. Без него компонент не будет доступен для использования.

### `[NetworkedComponent]`

Указывает, что компонент синхронизируется по сети между сервером и клиентом. Используется вместе с `[AutoGenerateComponentState]`.

### `[AutoGenerateComponentState]` и `[AutoGenerateComponentState(true)]`

Автоматически генерирует код для сериализации/десериализации состояния компонента при сетевой синхронизации.

Вариант `(true)` дополнительно генерирует метод `AfterAutoHandleState`, который вызывается после применения состояния — полезно для выполнения побочных эффектов после сетевого обновления.

### `[AutoGenerateComponentPause]`

Генерирует код для автоматической паузы таймерных полей (`TimeSpan`) при паузе карты. Работает в связке с `[AutoPausedField]`.

## Атрибуты полей

### `[DataField]`

Маркирует поле для десериализации из YAML прототипов. Имя поля в YAML — это camelCase версия имени в C#:

```csharp
[DataField]
public float BaseSpeed = 5f;  // → baseSpeed в YAML

[DataField(required: true)]
public EntProtoId EntityId;   // Обязательное, ошибка если не задано
```

> **⚠️ Анти-паттерн: строковое имя в DataField (легаси)**
>
> Не указывайте строковое имя поля в `DataField`. Это устаревший подход. Имя в YAML **всегда** равно имени в C# с маленькой буквы:
>
> ```csharp
> // ❌ Легаси — НЕ делайте так
> [DataField("counter")]
> public int Counter;
>
> [DataField("baseSpeed")]
> public float BaseSpeed;
>
> // ✅ Правильно — имя выводится автоматически
> [DataField]
> public int Counter;        // → counter в YAML
>
> [DataField]
> public float BaseSpeed;    // → baseSpeed в YAML
> ```

> **⚠️ Анти-паттерн: DataField на рантайм-полях**
>
> `[DataField]` нужен **только** для полей, которые задаются через YAML-прототипы. Поля, которые генерируются в коде во время игры, **не должны** иметь `[DataField]`:
>
> ```csharp
> // ❌ Неправильно — EntityUid генерируется в коде, не в YAML
> [DataField]
> public EntityUid? CurrentTarget;
>
> [DataField]
> public HashSet<EntityUid> ActiveTargets = [];
>
> [DataField]
> public TimeSpan? RageStartTime;  // Устанавливается системой, не прототипом
>
> // ✅ Правильно — без DataField, только AutoNetworkedField если нужна синхронизация
> [AutoNetworkedField]
> public EntityUid? CurrentTarget;
>
> [AutoNetworkedField]
> public HashSet<EntityUid> ActiveTargets = [];
>
> [AutoNetworkedField]
> public TimeSpan? RageStartTime;
>
> // ✅ Правильно — DataField только для конфигурации из прототипа
> [DataField]
> public float MaxSpeed = 8f;  // Настраивается в YAML
>
> [DataField]
> public TimeSpan RageDuration = TimeSpan.FromMinutes(4);  // Настраивается в YAML
> ```

### `[AutoNetworkedField]`

Маркирует поле для автоматической сетевой синхронизации. Используется только совместно с `[AutoGenerateComponentState]` на классе:

```csharp
[DataField, AutoNetworkedField]
public int Counter;

[AutoNetworkedField]
public TimeSpan? StartTime;
```

### `[AutoPausedField]`

Маркирует поле типа `TimeSpan` для автоматической паузы при паузе карты. Используется с `[AutoGenerateComponentPause]`:

```csharp
[AutoNetworkedField, AutoPausedField]
public TimeSpan? ActivationTime;
```

### `[ViewVariables]`

Делает поле видимым в отладочном View Variables панели (VV):

```csharp
[ViewVariables] // По умолчанию доступ = (VVAccess.ReadWrite), прописывать снова НЕ нужно!
public float DebugValue;

[ViewVariables(VVAccess.ReadOnly)]
public int ReadOnlyValue;
```

### `[Access]`

Ограничивает доступ к полям/свойствам компонента. Только указанные типы могут писать в поля:

```csharp
[RegisterComponent, NetworkedComponent, Access(typeof(SharedMySystem))]
public sealed partial class MyComponent : Component
{
    // Только SharedMySystem и наследники могут изменять поля
}
```

### `[NonSerialized]`

Исключает поле из сериализации. Используется для рантайм-данных, которые не нужно сохранять и передавать:

```csharp
[NonSerialized]
public IPlayingAudioStream? SoundStream;

[NonSerialized]
public EntityUid? CurrentTarget;
```

## Типы данных компонентов

### Основные типы

```csharp
// ID прототипа сущности
[DataField]
public EntProtoId? SpawnPrototype;

// ID прототипа определённого типа
[DataField]
public ProtoId<DamageModifierSetPrototype> DamageModifier;

// Звук
[DataField]
public SoundSpecifier? Sound = new SoundPathSpecifier("/Audio/path.ogg");

[DataField]
public SoundSpecifier Sound = new SoundCollectionSpecifier("CollectionName");

// Урон
[DataField]
public DamageSpecifier Damage = new();

// Фильтрация сущностей
[DataField]
public EntityWhitelist? Whitelist;
[DataField]
public EntityWhitelist? Blacklist;

// Временные промежутки
[DataField]
public TimeSpan Duration = TimeSpan.FromSeconds(10);
```

### Коллекции

```csharp
[DataField]
public List<EntProtoId> Prototypes = [];

[DataField]
public HashSet<EntityUid> Targets = [];

[DataField]
public Dictionary<string, float> Values = new();
```

## Паттерн «состояние как компонент»

Вместо хранения enum-состояний внутри одного компонента, каждое состояние моделируется **отдельным компонентом**. Переход между состояниями = добавление/удаление компонентов.

### Основной компонент (хранит конфигурацию):

```csharp
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CreatureComponent : Component
{
    [DataField]
    public float BaseSpeed = 3f;

    [DataField]
    public TimeSpan RageDuration = TimeSpan.FromMinutes(2);
}
```

### Состояние «Спокоен» — нет дополнительных компонентов

### Состояние «Нагревается»:

```csharp
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ActiveCreatureHeatingUpComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan HeatingDuration = TimeSpan.FromSeconds(30);

    [AutoNetworkedField]
    public TimeSpan? StartTime;
}
```

### Состояние «Ярость»:

```csharp
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ActiveCreatureRageComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan RageDuration = TimeSpan.FromMinutes(4);

    [DataField]
    public float Speed = 8f;

    [AutoNetworkedField]
    public TimeSpan? RageStartTime;
}
```

### Переходы между состояниями в системе:

```csharp
// Переход в ярость
private void EnterRage(EntityUid uid, CreatureComponent comp)
{
    RemComp<ActiveCreatureHeatingUpComponent>(uid);  // выход из предыдущего
    var rage = EnsureComp<ActiveCreatureRageComponent>(uid);  // вход в новое
    rage.RageStartTime = _timing.CurTime;
    Dirty(uid, rage);
}

// Выход из ярости
private void ExitRage(EntityUid uid, CreatureComponent comp)
{
    RemComp<ActiveCreatureRageComponent>(uid);
}
```

**Преимущества паттерна:**
- Системы могут подписываться на `ComponentStartup`/`ComponentShutdown` состояний
- `EntityQueryEnumerator` итерирует только по сущностям в нужном состоянии
- Сетевая синхронизация состояний происходит автоматически
- Легко добавлять новые состояния без модификации основного компонента

## Компоненты-маркеры

Компоненты без полей данных, используемые как «теги» для фильтрации:

```csharp
[RegisterComponent]
public sealed partial class ProtectedComponent : Component
{
    // Нет полей — это просто маркер
}
```

Системы проверяют наличие маркера:
```csharp
if (HasComp<ProtectedComponent>(uid))
    return; // Сущность защищена, пропускаем
```

## Компонент как целевая метка

Компоненты могут добавляться к **другим** сущностям для установления связи:

```csharp
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TargetMarkerComponent : Component
{
    /// <summary>
    /// Ссылка на преследующую сущность
    /// </summary>
    [AutoNetworkedField]
    public EntityUid? Source;

    [DataField]
    public float RequiredDamage = 200f;

    [AutoNetworkedField]
    public float DamageApplied;
}
```

## Правила написания компонентов

1. **Класс всегда `sealed partial`** — `sealed` предотвращает наследование, `partial` нужен для source generators
2. **Наследуйтесь от `Component`** — не от других компонентов
3. **Без логики** — только поля данных. Никаких методов, свойства только для простого доступа
4. **XML-документация** — каждое публичное поле должно иметь `/// <summary>` комментарий
5. **Разумные значения по умолчанию** — поля должны иметь дефолтные значения, чтобы прототип мог опускать необязательные поля
6. **`[NonSerialized]` для рантайм-данных** — звуковые потоки, кешированные ссылки на сущности, временные данные
7. **Организация полей** — используйте `#region` блоки для группировки связанных полей в больших компонентах
8. **`[DataField]` только для YAML-конфигурации** — не ставьте на рантайм-поля (`EntityUid`, таймстампы, кеши)
9. **Не указывайте строковое имя в `[DataField]`** — имя выводится автоматически из имени поля

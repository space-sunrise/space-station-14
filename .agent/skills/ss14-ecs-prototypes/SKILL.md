---
name: SS14 ECS Prototypes
description: YAML prototypes in Space Station 14 — entity definitions, field inheritance, prototype classes, YAML linter, naming conventions, and localization
---

# Prototypes — прототипы в ECS

## Граница ответственности

Этот skill покрывает механику прототипов: структуру YAML, наследование, маппинг `DataField`, типы прототипов и валидацию.
Строгие naming-нормативы (prototype ID, форк-префиксы, английские fallback-поля, `ent-*` и `kebab-case` ключи) ведутся в `ss14-naming-conventions`.
Если пример по именованию расходится с `ss14-naming-conventions`, применяй `ss14-naming-conventions`.

## Что такое Prototype

Прототип — это YAML-определение данных, которое движок загружает при инициализации. Прототипы описывают сущности, рецепты, реагенты и другие игровые объекты. Прототипы сущностей определяют, какие компоненты и с какими значениями будут у сущности при создании.

## Формат ID — краткая памятка (см. ss14-naming-conventions)

Все идентификаторы прототипов **обязательно** записываются в CamelCase:

```yaml
# Правильно
- type: entity
  id: Scp096

- type: entity
  id: BaseScp

- type: entity
  id: MobCombat

- type: entity
  id: Scp096CryOut

- type: entity
  id: XenoArchTriggerHeat

- type: entity
  id: WeaponArcClaw
```

```yml
# Неправильно
- type: entity
  id: scp_096          # подчёркивания

- type: entity
  id: scp-096          # дефисы

- type: entity
  id: SCP096           # все заглавные

- type: entity
  id: scp096           # все строчные
```

## Прототип сущности — базовая структура

```yaml
- type: entity
  id: MyEntity
  parent: BaseParent        # Наследование от одного родителя
  suffix: Debug             # Пометка в spawn-панели (необязательно)
  components:
  - type: Sprite
    sprite: path/to/sprite.rsi
    layers:
    - state: idle
  - type: MyAction # Оригинальное название MyActionComponent. в Прототипе окончание не пишется!
    speed: 5.0
    activationSound:
      path: /Audio/sound.ogg
```

## Наследование прототипов

### Одиночное наследование

```yaml
- type: entity
  id: BaseAnimal
  abstract: true           # Абстрактный — не создаётся напрямую
  components:
  - type: MobState
  - type: Damageable

- type: entity
  id: Cat
  parent: BaseAnimal       # Наследует все компоненты BaseAnimal
  components:
  - type: Sprite
    sprite: animals/cat.rsi
```

### Множественное наследование

```yaml
- type: entity
  id: Scp096
  parent:
  - BaseScp                # Наследует от нескольких родителей
  - MobCombat
  - MobBloodstream
  - StripableInventoryBase
  # Наложение наследований идет СВЕРХУ ВНИЗ!
  # Самый нижний имеет наиболее высокий приоритет, его компоненты и значения перезапишут остальные, если будет конфликт
  components:
  - type: Scp096
    # ...специфичные поля
```

Порядок родителей имеет значение — данные применяются в порядке указания.

## Система наследования полей

По умолчанию, поле дочернего прототипа **полностью перезаписывает** родительское значение. Это поведение изменяется атрибутами:

### `[AlwaysPushInheritance]` — мержинг

Вместо перезаписи **объединяет** данные родителя и потомка. Мержинг работает рекурсивно на уровне YAML-маппингов и последовательностей.

```yaml
# Родитель
- type: entity
  id: BaseEntity
  abstract: true
  components:              # components помечен [AlwaysPushInheritance]
  - type: Sprite
    sprite: base.rsi

# Потомок
- type: entity
  id: ChildEntity
  parent: BaseEntity
  components:
  - type: MyComponent      # ДОБАВЛЯЕТСЯ к родительским компонентам
    value: 5
```

Результат: у `ChildEntity` есть и `Sprite` (от родителя), и `MyComponent` (свой).

Основные применения:
- **`components`** в `EntityPrototype` — компоненты потомка **мержатся** с родительскими
- **Списки действий** (`ActionGrantComponent.actions`) — действия потомка **добавляются** к родительским
- **Списки рецептов** (`LatheRecipePackPrototype`) — рецепты потомка добавляются

### `[NeverPushInheritance]` — блокировка наследования

Значение поля **никогда** не передаётся от родителя к потомку. Потомок получает значение по умолчанию.

```csharp
[NeverPushInheritance]
public bool Abstract { get; private set; }  // abstract не наследуется

[NeverPushInheritance]
public HashSet<ProtoId<EntityCategoryPrototype>>? Categories;  // категории не наследуются
```

Используется для:
- `abstract` — потомок не абстрактный, даже если родитель абстрактный
- `categories` — категории индивидуальны для каждого прототипа
- Уникальные идентификаторы — ID, визуальные данные, которые не должны каскадировать

### `[AbstractDataField]` — абстрактный прототип

Маркирует поле `abstract` в прототипе. Абстрактные прототипы:
- Не индексируются через `IPrototypeManager`
- Не отображаются при перечислении прототипов
- Служат только источником данных для наследования

```yaml
- type: entity
  id: BaseMob
  abstract: true       # Этот прототип — только шаблон
  components:
  - type: MobState
  - type: Damageable
```

### `[ParentDataField]` — поле родителя

Маркирует поле, содержащее ссылку на родительские прототипы. В `EntityPrototype` это поле `Parents`.

## Маппинг C# → YAML

Поля компонентов с `[DataField]` сериализуются в YAML через camelCase:

```csharp
// C# компонент
[DataField]
public float BaseSpeed = 5f;

[DataField]
public TimeSpan RageDuration = TimeSpan.FromMinutes(4);

[DataField]
public SoundSpecifier RageSound = new SoundPathSpecifier("/Audio/scream.ogg");

[DataField]
public DamageSpecifier CryOutDamage = new();

[DataField]
public EntityWhitelist? CryOutWhitelist;
```

```yaml
# YAML прототип
- type: entity
  id: MyCreature
  components:
  - type: MyComponent
    baseSpeed: 5.0
    rageDuration: 240         # секунды (TimeSpan)
    rageSound:
      path: /Audio/scream.ogg
      params:
        volume: 20
        maxDistance: 30
    cryOutDamage:
      types:
        Structural: 850
    cryOutWhitelist:
      tags:
      - Wall
      - Window
```

### Кастомное имя поля

```csharp
[DataField("customYamlName")]
public float SomeCSharpName;
```

```yaml
  customYamlName: 5.0    # Используется кастомное имя
```

## Создание своего типа прототипа

### Интерфейсы

- `IPrototype` — базовый, обязательно `string ID`
- `IInheritingPrototype` — добавляет поддержку наследования (`Parents`, `Abstract`)

### Простой прототип (без наследования)

```csharp
[Prototype]
public sealed partial class MyTriggerPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public LocId Tip;

    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public ComponentRegistry Components = new();
}
```

```yaml
- type: MyTrigger
  id: HeatTrigger
  tip: trigger-heat-tip
  components:
  - type: Temperature
    minValue: 500
```

### Прототип с наследованием

```csharp
[Prototype]
public sealed partial class MyInheritingPrototype : IPrototype, IInheritingPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<MyInheritingPrototype>))]
    public string[]? Parents { get; private set; }

    [NeverPushInheritance, AbstractDataField]
    public bool Abstract { get; private set; }

    [DataField]
    public float Value;
}
```

### Атрибут `[Prototype]`

```csharp
[Prototype]                        // Имя типа выводится из имени класса
[Prototype("customTypeName")]      // Кастомное имя типа для YAML
[Prototype(loadPriority: -1)]      // Приоритет загрузки
```

Тип `type` в YAML вычисляется из имени класса: `MyTriggerPrototype` → `MyTrigger` (суффикс `Prototype` отбрасывается).

## Ссылки на прототипы в коде

```csharp
// Типизированная ссылка на прототип сущности
[DataField]
public EntProtoId SpawnEntity = "DefaultEntity";

// Типизированная ссылка на произвольный прототип
[DataField]
public ProtoId<DamageModifierSetPrototype> DamageModifier = "Default";

// Получение прототипа в системе
[Dependency] private readonly IPrototypeManager _proto = default!;

var proto = _proto.Index<MyPrototype>("protoId");
if (_proto.TryIndex<MyPrototype>("protoId", out var proto))
{
    // proto доступен
}
```

## Именование и локализация (name / description)

### Система локализации через FTL

Имена и описания задаются **не** в YAML, а через файлы локализации (`.ftl`):

```text
Resources/Locale/en-US/_prototypes/.../myentity.ftl
Resources/Locale/ru-RU/_prototypes/.../myentity.ftl
```

Формат FTL:
```ftl
ent-MyEntityId = entity name
    .desc = Entity description goes here.
    .suffix = Debug variant
```

### Правила именования

1. **Названия с маленькой буквы**: `ent-Scp096CryOut = emit mournful scream`
2. **Описания с большой буквы**: `.desc = A strange creature that reacts to being seen.`
3. **В прототипе YAML — только fallback на английском**: поля `name` и `description` в YAML служат запасным вариантом, если локализация отсутствует
4. **Имена наследуются от родителя** автоматически через FTL — потомок использует имя родителя, если своё не задано
5. **`suffix`** — для пометки вариантов в spawn-панели, не видна игрокам

### Внутренние сущности

```yaml
- type: entity
  id: InternalEntity
  save: false                    # Не сохраняется на карте
  categories: [ HideSpawnMenu ] # Не показывается в spawn-панели
  components:
  - type: MyComponent
```

`save: false` — сущность не попадёт в файл карты при сохранении.
`categories: [HideSpawnMenu]` — скрывает сущность из панели спавна.

## Content.YAMLLinter

Отдельный проект `Content.YAMLLinter` — инструмент валидации всех YAML-прототипов.

### Что проверяет

1. **Корректность полей** — все `[DataField]` в YAML должны существовать в C# компоненте
2. **Ссылки на прототипы** — `ProtoId<T>` и `EntProtoId` ссылаются на существующие прототипы
3. **Клиент-серверная валидация** — проверяет YAML на обеих сторонах, учитывает client-only и server-only типы
4. **Валидация наследования** — корректность цепочки `parent`

### Как работает

- Запускает сервер и клиент через интеграционные тесты
- Вызывает `IPrototypeManager.ValidateDirectory` для всех директорий с прототипами
- Вызывает `ValidateStaticFields` для проверки ссылок в коде
- Объединяет ошибки обеих сторон

### В CI

YAMLLinter запускается автоматически в CI/CD. Ошибки блокируют мердж. Формат выходных ошибок:
```text
::error in Prototypes/file.yml(42,5)  Unknown field 'nonExistentField' for component 'MyComponent'
```

## Специальные YAML-конструкции

### Типизированные ноды

```yaml
containers:
  my_slot: !type:ContainerSlot     # Конкретный тип контейнера
  storage: !type:Container          # Обычный контейнер
```

### Вложенные маппинги

```yaml
- type: entity
  id: MyEntity
  components:
  - type: Bloodstream
    bloodReferenceSolution:
      reagents:
      - ReagentId: Water
        Quantity: 300
  - type: Fixtures
    fixtures:
      fix1:
        shape:
          !type:PhysShapeAabb
          bounds: "-0.25,-0.4,0.25,0.4"
        density: 500
        mask:
        - MobMask
        layer:
        - MobLayer
```

### Whitelist / Blacklist

```yaml
- type: MyComponent
  pickupBlacklist:
    components:
    - Gun
    - MeleeWeapon
    - Storage
  targetWhitelist:
    tags:
    - Wall
    - Window
```

### Урон (DamageSpecifier)

```yaml
  damage:
    types:
      Slash: 27
      Structural: 110
      Bloodloss: 5
```

### Звуки

```yaml
  # Путь к файлу
  sound:
    path: /Audio/sound.ogg
    params:
      volume: -4
      maxDistance: 10
```

```yml
  # Коллекция звуков
  sound:
    collection: GibCollection
    params:
      variation: 0.125
```

---
name: ss14-naming-conventions
description: Строгий норматив по неймингу в Space Station 14 для C#, YAML prototypes и FTL: имена компонентов/систем/зависимостей, prototype IDs, локализационные ключи, переменные и файлы. Используй при создании или ревью нового кода, прототипов и локализации, когда нужно проверить соответствие naming-стандарту.
---

# SS14 Naming Conventions

Этот skill задает единый строгий стандарт нейминга для SS14-кода, прототипов и локализации 🙂
Используй его как норматив: если новый код/контент не соответствует правилам ниже, это ошибка, а не «вариант стиля».

## Порядок чтения

1. Сначала прочитай `references/fresh-pattern-catalog.md`.
2. Затем прочитай `references/rejected-snippets.md`.
3. В конце сверяй терминологию и границы docs в `references/docs-context.md`.

## Источник истины

1. Источник истины для правил — текущая кодовая база.
2. Документация нужна для intent, терминов и пояснений, но не перезаписывает поведение живого кода.
3. Примеры старше cutoff `2024-02-20` и фрагменты с TODO/HACK/FIXME по теме нейминга не использовать как эталон.

## Ментальная модель

1. Имена в SS14 — это контракт между C#, YAML и FTL.
2. Базовая цель имени: быстро передать роль сущности/типа/ключа, не открывая реализацию.
3. Хороший нейминг минимизирует переименования при расширении функционала.
4. Новый код обязан быть англоязычным в идентификаторах и fallback-полях прототипов.

## Строгие правила (MUST/SHOULD)

### 1) Компоненты

1. MUST: имя компонента заканчивать на `Component`.
2. MUST: формат имени — `CamelCase`.
3. MUST: имя отражает выдаваемое поведение в 1-3 словах.
4. SHOULD: отдавать приоритет формам с прилагательным, например `ClickableComponent`.
5. MUST: если есть парная система, базовая часть имени системы и компонента совпадает.
6. MUST: в YAML-прототипе у `- type:` не писать суффикс `Component`.

### 2) Системы

1. MUST: имя системы заканчивать на `System`.
2. MUST: формат имени — `CamelCase`.
3. MUST: при наличии целевого компонента использовать ту же базовую часть (`XxxComponent` <-> `XxxSystem`).
4. MUST: если целевого компонента нет, имя системы описывает действие в 1-3 словах.

### 3) Dependency-поля

1. MUST: приватные зависимости начинать с `_`.
2. MUST: имя зависимости строить из базовой части типа без `System/Manager`.
3. MUST: `TransformSystem` -> `_transform`, `IPlayerManager` -> `_player`.
4. MUST: придерживаться каноничных коротких форм: `IGameTiming` -> `_timing`, `IRobustRandom` -> `_random`, `EntityWhitelistSystem` -> `_whitelist`.
5. SHOULD: избегать шумных вариантов вроде `_transformSystem`/`_playerManager`, если есть устоявшийся короткий алиас.

### 4) Prototype ID

1. MUST: формат ID — `CamelCase`.
2. MUST: при наследовании от `BaseXxx` базовое имя в дочернем ID писать без префикса `Base`.
3. MUST: при развитии цепочки наследования наращивать суффиксы справа (`Meat` -> `MeatCat`).
4. MUST: если сущность уникальна для форка и особенно если это форк-копия ванильной сущности, добавлять префикс форка (`ScpXxx`, `SunriseXxx`).
5. MUST NOT: использовать snake_case, kebab-case или lowercase ID для нового production-кода.

### 5) Name/Description в прототипах и локализации

1. MUST: `name` и `description` fallback в YAML писать на английском.
2. MUST: fallback-содержимое в YAML совпадает по смыслу с английской локализацией.
3. MUST: название сущности в локализации — с маленькой буквы, максимум 3 слова.
4. MUST: описание сущности — с заглавной буквы, максимум 3 предложения.
5. SHOULD: не использовать кавычки, кроме случаев обязательного экранирования.

### 6) Localization keys

1. MUST: обычные ключи локализации — `kebab-case` (`word1-word2-word3`).
2. MUST: для сущностей использовать формат `ent-MyEntity`, `.desc`, `.suffix`.
3. MUST: обычный ключ отражает смысл действия/состояния (`item-pick-up-start`).
4. SHOULD: новые вариации строить добавлением суффиксов к существующему ключу, а не созданием несвязанного нового дерева.

### 7) Localization content

1. MUST: содержимое пишется на языке конкретной локали.
2. MUST: name/desc сущностей писать в IC-стиле как внешнее наблюдаемое описание.
3. MUST: OOC-текст явно маркировать префиксом `OOC:`.
4. MUST NOT: смешивать внутриигровое описание с OOC-инструкциями без маркера.

### 8) Переменные в коде

1. MUST: формат — `camelCase` для локальных/параметров, приватные поля с `_`.
2. MUST: имя отражает суть данных.
3. MUST: если переменная хранит компонент, использовать базовую часть имени компонента (`ActiveScp096RageComponent` -> `scp096Rage`).
4. MUST NOT: использовать бессмысленные имена (`data`, `value2`, `tmp`) вне микро-области.

### 9) Имена файлов

1. MUST: `yml/ftl/swsl` — `snake_case`.
2. SHOULD: максимум 2 слова, если контекст уже выражен папками.
3. MUST: C# файл в `CamelCase`, имя совпадает с ключевым классом.
4. SHOULD: если в файле несколько классов/partial-частей, имя файла описывает подсекцию максимум в 2 слова.

## Decision Tree

1. Нужен новый ECS data-контейнер?
   Выбери базовое действие/свойство -> оформи `XxxComponent`.
2. Нужна логика к компоненту?
   Используй ту же базу -> `XxxSystem`.
3. Добавляешь компонент в прототип?
   Пиши `- type: Xxx` без `Component`.
4. Выбираешь алиас зависимости?
   Убери `System/Manager` -> сократи до каноничной формы (`_timing`, `_random`, `_transform`, `_player`, `_whitelist`).
5. Создаешь fork-only или fork-копию ванили?
   Добавь префикс форка в ID (`Scp*`, `Sunrise*`).
6. Создаешь локализационный ключ?
   Сущность: `ent-MyEntity`; обычная строка: `kebab-case`.

## Паттерны ✅

1. `ClickableComponent` + `ClickableSystem` с общей базовой частью.
2. `- type: Clickable` в YAML вместо `ClickableComponent`.
3. Алиас `IGameTiming` как `_timing`.
4. Алиас `IRobustRandom` как `_random`.
5. Алиас `TransformSystem` как `_transform`.
6. Алиас `IPlayerManager` как `_player`.
7. Алиас `EntityWhitelistSystem` как `_whitelist`.
8. ID в стиле `Scp096CryOut` с понятным доменным префиксом.
9. Форк-ID с префиксом `Sunrise*` для уникального контента форка.
10. `ent-BasePart = body part` как короткое внешнее имя сущности.
11. `armable-examine-armed` как обычный `kebab-case` ключ не-сущностной строки.
12. Приватное поле-компонент `scp096Rage` по базовой части типа.

## Анти-паттерны ❌

1. `Clickable` как имя C#-компонента без суффикса `Component`.
2. `ClickableComponentSystem` вместо `ClickableSystem`.
3. `- type: ClickableComponent` в YAML.
4. `TransformSystem` -> `_transformSystem` в коде при наличии каноничного `_transform`.
5. `IPlayerManager` -> `_playerManager` в геймплей-системе при возможности `_player`.
6. Новый prototype ID в snake_case или kebab-case.
7. Fork-копия ванильной сущности без форк-префикса.
8. Неанглийский `name/description` fallback в YAML.
9. Обычный локализационный ключ в `PascalCase` или с `_`.
10. OOC-подсказка в `.desc` без маркера `OOC:`.
11. Описание длиннее 3 предложений или имя длиннее 3 слов.
12. Приватные поля без `_` (кроме констант).

## Примеры из кода

### Пример 1: парный нейминг компонента и системы

```csharp
[RegisterComponent]
public sealed partial class ClickableComponent : Component
{
}

public sealed class ClickableSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transforms = default!;
}
```

Комментарий: базовая часть `Clickable` совпадает у пары `Component/System`; это каноничный связанный нейминг.

### Пример 2: корректный dependency alias для whitelist

```csharp
public sealed partial class ChangeNameInContainerSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
}
```

Комментарий: используется короткий каноничный алиас `_whitelist`, а не `_whitelistSystem`.

### Пример 3: корректный dependency alias для transform

```csharp
public sealed class EntityPickupAnimationSystem : EntitySystem
{
    [Dependency] private readonly TransformSystem _transform = default!;
}
```

Комментарий: база типа `Transform` переносится в `_transform`.

### Пример 4: корректный dependency alias для random/player

```csharp
public sealed class DrugOverlaySystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
}
```

Комментарий: `Manager`/`Random` нормализованы до устойчивых коротких имен.

### Пример 5: компонент в YAML без суффикса Component

```yaml
- type: entity
  id: FloorWaterEntity
  components:
  - type: Clickable
```

Комментарий: в YAML используется базовая часть имени компонента.

### Пример 6: fork-префикс + CamelCase для prototype ID

```yaml
- type: entity
  id: Scp096CryOut
  name: emit mournful scream
```

Комментарий: `Scp`-префикс и `CamelCase` соблюдены; имя действия на английском.

### Пример 7: Sunrise-префикс в форк-контенте

```yaml
- type: guideEntry
  id: SunriseAmmunition
  name: guide-entry-ammunition
```

Комментарий: форк-идентификатор явно отделен от ванильного пространства имен.

### Пример 8: `ent-*` ключи сущностей и lowercase имя

```ftl
ent-BasePart = body part
    .desc = { ent-BaseItem.desc }
```

Комментарий: имя сущности короткое и lowercase; описание задается отдельно через `.desc`.

### Пример 9: обычные ключи в kebab-case

```ftl
armable-examine-armed = {CAPITALIZE(THE($name))} is [color=red]armed[/color].
armable-examine-not-armed = {CAPITALIZE(THE($name))} needs to be armed.
```

Комментарий: не-сущностные ключи идут в `kebab-case`.

## Чеклист перед PR

1. Все новые C# типы и поля названы по правилам выше.
2. В YAML нет `...Component` после `- type:`.
3. Prototype ID в `CamelCase`, форк-контент имеет корректный префикс.
4. FTL-ключи разделены на `ent-*` и `kebab-case` по назначению.
5. `name/description` fallback в YAML — на английском и согласованы с английской локалью.
6. Нет OOC-текста без `OOC:` маркера.

## Правило расширения

1. Добавляй новый паттерн только если он подтвержден свежим кодом и не конфликтует с текущим MUST-стандартом.
2. Любое исключение сначала фиксируй как анти-паттерн/legacy в `references/rejected-snippets.md`.
3. Если появляется отдельная большая тема (например, только naming UI/XAML), выноси ее в отдельный skill и оставляй тут только кросс-ссылку.

Держи нейминг предсказуемым и однообразным: так ревью быстрее, а регрессий меньше 🚀

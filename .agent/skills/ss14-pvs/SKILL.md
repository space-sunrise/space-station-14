---
name: SS14 PVS (Potentially Visible Set)
description: Architecture guide for PVS in Space Station 14 — chunk-based spatial partitioning, visibility determination, override types, budgets, Level-of-Detail, leave mechanics, visibility masks, and ExpandPvsEvent
---

# PVS — Potentially Visible Set

## Что такое PVS

PVS (Potentially Visible Set) — серверная система, определяющая **какие сущности видит каждый клиент**. Вместо отправки всего мира каждому игроку, сервер фильтрует данные по расстоянию, видимости и приоритету.

**Зачем это нужно:**
- **Экономия трафика** — отправлять 500 сущностей вместо 50 000
- **Защита от читов** — клиент не получает данные, которые не должен видеть
- **Производительность** — клиент обрабатывает только видимые сущности

## Пространственное разбиение на чанки

### Структура чанков

Мир разделён на **чанки** размером 8×8 единиц. Каждый чанк привязан к **корневой сущности** (карта или грид):

```
┌────────────────────────────────────────────────┐
│  Карта (MapEntity)                             │
│  ┌────┬────┬────┬────┐                         │
│  │ 0,0│ 1,0│ 2,0│ 3,0│  ← Чанки карты         │
│  ├────┼────┼────┼────┤                         │
│  │ 0,1│ 1,1│ 2,1│ 3,1│                         │
│  └────┴────┴────┴────┘                         │
│       ┌────┬────┬────┐                         │
│       │ 0,0│ 1,0│ 2,0│  ← Чанки грида          │
│       ├────┼────┼────┤    (отдельная сетка)     │
│       │ 0,1│ 1,1│ 2,1│                         │
│       └────┴────┴────┘                         │
└────────────────────────────────────────────────┘
```

### PvsChunkLocation

Чанк однозначно идентифицируется парой `(EntityUid root, Vector2i indices)`:
- **root** — `EntityUid` карты или грида, к которому привязан чанк
- **indices** — координаты чанка в сетке (`position / 8`, округлённые вниз)

### Содержимое чанка

`PvsChunk` хранит **отсортированный** список всех сущностей:

1. Сначала — сущности с флагом `MetaDataFlags.PvsPriority`
2. Затем — заякоренные (anchored) сущности
3. Затем — остальные прямые дети
4. Затем — дети детей (внуки)
5. Затем — все остальные потомки рекурсивно

Этот порядок используется для **Level-of-Detail** (LoD) — дальние чанки могут отправлять только часть содержимого.

### Dirty-механика чанков

Чанк помечается как **dirty** когда:
- Сущность добавляется в чанк
- Сущность удаляется из чанка
- Сущность перемещается между чанками
- Сущность меняет родителя

При следующем PVS-обновлении dirty-чанк перестраивает свой список `Contents`.

## Определение видимости

### Viewers (наблюдатели)

У каждой сессии есть один или несколько **viewers** — сущностей, глазами которых игрок видит мир:

- **Основной** — `session.AttachedEntity` (персонаж игрока)
- **Подписки** — `session.ViewSubscriptions` (камеры наблюдения, призраки и т.д.)

Основной viewer всегда обрабатывается первым для приоритизации.

### ViewBounds

Для каждого viewer вычисляется прямоугольник видимости:

- **Позиция** — мировые координаты viewer + `EyeComponent.Offset`
- **Размер** — `NetPvsPriorityRange` × `EyeComponent.PvsScale`

Чанки, пересекающие этот прямоугольник, считаются видимыми.

### Порядок обработки чанков

Видимые чанки **сортируются по расстоянию** к ближайшему viewer. Это означает:
- Ближние чанки обрабатываются первыми
- Если бюджет исчерпан, дальние сущности просто не войдут в состояние
- Это создаёт эффект «загрузки мира от центра»

## Level-of-Detail (LoD)

PVS использует простую систему LoD на основе расстояния:

| LoD уровень | Что отправляется | Когда |
|-------------|-----------------|-------|
| 0 | Только `PvsPriority` сущности | Очень далеко |
| 1 | + Заякоренные сущности | Далеко |
| 2 | + Все прямые дети чанка | Средне |
| 3 | + Дети детей (1 уровень вложенности) | Близко |
| 4 | Все сущности | Внутри обычного `NetMaxUpdateRange` |

Сущности с `MetaDataFlags.PvsPriority` видны **на любом расстоянии** в пределах PVS — это важно для стен, дверей и других объектов окклюзии.

## Бюджет (PVS Budget)

Сервер ограничивает количество **новых** сущностей, отправляемых клиенту за тик:

### Параметры бюджета

- **EnterLimit** (CVar: `net.pvs_entity_enter_budget`) — максимум сущностей, впервые или повторно входящих в PVS за тик
- **NewLimit** (CVar: `net.pvs_entity_budget`) — максимум сущностей, которых клиент **никогда не видел**

### Как это работает

```
Каждый тик для каждого клиента:

1. Сначала обрабатываются ForceSend сущности → бюджет НЕ применяется
2. Устанавливается реальный бюджет из CVar
3. Обрабатываются overrides → бюджет применяется
4. Обрабатываются видимые чанки → бюджет применяется

Если бюджет исчерпан:
  → Сущность не отправляется в этом тике
  → Она попадёт в следующий тик (если ещё видима)
```

### Определение «входящей» сущности

Сущность считается «входящей» в PVS если:
- Клиент видит её впервые (`EntityLastAcked == 0`)
- Она не была в предыдущем кадре (`LastSeen != CurTick - 1`)
- Она не была в последнем подтверждённом кадре (`EntityLastAcked < FromTick`)
- Она покинула и вернулась в PVS (`LastLeftView >= FromTick`)

## Типы PVS Override

### Иерархия переопределений

```
┌───────────────────────────────────────────────────────┐
│ ForceSend (глобальный)                                │
│ • Отправляется ВСЕМ клиентам                          │
│ • Игнорирует бюджет                                   │
│ • Игнорирует маску видимости                          │
│ • НЕ отправляет дочерние                              │
│ Пример: карты, гриды                                  │
├───────────────────────────────────────────────────────┤
│ ForceSend (per-session)                               │
│ • Как глобальный, но для одного клиента               │
│ Пример: (используется редко)                          │
├───────────────────────────────────────────────────────┤
│ GlobalOverride                                        │
│ • Отправляется ВСЕМ клиентам                          │
│ • Подчиняется бюджету                                 │
│ • Подчиняется маске видимости                         │
│ • Отправляет сущность + родителей + дочерних          │
│ Пример: станция, сингулярность, взрывы                │
├───────────────────────────────────────────────────────┤
│ SessionOverride                                       │
│ • Отправляется конкретному клиенту                    │
│ • Подчиняется бюджету                                 │
│ • Подчиняется маске видимости                         │
│ • Отправляет сущность + родителей + дочерних          │
│ Пример: разумы, SCP-096 для конкретного цели          │
└───────────────────────────────────────────────────────┘
```

### API — SharedPvsOverrideSystem

```csharp
// Инжектим систему
[Dependency] private readonly SharedPvsOverrideSystem _pvsOverride = default!;

// === GlobalOverride ===
// Сущность и все дети видны всем. Уважает маску видимости и бюджет.
_pvsOverride.AddGlobalOverride(uid);
_pvsOverride.RemoveGlobalOverride(uid);

// === SessionOverride ===
// Сущность и все дети видны конкретному игроку.
_pvsOverride.AddSessionOverride(uid, session);
_pvsOverride.RemoveSessionOverride(uid, session);

// === SessionOverrides через Filter ===
// Сущность видна нескольким игрокам через фильтр.
_pvsOverride.AddSessionOverrides(uid, filter);
```

### API — PvsOverrideSystem (серверный)

На сервере доступны **дополнительные** методы через `PvsOverrideSystem`:

```csharp
[Dependency] private readonly PvsOverrideSystem _pvsOverride = default!;

// === ForceSend (глобальный) ===
// Критичная сущность — игнорирует бюджет и маску видимости.
// НЕ отправляет дочерних сущностей.
_pvsOverride.AddForceSend(uid);
_pvsOverride.RemoveForceSend(uid);

// === ForceSend (per-session) ===
_pvsOverride.AddForceSend(uid, session);
_pvsOverride.RemoveForceSend(uid, session);
```

### Когда что использовать

| Ситуация | Метод |
|----------|-------|
| Глобальный контроллер (станция, очки) | `AddGlobalOverride` |
| Взрыв, видимый всем | `AddGlobalOverride` |
| Инвентарь/разум игрока | `AddSessionOverride` |
| Камера наблюдения | `AddSessionOverride` |
| SCP, видимый конкретным целям | `AddSessionOverride` / `AddSessionOverrides` |
| Карта / грид (критичен для мира) | `AddForceSend` (системный) |

### Автоматические ForceSend

Движок автоматически добавляет в `ForceSend`:
- **Все карты** при создании (`OnMapCreated`)
- **Все гриды** при создании (`OnGridCreated`)
- **Viewers** сессии — сущность игрока и камеры всегда отправляются владельцу

## ExpandPvsEvent

Для **динамического** расширения PVS используется `ExpandPvsEvent`. Рейзится на `AttachedEntity` сессии каждый тик:

```csharp
[ByRefEvent]
public struct ExpandPvsEvent
{
    public readonly ICommonSession Session;
    public List<EntityUid>? Entities;           // добавить эти сущности
    public List<EntityUid>? RecursiveEntities;  // добавить эти + всех детей
    public int VisMask;                         // маска видимости для всей сессии
}
```

### Использование

```csharp
// Подписка на событие
SubscribeLocalEvent<MyComponent, ExpandPvsEvent>(OnExpandPvs);

private void OnExpandPvs(EntityUid uid, MyComponent comp, ref ExpandPvsEvent args)
{
    // Добавить удалённую сущность в PVS этого клиента
    args.Entities ??= new();
    args.Entities.Add(comp.RemoteEntity);

    // Или добавить сущность со всеми детьми
    args.RecursiveEntities ??= new();
    args.RecursiveEntities.Add(comp.Container);
}
```

**Важно:** `ExpandPvsEvent` уважает маски видимости и PVS бюджет (в отличие от `ForceSend`).

## Маски видимости (Visibility Masks)

### Как работают маски

Каждая сущность имеет `MetaDataComponent.VisibilityMask` (по умолчанию = 1).
Каждый viewer имеет `EyeComponent.VisibilityMask` (по умолчанию = 1).

Сущность отправляется клиенту **только если** битовая маска viewer'а **содержит** все биты маски сущности:

```
(eyeMask & entityMask) == entityMask  // true → видна
```

### Примеры

```csharp
// Сущность видна только призракам
meta.VisibilityMask = (int)VisibilityFlags.Ghost;

// Eye видит и обычные сущности, и призрачные
eye.VisibilityMask = (int)(VisibilityFlags.Normal | VisibilityFlags.Ghost);
```

### Слои видимости (VisibilityMaskLayer)

Слои определяются в прототипах и используются как именованные флаги. Стандартные слои определяются в `VisibilityFlags`:
- `Normal` (1) — видно всем по умолчанию
- `Ghost` — видно только призракам
- Кастомные слои для специфической механики

### Маски и PVS overrides

- **GlobalOverride** и **SessionOverride** — **уважают** маски видимости
- **ForceSend** — **игнорирует** маски видимости
- **ExpandPvsEvent** — можно изменить маску через поле `VisMask`

## Жизненный цикл видимости сущности

### Вход в PVS

```
1. Сервер определяет, что чанк сущности попал в ViewBounds viewer'а
2. Проверяется маска видимости
3. Проверяется бюджет (EnterLimit, NewLimit)
4. Если бюджет не исчерпан:
   → Сущность добавляется в ToSend
   → Формируется EntityState (полное или дельта)
   → PvsData.LastSeen = CurTick
```

### Отправка бывшей сущности

Если сущность была ранее видна клиенту (EntityLastAcked > 0), при повторном входе сервер отправляет **дельта-состояние** от последнего подтверждённого тика, а не полное состояние. Это экономит трафик.

### Выход из PVS

```
1. ProcessLeavePvs() проверяет LastSent список
2. Сущности, у которых LastSeen != текущий тик → покинули PVS
3. Формируется MsgStateLeavePvs (отправляется RELIABLY)
4. PvsData.LastLeftView = текущий тик
5. Клиент получает MsgStateLeavePvs:
   → Устанавливает MetaDataFlags.Detached
   → Перемещает сущность в null-space
   → Сущность НЕ удаляется — остаётся в памяти
```

### Detached-сущности на клиенте

Когда сущность покидает PVS:
- Флаг `MetaDataFlags.Detached` устанавливается
- Сущность убирается из broadphase (физики) и рендерера
- При **повторном входе** в PVS:
  - Флаг снимается
  - Сущность возвращается на свою позицию
  - Состояние обновляется до актуального

## Отслеживание данных на стороне сервера

### PvsData (per-entity, per-session)

Для каждой пары (сущность, клиент) сервер хранит:

| Поле | Описание |
|------|----------|
| `LastSeen` | Тик, когда сущность последний раз отправлялась клиенту |
| `LastLeftView` | Тик, когда сущность покинула PVS клиента |
| `EntityLastAcked` | Тик последнего подтверждённого клиентом состояния, включавшего эту сущность |

### PvsSession (per-session)

Для каждой сессии хранится:

| Поле | Описание |
|------|----------|
| `VisMask` | Объединённая маска видимости всех viewers |
| `Viewers` | Список сущностей-наблюдателей |
| `Budget` | Текущий бюджет (NewLimit, EnterLimit, счётчики) |
| `ToSend` | Список сущностей для отправки в текущем тике |
| `States` | Сформированные EntityState для GameState |
| `FromTick` | Тик, от которого считаются дельты |
| `LastReceivedAck` | Последний подтверждённый клиентом тик |
| `RequestedFull` | Клиент запросил полное состояние |
| `Chunks` | Видимые чанки, отсортированные по расстоянию |

## Конвейер обработки PVS (за один тик)

```
SendGameStates(players)
│
├─ ProcessDisconnections()    — обработка отключений
├─ CacheSessionData(players)  — инициализация PvsSession
│
├─ BeforeSerializeStates()
│  ├─ ProcessQueuedAcks()     — обработка подтверждений
│  ├─ GetVisibleChunks()      — определение видимых чанков
│  └─ ProcessVisibleChunks()  — обновление dirty-чанков + кеш overrides
│
├─ SerializeStates()          — для каждого игрока параллельно:
│  ├─ UpdateSession()         — обновить VisMask, Viewers, сортировка чанков
│  ├─ AddForcedEntities()     — добавить ForceSend (без бюджета)
│  ├─ AddAllOverrides()       — добавить GlobalOverride + SessionOverride
│  ├─ ExpandPvsEvent          — динамическое расширение PVS
│  ├─ AddPvsChunks()          — добавить сущности из видимых чанков
│  └─ ComposeGameState()      — собрать GameState из States
│
├─ SendStates()               — сжать и отправить MsgState
├─ AfterSerializeStates()     — очистка dirty-буферов, cull истории удалений
└─ ProcessLeavePvs()          — обнаружить и отправить MsgStateLeavePvs
```

## CVars для настройки PVS

### Основные

| CVar | По умолчанию | Описание |
|------|-------------|----------|
| `net.pvs` | `true` | Включить/выключить PVS |
| `net.maxupdaterange` | `12.5` | Радиус видимости в единицах |
| `net.pvs_priority_range` | зависит | Радиус приоритетного обзора |
| `net.pvs_entity_budget` | `50` | Макс. **новых** сущностей за тик |
| `net.pvs_entity_enter_budget` | `80` | Макс. **входящих** сущностей за тик |

### Отладка

```
// Отключить PVS — отправлять всё всем (для разработки)
net.pvs false

// Увеличить радиус видимости
net.maxupdaterange 50

// Увеличить бюджет (меньше pop-in)
net.pvs_entity_budget 200
net.pvs_entity_enter_budget 200
```

### Диагностическая команда

```
pvs_override_info <NetEntity>  // Показать PVS override информацию для сущности
```

## Частые ошибки

### 1. Забыли добавить override для UI-сущности

```csharp
// ❌ Сущность пропадёт, если игрок далеко
var ui = Spawn("UiEntity", coordinates);

// ✅ Добавить override, чтобы UI всегда было видно
var ui = Spawn("UiEntity", coordinates);
_pvsOverride.AddSessionOverride(ui, session);
```

### 2. Использовали ForceSend вместо GlobalOverride

```csharp
// ❌ ForceSend игнорирует бюджет и видимость — перегрузка при массовом использовании
_pvsOverride.AddForceSend(uid);

// ✅ GlobalOverride уважает бюджет — безопаснее для большинства случаев
_pvsOverride.AddGlobalOverride(uid);
```

### 3. Не очистили override при удалении

```csharp
// ✅ Движок автоматически очищает overrides при удалении сущности
// Но если вы вручную управляете временными overrides — удаляйте сами
_pvsOverride.RemoveSessionOverride(uid, session);
```

### 4. Не учли видимость дочерних сущностей

```csharp
// GlobalOverride и SessionOverride рекурсивно добавляют детей.
// ForceSend НЕ добавляет детей!

// ❌ Если контейнер в ForceSend, предметы внутри могут быть не видны
_pvsOverride.AddForceSend(containerUid);

// ✅ Для контейнеров используйте GlobalOverride или SessionOverride
_pvsOverride.AddGlobalOverride(containerUid);
```

### 5. Сущность прыгает при re-entry

Когда сущность возвращается в PVS, клиент может увидеть «прыжок» позиции. Это нормальное поведение — сущность обновляется до актуального состояния мгновенно.

Уменьшить эффект можно:
- Увеличив `net.maxupdaterange` (больше радиус → меньше прыжков)
- Увеличив `net.pvs_entity_enter_budget` (больше бюджет → быстрее загрузка)

## Связь с другими скиллами

- **SS14 Netcode Architecture** — как PVS интегрируется в общий сетевой стек
- **SS14 Prediction** — как предикция работает с PVS detach/enter
- **SS14 ECS Components** — `[NetworkedComponent]`, `Dirty()`, `MetaDataFlags`
- **SS14 ECS Entities** — `EntityUid` vs `NetEntity`, жизненный цикл сущностей
- **SS14 ECS Systems** — интеграция PVS overrides через системы

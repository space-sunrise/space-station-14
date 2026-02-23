---
name: SS14 ECS Entities
description: Working with entities in Space Station 14 — EntityUid, Entity<T>, component operations, containers, network identity, and entity lifecycle
---

# Entity — сущности в ECS

## Что такое Entity

Сущность (Entity) — это **уникальный идентификатор** (`EntityUid`), к которому прикрепляются компоненты. Сущность сама по себе не содержит данных и логики — это просто числовой ID. Компоненты определяют свойства сущности, системы определяют её поведение.

## EntityUid

Основной идентификатор сущности — value type, сравнивается по значению:

```csharp
EntityUid uid = args.User;

// Проверка валидности
if (!uid.IsValid())
    return;

// EntityUid.Invalid — невалидный ID
if (uid == EntityUid.Invalid)
    return;
```

## Entity\<T\> — тупл сущности и компонента

`Entity<T>` — основной способ одновременной передачи `EntityUid` и компонента:

```csharp
// Создание
Entity<MyComponent> ent = (uid, myComp);

// Доступ
EntityUid owner = ent.Owner;
MyComponent comp = ent.Comp;

// Деконструкция
var (entityUid, component) = ent;
```

### Entity\<T?\> — nullable вариант

Используется когда компонент может отсутствовать. Паттерн `Resolve` — система сама получит компонент:

```csharp
public void SetSpeed(Entity<MyComponent?> ent, float speed)
{
    // Если ent.Comp == null, Resolve попробует получить его
    if (!Resolve(ent, ref ent.Comp))
        return;

    ent.Comp.Speed = speed;
    Dirty(ent);
}

// Вызов — можно передать с или без компонента:
SetSpeed((uid, myComp), 5f);    // с компонентом
SetSpeed((uid, null), 5f);       // Resolve сам получит
```

### AsNullable

Преобразование `Entity<T>` в `Entity<T?>`:

```csharp
Entity<MyComponent> ent = (uid, comp);
Entity<MyComponent?> nullable = ent.AsNullable();
```

## NetEntity — сетевой идентификатор

`NetEntity` — это сетевая версия `EntityUid`. Используется при передаче данных по сети:

```csharp
// Конвертация EntityUid → NetEntity (для отправки по сети)
NetEntity netEnt = GetNetEntity(uid);

// Конвертация NetEntity → EntityUid (при получении из сети)
EntityUid uid = GetEntity(netEnt);

// Nullable варианты
EntityUid? uid = GetEntity(netEnt);
NetEntity? netEnt = GetNetEntity(uid);
```

## Создание и удаление сущностей

```csharp
// Создание по прототипу
EntityUid newEntity = Spawn("PrototypeName", Transform(uid).Coordinates);

// Создание с позицией
EntityUid newEntity = Spawn("PrototypeName", new EntityCoordinates(mapUid, position));

// Удаление (немедленное)
Del(uid);

// Удаление (отложенное, безопасное в обработчиках событий)
QueueDel(uid);
```

## Работа с компонентами на сущности

### Добавление

```csharp
// Добавить компонент (бросит исключение если уже есть)
AddComp<MyComponent>(uid);

// Гарантированно получить компонент (добавит если нет)
var comp = EnsureComp<MyComponent>(uid);
```

### Получение

```csharp
// Безопасное получение
if (TryComp<MyComponent>(uid, out var comp))
{
    // comp доступен
}

// Гарантированное получение (бросит исключение если нет)
var comp = Comp<MyComponent>(uid);
```

### Проверка наличия

```csharp
if (HasComp<MyComponent>(uid))
{
    // Компонент есть
}
```

### Удаление

```csharp
// Немедленное удаление
RemComp<MyComponent>(uid);

// Отложенное удаление (безопасно в обработчиках событий)
RemCompDeferred<MyComponent>(uid);
```

## EntityQueryEnumerator — итерация по сущностям

Наиболее эффективный способ найти все сущности с определённым набором компонентов:

```csharp
// Один компонент
var query = EntityQueryEnumerator<MyComponent>();
while (query.MoveNext(out var uid, out var comp))
{
    // Обработка каждой сущности с MyComponent
}

// Два компонента
var query = EntityQueryEnumerator<MyComponent, TransformComponent>();
while (query.MoveNext(out var uid, out var myComp, out var xform))
{
    // Только сущности, имеющие ОБА компонента
}

// Три компонента
var query = EntityQueryEnumerator<CompA, CompB, CompC>();
while (query.MoveNext(out var uid, out var a, out var b, out var c))
{
    // ...
}
```

## Встроенные хелперы EntitySystem

Каждая система наследует множество удобных методов от `EntitySystem`:

```csharp
// Получить TransformComponent
var xform = Transform(uid);

// Получить MetaDataComponent
var meta = MetaData(uid);

// Получить прототип сущности
var prototypeName = Prototype(uid)?.ID;

// Отладочная строка
var debugStr = ToPrettyString(uid);  // → "Scp096 (1234)"
```

## Контейнерная система

Сущности могут содержать другие сущности через контейнеры:

### Определение в компоненте

```csharp
[RegisterComponent]
public sealed partial class MyContainerComponent : Component
{
    // Контейнеры создаются через ContainerContainer в YAML прототипе
}
```

### Определение в YAML

```yaml
- type: ContainerContainer
  containers:
    my_slot: !type:ContainerSlot
    storage: !type:Container
```

### Работа в системе

```csharp
[Dependency] private readonly SharedContainerSystem _container = default!;

// Получить контейнер
if (_container.TryGetContainer(uid, "my_slot", out var container))
{
    // Вставить сущность
    _container.Insert(entityToInsert, container);

    // Извлечь
    _container.Remove(entityToRemove, container);
}
```

## MetaData — метаинформация сущности

```csharp
var meta = MetaData(uid);

// Имя сущности
string name = meta.EntityName;

// Описание
string desc = meta.EntityDescription;

// Флаги
_meta.AddFlag(uid, MetaDataFlags.PvsPriority, meta);
_meta.RemoveFlag(uid, MetaDataFlags.PvsPriority, meta);
```

## Проверки состояния сущности

```csharp
// Существует ли сущность
if (Exists(uid))
{
    // ...
}

// Удалена ли сущность
if (Deleted(uid))
{
    return;
}

// Проверка, что сущность ещё на стадии инициализации
if (LifeStage(uid) < EntityLifeStage.MapInitialized)
{
    // ...
}

// Клиентская ли сущность (не имеет серверного аналога)
if (IsClientSide(uid))
{
    // ...
}
```

## Паттерны работы с Entity

### Передача Entity\<T\> в методы системы

```csharp
// Предпочтительный формат публичного API
public void DoSomething(Entity<MyComponent?> ent, float value)
{
    if (!Resolve(ent, ref ent.Comp))
        return;

    ent.Comp.Value = value;
    Dirty(ent);
}

// Внутренний метод — когда компонент гарантированно есть
private void DoInternal(Entity<MyComponent> ent)
{
    ent.Comp.Value = 42;
    Dirty(ent);
}
```

### HashSet\<EntityUid\> для отслеживания

```csharp
// В компоненте
[AutoNetworkedField]
public HashSet<EntityUid> TrackedEntities = new();

// В системе
comp.TrackedEntities.Add(targetUid);
comp.TrackedEntities.Remove(targetUid);
Dirty(uid, comp);
```

### Проверка с множественными компонентами — паттерн раннего возврата

**Не нагромождайте условия** в один `if`. Используйте ранний возврат при отсутствии компонента:

```csharp
// ✅ Правильно — ранний возврат
if (!TryComp<MyComponent>(uid, out var myComp))
    return;

if (!TryComp<TransformComponent>(uid, out var xform))
    return;

if (!HasComp<RequiredMarker>(uid))
    return;

// Работа с сущностью — все компоненты гарантированно есть

// ❌ Неправильно — вложенные условия
if (TryComp<MyComponent>(uid, out var myComp) &&
    TryComp<TransformComponent>(uid, out var xform) &&
    HasComp<RequiredMarker>(uid))
{
    // ...
}
```

## Оптимизации работы с сущностями (дополнение)

### 1) Для частых проверок используй кешированный `EntityQuery<T>`

Если API системы часто дергает один и тот же компонент, кешируй query в `Initialize()`:

```csharp
private EntityQuery<TagComponent> _tagQuery;

public override void Initialize()
{
    _tagQuery = GetEntityQuery<TagComponent>();
}

public bool HasTagFast(EntityUid uid, ProtoId<TagPrototype> tag)
{
    return _tagQuery.TryComp(uid, out var comp) &&
           comp.Tags.Contains(tag);
}
```

Это уменьшает накладные расходы по сравнению с многократными общими проверками.

### 2) Удаляй лишние временные компоненты сразу после завершения роли

```csharp
if (timer.NextTrigger <= curTime)
{
    Trigger(uid, timer.User, timer.KeyOut);
    RemComp<ActiveTimerTriggerComponent>(uid); // Сущность больше не активна.
}
```

Идея простая: у сущности должны оставаться только реально используемые компоненты.  
Иначе она продолжит попадать в query и увеличивать стоимость перебора.

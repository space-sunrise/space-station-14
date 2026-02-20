---
name: SS14 ECS Systems
description: Architecture guide for EntitySystem in Space Station 14 — lifecycle, events, queries, networking, prediction, and partial class decomposition patterns
---

# EntitySystem — системы в ECS

## Граница ответственности

Этот skill покрывает устройство систем, lifecycle, события, query и предикцию.
Строгие naming-нормативы (суффикс `System`, парность имен `Component/System`, стиль dependency-алиасов, соглашения по именам файлов) ведутся в `ss14-naming-conventions`.
Если пример здесь конфликтует с `ss14-naming-conventions`, применяй `ss14-naming-conventions`.

## Что такое EntitySystem

EntitySystem — это синглтон-класс, который содержит **всю логику и поведение** для сущностей. В ECS-архитектуре компоненты хранят только данные, а системы оперируют этими данными. Системы автоматически создаются и управляются движком — не нужно их вручную регистрировать.

## Базовый жизненный цикл

```csharp
public sealed class MySystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        // Подписки на события, кеширование EntityQuery
    }

    public override void Shutdown()
    {
        base.Shutdown();
        // Очистка ресурсов (особенно важно на клиенте)
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        // Логика, выполняемая каждый тик
    }
}
```

- `Initialize()` — вызывается один раз при создании системы. Здесь подписываемся на события и кешируем запросы.
- `Shutdown()` — вызывается при уничтожении системы. На сервере — при завершении программы. На клиенте — при отключении от сервера, поэтому на клиенте крайне важно корректно очищать ресурсы.
- `Update(float frameTime)` — вызывается каждый тик. Используется для периодической логики (таймеры, итерация по сущностям).

## Dependency Injection

Системы получают зависимости через атрибут `[Dependency]`. Это работает как для других систем, так и для IoC-менеджеров:

```csharp
public sealed class MySystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
}
```

Зависимости разрешаются автоматически до вызова `Initialize()`. Всегда используйте `= default!` для подавления предупреждений компилятора.

## Порядок членов системы (обязательный)

Держи стабильный порядок блоков в каждом `*System`, чтобы код читался быстро и одинаково во всех подсистемах 🙂

1. Зависимости (`[Dependency]` поля).
2. Константы + `static readonly` поля.
3. Runtime-кэшированные поля и долгоживущие поля состояния.
4. `Initialize()`, `Shutdown()`.
5. Event handlers (`On...`, `Handle...`, сетевые и компонентные события).
6. Main logic (публичный/защищенный API системы).
7. Other code (override/специализированные методы, не helper-блок).
8. Helpers (маленькие private-методы для обслуживания логики).
9. Private nested classes / records / enums / прочее.

Не смешивай блоки между собой: helpers не поднимай выше event handlers, runtime-кэш не размазывай по файлу, private nested-типы держи внизу.

Пример:

```csharp
public sealed class ExampleSystem : EntitySystem
{
    // 1) Dependencies
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    // 2) Constants + static readonly
    private const float TimeoutSeconds = 1.0f;
    private static readonly ProtoId<TagPrototype> SpecialTag = "Special";

    // 3) Runtime cache/state
    private readonly Dictionary<EntityUid, TimeSpan> _cooldowns = new();

    // 4) Init/Shutdown
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MyComponent, ComponentInit>(OnInit);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _cooldowns.Clear();
    }

    // 5) Event handlers
    private void OnInit(Entity<MyComponent> ent, ref ComponentInit args)
    {
        _cooldowns[ent] = _timing.CurTime;
    }

    // 6) Main logic
    public bool TryActivate(EntityUid uid) => true;

    // 7) Other code
    public override void Update(float frameTime)
    {
        base.Update(frameTime);
    }

    // 8) Helpers
    private bool IsCoolingDown(EntityUid uid) => _cooldowns.ContainsKey(uid);

    // 9) Private nested types
    private sealed record DebugEntry(EntityUid Uid, TimeSpan Time);
}
```

## Подписка на события

### Directed Events (направленные)

Привязаны к конкретной сущности. Вызываются только если у сущности есть указанный компонент:

```csharp
public override void Initialize()
{
    SubscribeLocalEvent<MyComponent, InteractUsingEvent>(OnInteractUsing);
    SubscribeLocalEvent<MyComponent, ComponentStartup>(OnStartup);
    SubscribeLocalEvent<MyComponent, ComponentShutdown>(OnShutdown);
}

private void OnInteractUsing(Entity<MyComponent> ent, ref InteractUsingEvent args)
{
    // ent.Owner — EntityUid сущности
    // ent.Comp — MyComponent
    // args — данные события
}
```

### Broadcast Events (широковещательные)

Не привязаны к конкретной сущности. Вызываются для всех подписчиков:

```csharp
SubscribeLocalEvent<MyBroadcastEvent>(OnMyBroadcast);

private void OnMyBroadcast(MyBroadcastEvent args)
{
    // Обработка события
}
```

### Сортированные подписки

Можно указать порядок обработки события между системами:

```csharp
SubscribeLocalEvent<MyComponent, SomeEvent>(OnEvent, before: [typeof(OtherSystem)], after: [typeof(AnotherSystem)]);
```

### Lifestage Events (события жизненного цикла компонентов)

Наиболее частые подписки — на создание и удаление компонентов:

- `ComponentInit` — компонент инициализирован
- `ComponentStartup` — компонент запущен
- `ComponentShutdown` — компонент выключен
- `ComponentRemove` — компонент удаляется

```csharp
SubscribeLocalEvent<MyComponent, ComponentStartup>(OnStartup);
SubscribeLocalEvent<MyComponent, ComponentShutdown>(OnShutdown);
```

## Создание и вызов событий

### Directed Event

```csharp
var ev = new MyEvent(someData);
RaiseLocalEvent(uid, ref ev);
```

### Broadcast Event

```csharp
var ev = new MyBroadcastEvent();
RaiseLocalEvent(ev);
```

### Directed + Broadcast

```csharp
RaiseLocalEvent(uid, ref ev, broadcast: true);
```

## EntityQuery — эффективный доступ к компонентам

### Кеширование в Initialize

```csharp
private EntityQuery<TransformComponent> _xformQuery;
private EntityQuery<PhysicsComponent> _physicsQuery;

public override void Initialize()
{
    _xformQuery = GetEntityQuery<TransformComponent>();
    _physicsQuery = GetEntityQuery<PhysicsComponent>();
}
```

### Использование

```csharp
// Безопасное получение
if (_xformQuery.TryComp(uid, out var xform))
{
    // работаем с xform
}

// Гарантированное получение (выбросит исключение если нет)
var xform = _xformQuery.Comp(uid);

// Проверка наличия
if (_xformQuery.HasComp(uid))
{
    // ...
}
```

### EntityQueryEnumerator — итерация в Update

Когда нужно пройтись по всем сущностям с определённым набором компонентов:

```csharp
public override void Update(float frameTime)
{
    var query = EntityQueryEnumerator<MyComponent, TransformComponent>();
    while (query.MoveNext(out var uid, out var myComp, out var xform))
    {
        // Логика для каждой сущности
    }
}
```

Можно итерировать по одному, двум или трём компонентам одновременно.

## Предикция (Prediction)

Многие системы работают одновременно на клиенте и сервере для плавного отображения. Важные проверки:

```csharp
// Выполнить код только при первом предсказании (не при повторных)
if (!_timing.IsFirstTimePredicted)
    return;

// Не выполнять при применении серверного состояния
if (_timing.ApplyingState)
    return;

// Проверка, клиентская ли сущность
if (IsClientSide(uid))
    return;
```

## Сервер vs Клиент

```csharp
// Проверка стороны
if (_net.IsServer)
{
    // Серверная логика
}

if (_net.IsClient)
{
    // Клиентская логика
}
```

## Dirty-механизм — сетевая синхронизация

Когда изменяется компонент с `[AutoNetworkedField]`, нужно сообщить движку о необходимости синхронизации:

```csharp
// Пометить компонент как "грязный" для отправки по сети
Dirty(uid, component);

// Или через Entity<T>
Dirty(ent);
```

## Паттерн Shared/Server/Client

### Абстрактный Shared-класс

Общая логика размещается в `Content.Shared`:

```csharp
// Content.Shared
public abstract partial class SharedMySystem : EntitySystem
{
    // Общая логика, работающая и на сервере, и на клиенте
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MyComponent, SomeEvent>(OnSomeEvent);
    }

    // Виртуальный метод для переопределения
    protected virtual void OnSpecificAction(EntityUid uid, MyComponent comp)
    {
        // Базовая реализация
    }
}
```

### Серверная реализация

```csharp
// Content.Server
public sealed partial class MySystem : SharedMySystem
{
    public override void Initialize()
    {
        base.Initialize();
        // Серверные подписки: БД, PVS, спавн
        SubscribeLocalEvent<MyComponent, PlayerAttachedEvent>(OnPlayerAttached);
    }

    protected override void OnSpecificAction(EntityUid uid, MyComponent comp)
    {
        base.OnSpecificAction(uid, comp);
        // Серверная логика: PVS overrides, spawn entities
    }
}
```

### Клиентская реализация

```csharp
// Content.Client
public sealed partial class MySystem : SharedMySystem
{
    public override void Initialize()
    {
        base.Initialize();
        // Клиентские подписки: UI, визуалы, звуки
    }
}
```

## Partial-класс декомпозиция

Для сложных систем логика разбивается на несколько файлов через `partial class`. Каждый файл отвечает за свою подсистему:

```text
SharedMySystem.cs            — Initialize, базовые подписки, DI
SharedMySystem.Actions.cs    — способности и действия
SharedMySystem.Target.cs     — управление целями
SharedMySystem.State.cs      — переходы между состояниями
SharedMySystem.Appearance.cs — визуалы и анимации
```

Пример структуры:

```csharp
// SharedMySystem.cs
public abstract partial class SharedMySystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private EntityQuery<MyComponent> _query;

    public override void Initialize()
    {
        _query = GetEntityQuery<MyComponent>();
        InitializeActions();     // из Actions.cs
        InitializeTargets();     // из Target.cs
    }

    public override void Update(float frameTime)
    {
        UpdateState(frameTime);  // из State.cs
    }
}

// SharedMySystem.Actions.cs
public abstract partial class SharedMySystem
{
    private void InitializeActions()
    {
        SubscribeLocalEvent<MyComponent, MyActionEvent>(OnAction);
    }
}
```

## Паттерн generic-систем

Для обобщённых систем используется generic-подход:

```csharp
// Базовый обработчик для всех эффектов определённого типа
public abstract class EntityEffectSystem<T, TEffect> : EntitySystem
    where T : unmanaged
    where TEffect : EntityEffectBase<TEffect>
{
    public override void Initialize()
    {
        SubscribeLocalEvent<T, EntityEffectEvent<TEffect>>(OnEffect);
    }

    protected abstract void OnEffect(Entity<T> ent, ref EntityEffectEvent<TEffect> args);
}
```

## Интерфейсы для систем

Системы могут реализовывать интерфейсы для стандартизации поведения:

```csharp
public interface IEntityEffectRaiser
{
    void RaiseEvent(EntityUid target, EntityEffect effect, float scale, EntityUid? user);
}

public sealed partial class MyEffectsSystem : EntitySystem, IEntityEffectRaiser
{
    public void RaiseEvent(EntityUid target, EntityEffect effect, float scale, EntityUid? user)
    {
        effect.RaiseEvent(target, this, scale, user);
    }
}
```

## Публичные методы системы (API)

Системы предоставляют публичные методы для взаимодействия других систем:

```csharp
public void SetSpeed(Entity<MyComponent?> ent, float speed)
{
    if (!Resolve(ent, ref ent.Comp))
        return;

    ent.Comp.Speed = speed;
    Dirty(ent);

    // Дополнительная логика: обновить движение, отправить событие
    RaiseLocalEvent(ent, new SpeedChangedEvent(speed));
}
```

Паттерн `Entity<T?>` с `Resolve` позволяет вызывающему коду необязательно передавать компонент — система сама его получит.

## Логирование

Логи должны быть **только на английском языке** и содержать достаточно информации для разбора **после окончания раунда** (EntityUid уже не будут доступны, поэтому обязательно включайте прототип, имя и другие идентификаторы):

```csharp
// ✅ Правильно — английский, прототип, имя, контекст
Log.Warning("Failed to apply effect on {Entity} (proto: {Proto})",
    ToPrettyString(uid), Prototype(uid)?.ID ?? "unknown");

Log.Error("Target {Target} (proto: {Proto}) is out of range for {Source} (proto: {SourceProto}), distance: {Distance}",
    ToPrettyString(targetUid), Prototype(targetUid)?.ID ?? "unknown",
    ToPrettyString(sourceUid), Prototype(sourceUid)?.ID ?? "unknown",
    distance);

Log.Debug("State transition: {Entity} (proto: {Proto}) entered rage, targets: {Count}",
    ToPrettyString(uid), Prototype(uid)?.ID ?? "unknown", targets.Count);

// ❌ Неправильно — русский язык, недостаточно контекста
Log.Debug("Отладочное сообщение");
Log.Warning("Ошибка: {Entity}", ToPrettyString(uid));  // нет прототипа, нет контекста
```

## Типы событий

### Cancellable (отменяемые)

```csharp
public sealed class MyAttemptEvent : CancellableEntityEventArgs
{
    // Другие системы могут вызвать args.Cancel() чтобы отменить действие
}
```

### Handled (обрабатываемые)

```csharp
public sealed class MyHandledEvent : HandledEntityEventArgs
{
    // args.Handled = true; — помечает событие как обработанное
}
```

### By-ref struct (производительные)

```csharp
[ByRefEvent]
public record struct MyPerformantEvent(EntityUid Target, float Value);
```

`[ByRefEvent]` передаёт структуру по ссылке вместо копирования — используется для часто вызываемых событий. При использовании `[ByRefEvent]` в подписке параметр события должен быть с `ref`.

### Именование событий

- Имена всегда заканчиваются на `Event`: `InteractUsingEvent`, `DamageChangedEvent`
- Попытки: `AttemptEvent` / `Attempt`: `PickupAttemptEvent`
- Уведомления: описательное имя: `MobStateChangedEvent`, `StackCountChangedEvent`

## Оптимизации hot-path (дополнение)

### 1) Предвычисляй инварианты до вложенных циклов

```csharp
var fastPath = false;
var itemShape = ItemSystem.GetItemShape(itemEnt); // Получаем форму один раз.
var fastAngles = itemShape.Count == 1;

if (itemShape.Count == 1 && itemShape[0].Contains(Vector2i.Zero))
    fastPath = true;

var angles = new ValueList<Angle>();
if (!fastAngles)
{
    for (var angle = startAngle; angle <= Angle.FromDegrees(360 - startAngle); angle += Math.PI / 2f)
        angles.Add(angle); // Подготовили набор углов один раз.
}
else
{
    angles.Add(startAngle);
    if (itemShape[0].Width != itemShape[0].Height)
        angles.Add(startAngle + Angle.FromDegrees(90));
}

while (chunkEnumerator.MoveNext(out var storageChunk))
{
    for (var y = bottom; y <= top; y++)
    {
        for (var x = left; x <= right; x++)
        {
            foreach (var angle in angles)
            {
                // Основная тяжёлая проверка использует уже предвычисленные значения.
            }
        }
    }
}
```

### 1.1) Храни агрегат (`TargetCount`/`BurstShotsCount`) как состояние

```csharp
// Вместо пересчёта "сколько выстрелов уже сделано в burst" на каждом шаге:
if (gun.BurstActivated)
{
    gun.BurstShotsCount += shots; // Инкрементальный счётчик.
    if (gun.BurstShotsCount >= gun.ShotsPerBurstModified)
    {
        gun.BurstActivated = false;
        gun.BurstShotsCount = 0;
    }
}
```

### 2) Не используй LINQ в горячих циклах

```csharp
// ✅ Для hot-path: явный цикл и ранний выход.
for (var i = 0; i < entities.Count; i++)
{
    var uid = entities[i];
    if (!TryComp<MyComponent>(uid, out var comp))
        continue;
    Process(uid, comp);
}

// ❌ Избегай в hot-path:
// entities.Where(...).Select(...).ToList();
```

### 3) Порядок компонентов в `EntityQueryEnumerator`

Ставь первым более редкий компонент, чтобы сократить пересечение множеств:

```csharp
var query = EntityQueryEnumerator<ActiveTimerTriggerComponent, TimerTriggerComponent>();
```

### 4) Ранние `continue/return` обязательны для дешёвых фильтров

Сначала дешёвые проверки, потом дорогие вычисления/события. Это снижает среднюю цену итерации и уменьшает шум профайлера.

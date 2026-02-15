---
name: SS14 ECS Systems
description: Architecture guide for EntitySystem in Space Station 14 — lifecycle, events, queries, networking, prediction, and partial class decomposition patterns
---

# EntitySystem — системы в ECS

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

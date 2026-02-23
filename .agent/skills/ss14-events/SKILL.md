---
name: ss14-events
description: Руководство по использованию событий в Space Station 14 — строгая таксономия, подписки, приоритизация ссылочных (by-ref) событий и паттерны сетевого взаимодействия.
---

# 📨 Руководство по Событиям в SS14

События — это основной способ коммуникации между системами и сущностями в Space Station 14. 🚀 Этот гайд охватывает правильное определение, вызов и обработку событий с соблюдением стандартов движка.

## 📝 Определение Событий

### Локальные События (Local Events) 🏠
Для локальных событий (внутри одного клиента или сервера) используйте простую структуру `struct` или `class`.
*   **Structs**: Предпочтительны для высокочастотных событий (например, `MoveEvent`, `DamageEvent`) для избежания нагрузки на GC. 🏎️
*   **Classes**: Используйте для сложных данных или событий, требующих наследования (например, `ExamineEvent`). 📚
*   **Именование**: Суффикс `Event` обязателен (например, `DoorOpenedEvent`).

```csharp
// Простая структура события
public readonly record struct DoorOpenedEvent(EntityUid User);

// Класс события с выходными данными
public sealed class ExamineEvent : EntityEventArgs {
    public readonly EntityUid Examined;
    public FormattedMessage Message = new();
}
```

### Сетевые События (Network Events) 🌐
События, передаваемые по сети, **ОБЯЗАНЫ** наследовать `EntityEventArgs` и быть помечены атрибутами `[Serializable, NetSerializable]`.

```csharp
[Serializable, NetSerializable]
public sealed class RequestStationNameEvent : EntityEventArgs {
    public string NewName;
}
```

## 🔗 Подписка на События

Подписки всегда обрабатываются в `EntitySystem.Initialize()`.

### 1. Направленная Подписка (`SubscribeLocalEvent`) 🎯
Используйте, когда хотите слушать событие *на конкретной сущности*, имеющей определенный компонент.

**Современный формат:** Используйте обертку `Entity<T>` для доступа к компоненту и UID одновременно.

```csharp
public override void Initialize() {
    base.Initialize();
    SubscribeLocalEvent<DoorComponent, DoorOpenedEvent>(OnDoorOpened);
}

private void OnDoorOpened(Entity<DoorComponent> ent, ref DoorOpenedEvent args) {
    // ent.Owner - это EntityUid
    // ent.Comp - это DoorComponent
    if (ent.Comp.IsOpen) ...
}
```

### 2. Широковещательная Подписка (`SubscribeEvent`) 📢
Используйте для глобальных событий, не привязанных к конкретной сущности.

```csharp
SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
```

### 3. Сетевая Подписка (`SubscribeNetworkEvent`) 📡
Используйте для обработки событий, отправленных с другой стороны (Клиент -> Сервер или Сервер -> Клиент).

```csharp
SubscribeNetworkEvent<RequestStationNameEvent>(OnNameRequest);
```

## 🧩 Специфичные Паттерны

### 1. Отменяемые События (Cancellable Events) 🚫
Используются для проверки возможности выполнения действия ("Attempt" events). Любой подписчик может отменить действие.

*   **Классы**: Наследуйте от `CancellableEntityEventArgs`.
*   **Структуры**: Добавьте поле `public bool Cancelled;`.
*   **Важно**: Всегда передавайте такие события через `ref`, чтобы изменения `Cancelled` были видны вызывающему коду.

**Использование:**
```csharp
// Определение
public sealed class DisarmAttemptEvent : CancellableEntityEventArgs { }
```

```csharp
// Подписка (Блокировка действия)
private void OnDisarmAttempt(Entity<ScpRestrictionComponent> ent, ref DisarmAttemptEvent args) {
    if (!ent.Comp.CanBeDisarmed)
        args.Cancel(); // Или args.Cancelled = true;
}
```

```csharp
// Вызов (Проверка разрешения)
var attempt = new DisarmAttemptEvent();
RaiseLocalEvent(target, attempt);

if (attempt.Cancelled)
    return; // Действие прервано
```

### 2. Обработанные События (Handled Events) ✅
Используются, когда событие должно быть обработано только одной системой (например, взаимодействие с предметом). Если одна система "обработала" (handled) событие, другие не должны выполнять свою логику.

*   **Реализация**: Добавьте поле `public bool Handled;` (или наследуйте `HandledEntityEventArgs` для классов).

**Использование:**
```csharp
// Определение
[ByRefEvent]
public struct InteractEvent {
    public bool Handled;
}
```

```csharp
// Подписка
private void OnInteract(Entity<MyComponent> ent, ref InteractEvent args) {
    if (args.Handled) return; // Уже кем-то обработано

    // Выполняем логику
    args.Handled = true; // Помечаем как обработанное
}
```
**Важно**: Паттерн `Handled` отличается от `Cancelled`. `Cancelled` спрашивает разрешения ("Можно ли?"), а `Handled` говорит о факте свершения ("Я это сделал!").

## ⚡ Производительность: By-Ref Events

Для высоконагруженного кода, особенно часто вызываемых событий (физика, движение), используйте **By-Ref** (ссылочные) события. Это избегает копирования больших структур.

### Определение By-Ref События
Пометьте структуру атрибутом `[ByRefEvent]`.

```csharp
[ByRefEvent]
public struct MoveEvent {
    public EntityCoordinates OldPosition;
    public EntityCoordinates NewPosition;
}
```

### Подписка By-Ref
Вы **ОБЯЗАНЫ** использовать ключевое слово `ref` в сигнатуре обработчика. ⚠️

```csharp
SubscribeLocalEvent<PhysicsComponent, MoveEvent>(OnMove);
```

```csharp
private void OnMove(Entity<PhysicsComponent> ent, ref MoveEvent args) {
    // args передается по ссылке, изменения видны везде
}
```

## 📤 Вызов Событий

### Вызов Локальных Событий
Используйте `RaiseLocalEvent` из `EntitySystem`.

```csharp
// По значению (By Value)
RaiseLocalEvent(uid, new DoorOpenedEvent(user));
```

```csharp
// По ссылке (By Ref) - автоматически для помеченных [ByRefEvent]
var moveEv = new MoveEvent(oldPos, newPos);
RaiseLocalEvent(uid, ref moveEv);
```

## ❌ Антипаттерны и Частые Ошибки

### 1. 🚫 Устаревшая сигнатура обработчика
**Ошибка**: Использовать развернутую сигнатуру `(EntityUid uid, Component comp, args)`.
**Почему**: Это устаревший стиль. Новый стиль с `Entity<T>` чище и удобнее.
**Правильно**:
```csharp
// ✅ GOOD
private void OnEvent(Entity<MyComponent> ent, ref MyEvent args) { ... }
```

```csharp
// ❌ BAD
private void OnEvent(EntityUid uid, MyComponent component, MyEvent args) { ... }
```

### 2. 🚫 Подписка в `OnMapInit` или `Startup`
**Ошибка**: Подписываться на события внутри методов жизненного цикла компонента.
**Почему**: Это вызывает утечки памяти и дублирование подписок.
**Правильно**: Всегда подписывайтесь только в `Initialize()` вашей `EntitySystem`.

### 3. 🚫 Использование `CancellableEntityEventArgs` для Structs
**Ошибка**: Пытаться наследовать структуры от классов или использовать `CancellableEntityEventArgs` без необходимости.
**Почему**: Это создает лишние аллокации (boxing).
**Правильно**: Добавьте поле `bool Handled` или `bool Cancelled` прямо в структуру и передавайте её через `ref`.

### 4. 🚫 Тяжелая логика в конструкторах событий
**Ошибка**: Выполнять сложные вычисления в конструкторе события.
**Почему**: События создаются часто.
**Правильно**: Передавайте только готовые данные.

### 5. 🚫 Забытый `sealed` для классов событий
**Ошибка**: Создание класса события без `sealed`.
**Почему**: Мешает JIT-компилятору девиртуализировать вызовы, снижая производительность.
**Правильно**: Всегда пишите `public sealed class MyEvent`.

### 6. 🚫 Изменение `ref` аргументов без нужды
**Ошибка**: Изменять поля в `ref` событии, если вы не являетесь "ответственной" системой.
**Почему**: Это может сломать логику других систем, которые получат измененное событие.
**Правильно**: Изменяйте данные только если ваша система должна перехватить или модифицировать результат (например, броня уменьшает урон).

## Дополнение по производительности: `ByRef record struct`

Для частых локальных событий предпочитай этот формат:

```csharp
[ByRefEvent] public record struct ChargedMachineActivatedEvent;

private void RaiseActivated(EntityUid uid)
{
    var ev = new ChargedMachineActivatedEvent();
    RaiseLocalEvent(uid, ref ev); // Важно: ref обязателен.
}
```

### Почему это полезно

1. Меньше копирований событий в массовых потоках.
2. Стабильнее поведение в hot-path по сравнению с тяжёлыми классами-событиями.

### Анти-паттерн

```csharp
// ❌ Частое событие как класс + вызов без by-ref:
public sealed class FrequentEvent : EntityEventArgs { }
RaiseLocalEvent(uid, new FrequentEvent());
```

Используй классы там, где это действительно нужно по семантике, а не по привычке.

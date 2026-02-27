---
trigger: always_on
---

# Правило: Архитектурный паттерн OnEvent -> TryDo -> CanDo -> Do

Это правило описывает мандаторный архитектурный паттерн для реализации действий и взаимодействий в кодовой базе Space Station 14. Следование этому паттерну обеспечивает предсказуемость, переиспользуемость и чистоту кода.

Структура: `OnEvent()` -> `TryDoSomething()` -> (проверка) `if (!CanDoSomething()) return` -> `DoSomething()`

## 📝 Общая схема

Логика разбивается на три уровня ответственности:

1.  **Event Handler (`OnEvent`)**: Точка входа. Принимает событие, распаковывает данные и вызывает `Try`-метод.
2.  **Public API (`TryDo`)**: "Публичный интерфейс" действия. Оркестрирует проверку (`CanDo`) и исполнение. Возвращает успех/неудачу.
3.  **Check (`CanDo`)**: Чистая проверка условий. Определяет, *можно* ли совершить действие, но *не совершает* его.

---

## 🔍 Компоненты паттерна

### 1. Обработчик событий (`OnEvent`)
Метод, подписанный на событие (`SubscribeLocalEvent`).
*   **Задача**: Перенаправить поток исполнения в публичный API.
*   **Логика**: Минимальная. Только проверка валидности события (например, `args.Handled`) и вызов `Try...`.
*   **Название**: `On[EventName]`, `On[Action]`.

### 2. Попытка действия (`TryDoSomething`)
Публичный метод, доступный для вызова из других систем (API).
*   **Сигнатура**: `public bool TryAction(Entity<Component?> ent, ...)`
*   **Задача**:
    1.  Вызвать `CanDoSomething`. Если вернул `false` — вернуть `false`.
    2.  Если проверки пройдены — выполнить действие (изменить компонент, вызвать событие, проиграть звук и т.д.).
    3.  Вернуть `true` при успехе.
*   **Важно**: Если действие требует специфических аргументов (например, `user`), они должны быть переданы сюда.

### 3. Проверка возможности (`CanDoSomething`)
Метод, содержащий условия выполнения.
*   **Сигнатура**: `public bool CanAction(Entity<Component?> ent, ..., bool quiet = false)`
*   **Задача**: Проверить все условия (дистанция, наличие инструмента, статус компонента).
*   **Side Effects**:
    *   ❌ **ЗАПРЕЩЕНО** менять состояние сущностей (компонентов).
    *   ✅ **РАЗРЕШЕНО** отправлять сообщения игроку (Popups), если аргумент `quiet` равен `false`.

---

## ✅ Пример (Система взаимодействия с предметами)

Обрати внимание на четкое разделение ответственности. Этот пример показывает, как система обработки взятия предмета в руки (Wielding) реализует паттерн.

```csharp
// 1. Event Handler
// Получает событие использования предмета в руке.
// Если событие уже обработано - выход.
// Иначе вызывает публичный метод попытки действия.
private void OnUseInHand(EntityUid uid, WieldableComponent component, UseInHandEvent args)
{
    if (args.Handled)
        return;

    // Вызов публичного API
    // Обработчик не знает деталей реализации, он просто "просит" попытаться взять в руки.
    if (TryWield(uid, component, args.User))
        args.Handled = true;
}

// 2. Public API (TryDo)
// Публичный метод, который могут вызвать другие системы (например, магия или админ-панель).
public bool TryWield(EntityUid used, WieldableComponent component, EntityUid user)
{
    // Шаг 1: Проверка (Early Return)
    // Строго через вызов Can-метода.
    if (!CanWield(used, component, user))
        return false;

    // Шаг 2: Исполнение (Do)
    // Здесь мы уже уверены, что действие валидно.

    // Логика изменения состояния (компонент, визуализация)
    SetWielded((used, component), true);

    // Визуальные и звуковые эффекты
    if (component.WieldSound != null)
        _audio.PlayPredicted(component.WieldSound, used, user);

    // События (для реакции других систем)
    var ev = new ItemWieldedEvent(user);
    RaiseLocalEvent(used, ref ev);

    // Popup об успехе для игрока
    var message = Loc.GetString("wieldable-component-successful-wield", ("item", used));
    _popup.PopupPredicted(message, user, user);

    return true;
}

// 3. Check (CanDo)
// Чистая функция проверки. Не меняет состояние игры (кроме отправки сообщений при ошибке).
public bool CanWield(EntityUid uid, WieldableComponent component, EntityUid user, bool quiet = false)
{
    // Проверка 1: Есть ли руки?
    // Использует TryComp для безопасного получения зависимостей.
    if (!TryComp<HandsComponent>(user, out var hands))
    {
        if (!quiet) // Popup только если не quiet
            _popup.PopupClient(Loc.GetString("wieldable-component-no-hands"), user, user);
        return false;
    }

    // Проверка 2: Предмет в руках?
    if (!_hands.IsHolding((user, hands), uid, out _))
    {
        if (!quiet)
            _popup.PopupClient(Loc.GetString("wieldable-component-not-in-hands", ("item", uid)), user, user);
        return false;
    }

    // Проверка 3: Достаточно ли свободных рук?
    // Логика подсчета слотов.
    if (_hands.CountFreeableHands((user, hands), except: uid) < component.FreeHandsRequired)
    {
        if (!quiet)
            _popup.PopupClient(Loc.GetString("wieldable-component-not-enough-free-hands"), user, user);
        return false;
    }

    // Все проверки пройдены
    return true;
}
```

---

## ❌ Анти-паттерны (Чего избегать)

### "Толстый" Event Handler
Вся логика находится внутри `OnEvent`.
*   **Проблема**: Логику невозможно переиспользовать (например, вызвать из консольной команды или другого события `InteractionVerb`).
*   **Плохо**:
    ```csharp
    private void OnUse(EntityUid uid, Comp comp, UseEvent args) {
        if (!Condition) return; // Проверка смешана с логикой
        PerformAction();        // Прямое выполнение
    }
    ```

### Side-effects в `CanDo`
Метод `Can` изменяет данные компонента.
*   **Проблема**: Вызов проверки "просто чтоб узнать" ломает состояние игры.
*   **Плохо**:
    ```csharp
    public bool CanShoot(GunComponent gun) {
        gun.Ammo--; // ❌ НИКОГДА так не делай в проверке!
        return gun.Ammo >= 0;
    }
    ```

### "Слепой" `TryDo`
Метод `Try` не вызывает `Can`, а полагается на то, что вызывающий уже всё проверил.
*   **Проблема**: Нарушение инкапсуляции. API становится небезопасным. `Try` всегда должен гарантировать проверку условий.

### Возврат строки вместо bool в `CanDo`
Возврат кода ошибки или строки вместо `bool`.
*   **Совет**: Используй `out string? reason`, если нужно вернуть причину отказа, но сам метод должен возвращать `bool` для удобства использования в `if`.
    ```csharp
    public bool CanDoWield(..., [NotNullWhen(false)] out string? reason)
    ```

---

## 🎯 Преимущества схемы

1.  **API для других систем**: `TryWield` можно вызвать откуда угодно (из вербов, из магии, из админки), и он корректно отработает со всеми проверками.
2.  **Прогностика (Prediction)**: Разделение позволяет клиенту легко спредиктить результат `CanWield` для UI (например, задизейблить кнопку), не вызывая само действие.
3.  **Читаемость**: `OnEvent` становится тривиальным маршрутизатором, а бизнес-логика четко структурирована.

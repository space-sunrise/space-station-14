---
name: ss14-localization-code
description: Руководство по использованию локализации в C# коде Space Station 14. Описывает ILocalizationManager, LocId и правильные паттерны внедрения зависимостей.
---

# SS14 Localization in Code (Russian)

Этот скилл описывает правила работы с локализацией (`ILocalizationManager`) в C# коде Space Station 14.

## 1. Получение локализации: String vs LocId

### LocId (Localization Identifier)
В современном коде SS14 для хранения ключей локализации следует использовать структуру `LocId` вместо чистого `string`. Это позволяет статическим анализаторам проверять существование ключей.

```csharp
// ✅ Хорошо: Используем LocId в компонентах и событиях
[DataDefinition]
public partial struct ExaminedEvent
{
    public LocId Message;
}

// ❌ Плохо: Использование string для ключей локализации
public string Message;
```

### Форматирование строк
Для подстановки переменных в FTL сообщения используются кортежи `(string key, object value)`.

```csharp
// FTL:
// my-message = Привет, { $name }! У тебя { $count } монет.

// C#:
var msg = _loc.GetString("my-message", ("name", "Urist"), ("count", 10));
```

## 2. Использование LocalizationManager

Единственный верный способ работы с локализацией в системах (`EntitySystem`) и контроллерах — через внедрение зависимостей (Dependency Injection).

### ✅ Паттерн: Dependency Injection

```csharp
using Robust.Shared.Localization;

public sealed class MyNotSystem : SomeBaseClass
{
    // Внедряем менеджер через атрибут [Dependency]
    [Dependency] private readonly ILocalizationManager _loc = default!;

    public void DoSomething()
    {
        // Используем внедренный инстанс
        var text = _loc.GetString("my-localization-key");
    }
}
```

### 🚫 Анти-паттерн: Ручное Dependency Injection в EntitySystem
`EntitySystem` уже имеют `ILocalizationManager` с именем `Loc`. Дополнительно создавать его самому - НЕ ТРЕБУЕТСЯ

### 🚫 Анти-паттерн: Ручное разрешение зависимостей (Manual Resolve)
Никогда не используйте `IoCManager.Resolve<T>()` внутри систем или методов, где можно использовать `[Dependency]`. Это нарушает принцип инверсии управления и усложняет тестирование.

```csharp
// ❌ ОЧЕНЬ ПЛОХО
public void BadMethod()
{
    var loc = IoCManager.Resolve<ILocalizationManager>(); // НЕТ!
    loc.GetString("...");
}
```

### 🚫 Анти-паттерн: Статический класс Loc
Класс `Loc` является статической оберткой над `ILocalizationManager`. Его использование в `EntitySystem` считается **устаревшим** (deprecated) и нежелательным, так как это скрытая зависимость.

```csharp
// ❌ Плохо (внутри систем)
var text = Loc.GetString("my-key");

// ✅ Хорошо
var text = _loc.GetString("my-key");
```

**Исключение:** Статический `Loc` допустим только в тех местах, где невозможно внедрение зависимостей (например, статические утилитные методы, методы расширения без доступа к IoC), но даже там лучше передавать `ILocalizationManager` как аргумент метода.

### 🚫 Анти-паттерн: Конкатенация строк
Никогда не склеивайте локализованные строки с переменными через `+` или `$` (интерполяцию).
Порядок слов в разных языках отличается (SVO vs SOV). Fluent поддерживает безопасную подстановку аргументов.

```csharp
// ❌ ПЛОХО: Ломает грамматику других языков
var text = "Игрок " + _loc.GetString("traitor-title") + " победил!";

// ✅ ХОРОШО: Передаем аргументы в FTL
// traitor-win-msg = Игрок { $role } победил!
var text = _loc.GetString("traitor-win-msg", ("role", roleName));
```

## 3. Автоматическая локализация сущностей

Вам не нужно вручную получать имя сущности через `_loc.GetString("ent-...")`.
Свойства `Name` и `Description` в `EntityPrototype` и компоненте `MetaData` уже делают это за вас.

```csharp
// Получение локализованного имени сущности
var name = Identity.Name(uid, EntityManager); // Учитывает ID карты, маскировку и т.д.
// ИЛИ (сырое имя прототипа)
var protoName = prototype.Name; // Уже локализовано
```

## 4. Грамматические атрибуты (Gender)

При передаче сущностей в сообщения локализации, движок автоматически пытается определить пол и имя. Чтобы это работало корректно, передавайте саму сущность (`EntityUid`), а не просто её имя строкой.

```csharp
// FTL:
// emote-jump = { THE($entity) } прыгает!

// C#:
// ✅ Хорошо: Передаем EntityUid, движок найдет пол и имя
_loc.GetString("emote-jump", ("entity", uid));

// ❌ Плохо: Передаем просто имя, функции THE() и GENDER() не сработают
_loc.GetString("emote-jump", ("entity", Name(uid)));
```

## Примеры из кода

### Регистрация компонента с LocId

```csharp
[RegisterComponent]
public sealed partial class VendingMachineComponent : Component
{
    [DataField]
    public LocId DenyMessage = "vending-machine-deny"; // Значение по умолчанию
}
```

### Использование в системе

```csharp
public sealed class VendingMachineSystem : EntitySystem
{
    // Не импортируем ILocalizationManager, так как он встроен в EntitySystem как Loc
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public void OnDeny(Entity<VendingMachineComponent> ent)
    {
        // Получаем строку из компонента и показываем попап
        var msg = Loc.GetString(ent.Comp.DenyMessage);
        _popup.PopupEntity(msg, ent, PopupType.Small);
    }
}
```

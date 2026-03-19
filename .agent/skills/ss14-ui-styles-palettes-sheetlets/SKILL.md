---
name: SS14 UI Styles Palettes Sheetlets
description: Практический гайд по стилевой системе SS14: StyleClass, палитры, StyleProperties, sheetlets, псевдоклассы и композиция правил. Используй при разработке и рефакторинге визуального языка UI без хардкода.
---

# Стили, палитры и sheetlets в SS14 UI

Этот skill покрывает только стиль-систему UI: классы, палитры, sheetlets и правила применения 🙂
Разметку окон и сетевой lifecycle UI веди в отдельных skill.

## Каркас системы

- `StyleClass`: единая точка имен общих style classes.
- `StyleProperties`: ключи семантических palette-свойств.
- `ColorPalette` + `Palettes` + `StatusPalette`: модель цветовой семантики.
- `Sheetlet<T>`: модуль стилей для конкретной группы контролов.
- `StylesheetHelpers`: DSL-обертки для удобной сборки `StyleRule`.

## Псевдоклассы: полный список и когда выдаются

| Псевдокласс | Где используется | Когда выставляется |
|---|---|---|
| `normal` | `ContainerButton`, `TextureButton`, наследники | Обычный режим отрисовки (`DrawModeEnum.Normal`). |
| `hover` | `ContainerButton`, `TextureButton`, `ScrollBar` | Курсор над контролом/граббером, но не активное нажатие/drag. |
| `pressed` | `ContainerButton`, `TextureButton`, наследники | Контрол в состоянии нажатия (`DrawModeEnum.Pressed`). |
| `disabled` | `ContainerButton`, `TextureButton`, наследники | Контрол отключен (`DrawModeEnum.Disabled`). |
| `grabbed` | `ScrollBar` | Ползунок скроллбара захвачен мышью (drag активен). |
| `placeholder` | `LineEdit`, `TextEdit` | Пустой текст + задан placeholder, отображается плейсхолдер. |
| `notEditable` | `TextEdit` | Контрол переведен в read-only (`Editable = false`). |
| `confirm-normal` | `ConfirmButton` | Кнопка в confirm-режиме и в обычном состоянии. |
| `confirm-hover` | `ConfirmButton` | Confirm-режим + наведение. |
| `confirm-pressed` | `ConfirmButton` | Confirm-режим + нажатие. |
| `confirm-disabled` | `ConfirmButton` | Confirm-режим + disabled. |

### Примеры из кода: выдача псевдоклассов

```csharp
// Базовая кнопка: псевдокласс следует за DrawMode.
protected override void DrawModeChanged()
{
    switch (DrawMode)
    {
        case DrawModeEnum.Normal:
            SetOnlyStylePseudoClass(StylePseudoClassNormal);
            break;
        case DrawModeEnum.Pressed:
            SetOnlyStylePseudoClass(StylePseudoClassPressed);
            break;
        case DrawModeEnum.Hover:
            SetOnlyStylePseudoClass(StylePseudoClassHover);
            break;
        case DrawModeEnum.Disabled:
            SetOnlyStylePseudoClass(StylePseudoClassDisabled);
            break;
    }
}
```

```csharp
// ScrollBar: grabbed имеет приоритет над hover.
private void _updatePseudoClass()
{
    if (_grabData != null)
        SetOnlyStylePseudoClass(StylePseudoClassGrabbed);
    else if (_isHovered)
        SetOnlyStylePseudoClass(StylePseudoClassHover);
    else
        SetOnlyStylePseudoClass(null);
}
```

```csharp
// TextEdit: placeholder + notEditable.
private void UpdatePseudoClass()
{
    SetOnlyStylePseudoClass(IsPlaceholderVisible ? StylePseudoClassPlaceholder : null);
    if (!Editable)
        AddStylePseudoClass(StylePseudoClassNotEditable);
}
```

```csharp
// ConfirmButton: добавляет префикс confirm- к базовым состояниям.
protected override void DrawModeChanged()
{
    if (IsConfirming)
    {
        switch (DrawMode)
        {
            case DrawModeEnum.Normal:
                SetOnlyStylePseudoClass(ConfirmPrefix + StylePseudoClassNormal);
                break;
            case DrawModeEnum.Pressed:
                SetOnlyStylePseudoClass(ConfirmPrefix + StylePseudoClassPressed);
                break;
            case DrawModeEnum.Hover:
                SetOnlyStylePseudoClass(ConfirmPrefix + StylePseudoClassHover);
                break;
            case DrawModeEnum.Disabled:
                SetOnlyStylePseudoClass(ConfirmPrefix + StylePseudoClassDisabled);
                break;
        }
        return;
    }

    base.DrawModeChanged();
}
```

## Все хелперы-обертки над `Prop`

Ниже полный список оберток из `StylesheetHelpers`, которые в итоге вызывают `Prop(...)`.

| Хелпер | Что выставляет |
|---|---|
| `Modulate(Color)` | `Control.StylePropertyModulateSelf` |
| `Margin(Thickness)` | `Control.Margin` |
| `Margin(float)` | `Control.Margin` (через `Thickness`) |
| `MinWidth(float)` | `Control.MinWidth` |
| `MinHeight(float)` | `Control.MinHeight` |
| `MinSize(Vector2)` | `Control.MinWidth` + `Control.MinHeight` |
| `MaxWidth(float)` | `Control.MaxWidth` |
| `MaxHeight(float)` | `Control.MaxHeight` |
| `MaxSize(Vector2)` | `Control.MaxWidth` + `Control.MaxHeight` |
| `SetWidth(float)` | `Control.SetWidth` |
| `SetHeight(float)` | `Control.SetHeight` |
| `SetSize(Vector2)` | `Control.SetWidth` + `Control.SetHeight` |
| `HorizontalExpand(bool)` | `Control.HorizontalExpand` |
| `VerticalExpand(bool)` | `Control.VerticalExpand` |
| `HorizontalAlignment(HAlignment)` | в текущей реализации helper пишет в ключ `Control.HorizontalExpand` |
| `VerticalAlignment(VAlignment)` | в текущей реализации helper пишет в ключ `Control.VerticalExpand` |
| `AlignMode(Label.AlignMode)` | `Label.StylePropertyAlignMode` |
| `Panel(StyleBox)` | `PanelContainer.StylePropertyPanel` |
| `Box(StyleBox)` | `ContainerButton.StylePropertyStyleBox` |
| `Font(Font)` | `Label.StylePropertyFont` |
| `FontColor(Color)` | `Label.StylePropertyFontColor` |

Дополнительно (не `Prop`-обертки, но часто идут рядом):
- `PseudoNormal()`, `PseudoHovered()`, `PseudoPressed()`, `PseudoDisabled()`
- `MaybeClass(...)`
- `IntoPatch(...)`
- `ParentOf(...)`

Важно:
- Для реального выравнивания лучше задавать нужный ключ напрямую через `Prop(...)`, пока helper-обертки `HorizontalAlignment/VerticalAlignment` не синхронизированы с ожидаемым поведением.

## Схемы: ребенок по родителю и родитель по ребенку

### 1) Изменение ребенка по родителю (поддерживается напрямую)

Схема:

```text
Parent selector -> Child selector -> Style props child
```

Пример из кода:

```csharp
// Стиль внутренней PanelContainer зависит от NanoHeading-родителя.
return
[
    E<NanoHeading>()
        .ParentOf(E<PanelContainer>())
        .Panel(nanoHeadingBox),
];
```

### 2) Изменение родителя по ребенку (прямого селектора нет)

Чисто CSS-стилем это не делается, рабочий паттерн такой:

```text
Child state/event -> code sets parent class/pseudoclass -> style matches parent
```

Рекомендация:
- в коде родителя/контейнера выставляй `StyleClass` или псевдокласс при изменении состояния дочернего контрола;
- в stylesheet описывай правила уже для родителя по этому class/pseudo.

Мини-пример подхода:

```csharp
// Code-behind: дочерний контрол изменил состояние -> помечаем родителя классом.
if (childIsWarning)
    parent.AddStyleClass("child-warning");
else
    parent.RemoveStyleClass("child-warning");

// Stylesheet: родитель стилизуется по class, выставленному из кода.
E<PanelContainer>()
    .Class("child-warning")
    .Modulate(sheet.NegativePalette.Element);
```

## Использование текстур для UI

### Базовые правила

- Берем текстуры через API stylesheet (`GetTexture`, `TryGetTexture`, `GetTextureOr`), а не хардкодим доступ к ресурсам напрямую.
- Для растягиваемых кнопок/панелей используем `StyleBoxTexture` и patch margins.
- Для иконок в контролах используем `TextureRect.StylePropertyTexture`.

### Примеры из кода

```csharp
// Текстура кнопки из stylesheet-ресурсов.
var buttonTex = sheet.GetTextureOr(cfg.BaseButtonPath, NanotrasenStylesheet.TextureRoot);

var topButtonBase = new StyleBoxTexture
{
    Texture = buttonTex,
};
topButtonBase.SetPatchMargin(StyleBox.Margin.All, 10);
```

```csharp
// Текстура иконки в стиле.
E<TextureRect>()
    .Class(OptionButton.StyleClassOptionTriangle)
    .Prop(TextureRect.StylePropertyTexture, invertedTriangleTex);
```

```csharp
// Удобная обертка для 9-slice.
var styleBox = texture.IntoPatch(StyleBox.Margin.All, 3);
styleBox.SetContentMarginOverride(StyleBox.Margin.Horizontal, 5);
```

## Правила подбора цветов для стилей

1. Сначала выбирай семантическую палитру:
- `Primary`: интерактивные элементы и основной акцент.
- `Secondary`: фоны и вторичный UI.
- `Positive`: подтверждение/успех.
- `Negative`: опасность/ошибка.
- `Highlight`: ключевой акцент.

2. Для интерактивных состояний соблюдай цепочку:
- `Element` -> `HoveredElement` -> `PressedElement` -> `DisabledElement`.

3. Для текста и фона используй назначенные роли:
- текст: `Text`/`TextDark`;
- подложки: `Background`/`BackgroundLight`/`BackgroundDark`.

4. Статусы и шкалы делай через `StatusPalette`, а не ручные if/hex:

```csharp
var good = Palettes.Status.GetStatusColor(1.0f);
var warning = Palettes.Status.GetStatusColor(0.5f);
var critical = Palettes.Status.GetStatusColor(0.0f);
```

5. `Color.FromHex(...)` допустим в одном месте: при создании новой палитры.

6. Для иконок/текстурных кнопок цветовую вариативность делай через `Modulate(...)`, чтобы не плодить дубли текстур.

## Работа со шрифтами

### Что использовать

- `sheet.BaseFont.GetFont(size, FontKind)` для конкретных правил.
- `FontKind`: `Regular`, `Bold`, `Italic`, `BoldItalic`.
- общие style classes для типографики: `FontSmall`, `FontLarge`, `Italic`, `Monospace`.

### Примеры из кода

```csharp
// Крупная жирная подпись для меню-кнопок.
E<Label>()
    .Class(MenuButton.StyleClassLabelTopButton)
    .Prop(Label.StylePropertyFont, sheet.BaseFont.GetFont(14, FontKind.Bold));
```

```csharp
// Обычный и italic-стиль для статусных текстов.
E()
    .Class(StyleClass.ItemStatus)
    .Prop("font", sheet.BaseFont.GetFont(10));

E()
    .Class(StyleClass.ItemStatusNotHeld)
    .Prop("font", sheet.BaseFont.GetFont(10, FontKind.Italic))
    .Prop("font-color", Color.Gray);
```

### Практика

- Размер/гарнитура должны быть функцией роли текста (label, heading, status), а не локального вкуса.
- Один и тот же semantic role = один и тот же font rule по проекту.
- Если нужен новый типографический паттерн, сначала оформляй его как общий class/rule, потом переиспользуй.

## Паттерны 😎

- Строй sheetlets маленькими и тематическими.
- Используй `ParentOf(...)` для контекстного оформления вложенных контролов.
- Храни цветовые решения в палитрах, а не в элементах.
- Для кнопок и интерактива в первую очередь думай состояниями `normal/hover/pressed/disabled`.
- Используй модификацию цвета через `Modulate`, если можно обойтись одной текстурой.

## Анти-паттерны

- Хардкодить цвета по месту (`Color.FromHex`) вместо палитр.
- Мешать визуальные задачи UI-стилей с задачами графического пайплайна мира.
- Разбрасывать одинаковые font-настройки по десяткам правил без общего класса.
- Пытаться реализовать стиль родителя по ребенку только селектором, без кода-связки.

## Чеклист качества

- Все состояния интерактива покрыты псевдоклассами.
- Для каждой `Prop(...)`-операции, где возможно, использованы обертки-хелперы.
- Контекстные стили через `ParentOf(...)` применены осознанно.
- Текстуры подключаются через API stylesheet и корректные patch margins.
- Цвета взяты из семантических палитр и status-модели.
- Шрифты и размеры задаются как системные правила, а не ad-hoc.

---
name: SS14 UI XAML
description: Практический гайд по XAML-интерфейсам SS14: структура окон, GenerateTypedNameReferences, загрузка через RobustXamlLoader, layout-контейнеры, локализация и стиль-классы. Используй при создании, рефакторинге и визуальной полировке UI-окон.
---

# XAML и окна UI в SS14

Этот skill покрывает только XAML и code-behind для окон/контролов 🙂
Сетевую часть (`EUI`, `UserInterfaceSystem`, `UserInterfaceManager`) и глубокую стилизацию (палитры/sheetlets) веди в отдельных skill.

## Жесткая связка XAML и `xaml.cs`

Это обязательное правило, не рекомендация ⚠️

1. Имя класса в `xaml.cs` и имя файла `.xaml` должны совпадать один-в-один.
2. Класс должен быть `partial`, если используется `[GenerateTypedNameReferences]`.
3. Корневой тип в XAML должен совпадать с классом из `xaml.cs` или его базовым классом.
4. Должен существовать ровно один подходящий `.xaml` для класса.

Если нарушить эти требования, генератор typed-name references выбрасывает compile-time ошибки (`RXN0001`, `RXN0002`, `RXN0005`).

Мини-пример правильной связки:

```text
AdminPanel.xaml
AdminPanel.xaml.cs -> public sealed partial class AdminPanel : FancyWindow
```

## Статическое получение данных в XAML

Для статических значений используй `x:Static` и `x:Type`.

Когда это нужно:
- константы размеров/параметров из C#;
- enum/статические поля для хоткеев и конфигураций;
- статические style-классы из `StyleClass`;
- передача `Type` в свойства, ожидающие тип окна/контрола.

Примеры:

```xml
<!-- Статическое поле enum -->
<ui:MenuButton BoundKey="{x:Static is:ContentKeyFunctions.OpenGuidebook}" />

<!-- Статическая константа высоты -->
<ContainerButton MinHeight="{x:Static ui:ContextMenuElement.ElementHeight}" />

<!-- Статическая строковая константа style class -->
<ui:MenuButton AppendStyleClass="{x:Static style:StyleClass.ButtonSquare}" />

<!-- Передача Type вместо строки -->
<cc:UICommandButton WindowType="{x:Type at:AddAtmosWindow}" />
```

Паттерн:
- Если значение «архитектурное» и уже объявлено в коде как `const/static`, подтягивай его в XAML через `x:Static`, а не дублируй литералом.

Анти-паттерн:
- Дублировать строки ключей/классов/типов в XAML вручную, когда есть статическое поле-источник.

## UiDependency в коде элемента

В текущей кодовой базе нет отдельного атрибута `UiDependency`.
Практический эквивалент для UI-элементов:

1. Используй `[Dependency]` для обычных IoC-зависимостей элемента.
2. Вызывай `IoCManager.InjectDependencies(this)` в конструкторе после `RobustXamlLoader.Load(this)`.
3. Для `UIController` и зависимостей от `EntitySystem` используй `[UISystemDependency]`.

Пример для UI-элемента:

```csharp
[GenerateTypedNameReferences]
public partial class FancyWindow : BaseWindow
{
    [Dependency] private readonly IEntitySystemManager _sysMan = default!;
    [Dependency] private readonly IStylesheetManager _styleMan = default!;

    public FancyWindow()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);
    }
}
```

Пример для `UIController`:

```csharp
public sealed class GuidebookUIController : UIController
{
    [UISystemDependency] private readonly GuidebookSystem _guidebookSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
}
```

## Таблица полезных UI-элементов

| Элемент | Когда использовать | Мини-пример |
|---|---|---|
| `FancyWindow` | Стандартные игровые окна с заголовком/закрытием | ``<ui:FancyWindow Title="{Loc 'ui-title'}">...</ui:FancyWindow>`` |
| `Control` | Базовый контейнер-обертка без лишнего поведения | ``<Control MinWidth="200">...</Control>`` |
| `BoxContainer` | Вертикальный/горизонтальный поток элементов | ``<BoxContainer Orientation="Vertical" SeparationOverride="4">...</BoxContainer>`` |
| `GridContainer` | Табличное размещение с колонками | ``<GridContainer Columns="3">...</GridContainer>`` |
| `ScrollContainer` | Прокрутка длинного контента | ``<ScrollContainer VerticalExpand="True">...</ScrollContainer>`` |
| `PanelContainer` | Фон/рамка/визуальное отделение блока | ``<PanelContainer StyleClasses="BackgroundPanel" />`` |
| `Label` | Обычный текст, заголовки, короткие подписи | ``<Label StyleClasses="LabelSubText" Text="{Loc 'ui-label'}" />`` |
| `RichTextLabel` | Разметка/многострочные форматированные тексты | ``<RichTextLabel Name="Description" />`` |
| `LineEdit` | Ввод строки и поиск | ``<LineEdit PlaceHolder="{Loc 'ui-search'}" HorizontalExpand="True" />`` |
| `Button` | Основное действие пользователя | ``<Button Text="{Loc 'ui-confirm'}" />`` |
| `TextureButton` | Иконка-кнопка (help/close/refresh) | ``<TextureButton StyleClasses="windowCloseButton" />`` |
| `ItemList` | Выбор из списка элементов | ``<ItemList Name="MusicList" SelectMode="Button" VerticalExpand="True" />`` |
| `Slider` | Выбор значения по диапазону | ``<Slider Name="PlaybackSlider" HorizontalExpand="True" />`` |
| `TextureRect` | Отображение текстур/иконок | ``<TextureRect Stretch="KeepCentered" />`` |

## Быстрый выбор подхода

1. Нужен стандартный игровый window chrome, заголовок и единый стиль?
- Используй `FancyWindow`.
2. Нужен переиспользуемый составной элемент?
- Делай `Control` + XAML + `[GenerateTypedNameReferences]`.
3. Нужны динамические элементы (кнопки/фильтры/списки) после загрузки разметки?
- Добавляй их в конструкторе после `RobustXamlLoader.Load(this)`.

## Рабочие паттерны

- Строй layout через контейнеры (`BoxContainer`, `ScrollContainer`, `GridContainer`), а не через ручные координаты.
- Держи XAML декларативным: структура, классы, базовые атрибуты.
- Держи поведение в code-behind: подписки, заполнение данных, условная логика.
- Локализуй текст через `Loc`/`{Loc ...}` и не хардкодь строки.
- Используй `Name` + `[GenerateTypedNameReferences]`, чтобы получать типобезопасные ссылки на контролы.
- Добавляй `Access="Public"` только там, где элемент реально нужен снаружи.

## Анти-паттерны

- Позиционировать UI «пикселями», когда достаточно контейнеров.
- Держать бизнес-логику прямо в XAML.
- Массово помечать элементы `Access="Public"` без причины.
- Хардкодить цвета/строки вместо style classes и локализации.
- Копировать старые примеры без проверки даты и проблемных комментариев ⚠️

## Примеры из кода

### Пример 1: каркас окна через `FancyWindow`

```xml
<controls:FancyWindow xmlns="https://spacestation14.io"
                      xmlns:controls="clr-namespace:Content.Client.UserInterface.Controls"
                      MouseFilter="Stop"
                      MinWidth="200" MinHeight="150">
    <!-- Фоновая панель в стиле текущей темы -->
    <PanelContainer StyleClasses="BackgroundPanel" />

    <BoxContainer Orientation="Vertical">
        <Control>
            <!-- Хедер окна со style class -->
            <PanelContainer StyleClasses="WindowHeadingBackground" Name="WindowHeader" />
            <BoxContainer Margin="4 2 4 0" Orientation="Horizontal">
                <Label Name="WindowTitle"
                       HorizontalExpand="True" VAlign="Center" StyleClasses="FancyWindowTitle" />
                <TextureButton Name="CloseButton" StyleClasses="windowCloseButton" />
            </BoxContainer>
        </Control>

        <PanelContainer StyleClasses="LowDivider" />

        <!-- Публичный контейнер контента для последующего наполнения -->
        <Control Access="Public" Name="ContentsContainer" VerticalExpand="true" />
    </BoxContainer>
</controls:FancyWindow>
```

### Пример 2: типобезопасные ссылки + программное расширение UI

```csharp
[GenerateTypedNameReferences]
public sealed partial class ActionsWindow : DefaultWindow
{
    public MultiselectOptionButton<Filters> FilterButton { get; private set; }

    public ActionsWindow()
    {
        // Сначала загружаем XAML-дерево.
        RobustXamlLoader.Load(this);

        // Потом дополняем его динамическим контролом.
        SearchContainer.AddChild(FilterButton = new MultiselectOptionButton<Filters>
        {
            Label = Loc.GetString("ui-actionmenu-filter-button")
        });
    }
}
```

### Пример 3: несколько style classes в XAML

```xml
<Button Name="DoneButton" Text="{Loc 'nano-task-ui-done'}">
    <Button.StyleClasses>
        <!-- Комбинируем размер и форму кнопки -->
        <system:String>ButtonSmall</system:String>
        <system:String>OpenLeft</system:String>
    </Button.StyleClasses>
</Button>
```

### Пример 4: связывание данных и событий после загрузки разметки

```csharp
public NanoTaskItemControl(NanoTaskItemAndId item)
{
    RobustXamlLoader.Load(this);

    // Заполняем UI данными.
    TaskLabel.Text = item.Data.Description;
    TaskForLabel.Text = item.Data.TaskIsFor;

    // Привязываем действия пользователя к доменной логике.
    MainButton.OnPressed += _ => OnMainPressed?.Invoke(item.Id);
    DoneButton.OnPressed += _ => OnDonePressed?.Invoke(item.Id);
}
```

## Чеклист перед PR

- Окно собирается контейнерами и корректно тянется при resize.
- Все пользовательские строки локализованы.
- Для статических значений применяются `x:Static`/`x:Type`, а не дубли литералов.
- Соблюдена жёсткая связка имени класса и имени `.xaml`.
- Нет хардкода визуала, который должен быть в style system.
- Подписки на события осмысленные, без «лишнего шума».
- Используются только свежие и чистые (без TODO/FIXME) референсы кода 👍

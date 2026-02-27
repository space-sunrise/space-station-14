---
name: SS14 UI EUI and UI Manager
description: Глубокий практический гайд по связке EUI, UserInterfaceSystem и UserInterfaceManager в SS14: выбор подхода, lifecycle, обмен state/message, управление окнами через UIController и безопасные сетевые паттерны.
---

# EUI + UserInterfaceSystem + UserInterfaceManager

Этот skill покрывает только архитектуру UI-жизненного цикла и сетевой обмен для интерфейсов 🙂
Детали XAML-вёрстки и стилизации веди в отдельных skill.

## Источник правды и отбор примеров

1. Приоритет: актуальный код движка и актуальный игровой код.
2. Документацию из `docs` используй как вторичный источник.
3. Не используй код старше 2 лет.
4. Не используй примеры с TODO/FIXME/проблемными комментариями по UI-сетке и lifecycle.

## Когда что выбирать

1. Локальное окно/виджет без серверного state?
- `UIController` + `UserInterfaceManager`.
2. Интерфейс привязан к сущности и её компонентам?
- `BoundUserInterface` + `UserInterfaceSystem` (BUI).
3. Серверный диалог/панель управления, не обязанный жить на сущности?
- `EUI` (server/client `BaseEui` + сообщения/состояние).

## Ментальная модель

- `UserInterfaceManager` владеет UI-root-ами (`StateRoot`, `WindowRoot`, `PopupRoot`) и очередями обновления.
- `UIController` живёт singleton-ом, связывает input/state/system события и окна.
- BUI работает сообщениями `BoundUserInterfaceMessage` и часто открывает `BaseWindow` через helper-ы.
- EUI работает сообщениями `EuiMessageBase` и state-снимками `EuiStateBase` с явным `StateDirty`.

## Паттерны

- Делай `EnsureWindow()` и переиспользуй окно вместо бесконтрольного `new`.
- В BUI связывай `OnClose` окна с `bui.Close()`, чтобы клиент и сервер закрывались синхронно.
- Передавай через EUI компактные DTO state/message, без «сырого» UI-шума.
- Отправляй обновления через `StateDirty()`/очередь, а не спамом каждый тик.
- Подписывайся/отписывайся на state/system события в соответствующих lifecycle-точках.
- Разделяй ответственность: контроллер управляет flow, окно отображает, система хранит доменный state.

## Анти-паттерны

- Использовать EUI для высокочастотной покадровой телеметрии.
- Создавать новое окно на каждый клик вместо переиспользования.
- Смешивать EUI и BUI для одного и того же use-case без причины.
- Забывать отправить `CloseEuiMessage` при закрытии client-side окна.
- Передавать в сообщения «универсальные» слаботипизированные payload-ы ⚠️

## Примеры из кода

### Пример 1: безопасный lifecycle окна в `UIController`

```csharp
public sealed class OptionsUIController : UIController
{
    private OptionsMenu _optionsWindow = default!;

    private void EnsureWindow()
    {
        if (_optionsWindow is { Disposed: false })
            return;

        // Создание через UIManager, а не прямой new.
        _optionsWindow = UIManager.CreateWindow<OptionsMenu>();
    }

    public void ToggleWindow()
    {
        EnsureWindow();

        if (_optionsWindow.IsOpen)
            _optionsWindow.Close();
        else
            _optionsWindow.OpenCentered();
    }
}
```

### Пример 2: entity-bound UI (BUI) с оконным представлением

```csharp
protected override void Open()
{
    base.Open();

    // Создаём окно как часть lifecycle BUI.
    _menu = this.CreateWindow<JukeboxMenu>();

    _menu.OnPlayPressed += shouldPlay =>
    {
        // Клиент шлёт typed-сообщение в серверную часть BUI.
        SendMessage(shouldPlay ? new JukeboxPlayingMessage() : new JukeboxPauseMessage());
    };

    _menu.OnStopPressed += () => SendMessage(new JukeboxStopMessage());
}
```

### Пример 3: восстановление позиции окна для BUI

```csharp
public static T CreateWindow<T>(this BoundUserInterface bui) where T : BaseWindow, new()
{
    var window = GetWindow<T>(bui);

    if (bui.EntMan.System<UserInterfaceSystem>().TryGetPosition(bui.Owner, bui.UiKey, out var position))
        window.Open(position);
    else
        window.OpenCentered();

    return window;
}
```

### Пример 4: серверный EUI-менеджер и отложенная отправка state

```csharp
public void QueueStateUpdate(BaseEui eui)
{
    _stateUpdateQueue.Enqueue((eui.Player, eui.Id));
}

public void SendUpdates()
{
    while (_stateUpdateQueue.TryDequeue(out var tuple))
    {
        var (player, id) = tuple;
        if (!_playerData.TryGetValue(player, out var plyDat) || !plyDat.OpenUIs.TryGetValue(id, out var ui))
            continue;

        // Реальная отправка состояния в одном месте lifecycle.
        ui.DoStateUpdate();
    }
}
```

### Пример 5: типобезопасный EUI state + client-side применение

```csharp
[Serializable, NetSerializable]
public sealed class PlayerPanelEuiState(
    NetUserId guid,
    string username,
    TimeSpan playtime)
    : EuiStateBase
{
    public readonly NetUserId Guid = guid;
    public readonly string Username = username;
    public readonly TimeSpan Playtime = playtime;
}

public override void HandleState(EuiStateBase state)
{
    if (state is not PlayerPanelEuiState s)
        return;

    // UI обновляется из state-снимка.
    PlayerPanel.SetUsername(s.Username);
    PlayerPanel.SetPlaytime(s.Playtime);
}
```

## Мини-чеклист

- Выбран правильный слой: `UIController` / BUI / EUI.
- Lifecycle закрытия симметричен lifecycle открытия.
- Сетевые сообщения минимальны и типобезопасны.
- Нет дублей окон и «висячих» подписок.
- Примеры опираются на свежий, не проблемный код ✅

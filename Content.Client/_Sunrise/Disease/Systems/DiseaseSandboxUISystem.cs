using Content.Client.UserInterface.Systems.Sandbox.Windows;
using Content.Shared._Sunrise.Disease.Components;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Console;
using Robust.Shared.Player;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client._Sunrise.Disease.Systems;

/// <summary>
///     Динамически добавляет кнопку переключения видимости заболеваний
///     в окно песочницы (SandboxWindow), не изменяя ванильные файлы.
/// </summary>
public sealed class DiseaseSandboxUISystem : EntitySystem
{
    [Dependency] private readonly IUserInterfaceManager _ui = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly IConsoleHost _console = default!;

    private SandboxWindow? _trackedWindow;
    private Button? _toggleDiseaseButton;

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (!_ui.TryGetFirstWindow(out SandboxWindow? window) || window == null)
        {
            // Окно не существует (напр. вышли из GameplayState)
            _trackedWindow = null;
            _toggleDiseaseButton = null;
            return;
        }

        if (window == _trackedWindow)
        {
            SyncButtonState();
            return;
        }

        // Новый экземпляр окна — внедряем кнопку.
        InjectButton(window);
        _trackedWindow = window;
    }

    private void InjectButton(SandboxWindow window)
    {
        // ShowBbButton последняя кнопка в секции видимости.
        var showBb = window.ShowBbButton;
        var container = showBb.Parent;

        if (container == null)
            return;

        _toggleDiseaseButton = new Button
        {
            Text = Loc.GetString("sandbox-window-toggle-disease-button"),
            ToggleMode = true,
        };

        var index = showBb.GetPositionInParent();
        container.AddChild(_toggleDiseaseButton);
        _toggleDiseaseButton.SetPositionInParent(index + 1);

        _toggleDiseaseButton.OnToggled += OnDiseaseToggled;

        SyncButtonState();
    }

    private void OnDiseaseToggled(ButtonToggledEventArgs args)
    {
        var player = _player.LocalEntity;
        if (player == null)
            return;

        var netEntity = EntityManager.GetNetEntity(player.Value);

        if (args.Pressed)
            _console.ExecuteCommand($"addcomp {netEntity.Id} DiseaseSandboxVisibility");
        else
            _console.ExecuteCommand($"rmcomp {netEntity.Id} DiseaseSandboxVisibility");
    }

    private void SyncButtonState()
    {
        if (_toggleDiseaseButton == null)
            return;

        var player = _player.LocalEntity;
        if (player == null)
            return;

        _toggleDiseaseButton.Pressed = HasComp<DiseaseSandboxVisibilityComponent>(player.Value);
    }
}

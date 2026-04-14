using System.Numerics;
using Content.Client._Sunrise.Sandbox.Transparency.Systems;
using Content.Client._Sunrise.UserInterface.Systems.Sandbox.Widgets;
using Content.Client.UserInterface.Screens;
using Content.Client.UserInterface.Systems.Gameplay;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client._Sunrise.UserInterface.Systems.Sandbox;

/// <summary>
/// Owns the on-screen transparency widget and synchronizes it with the overlay system.
/// </summary>
public sealed class MappingTransparencyWidgetController : UIController, IOnSystemChanged<MappingTransparencySystem>
{
    private MappingTransparencySystem? _mappingTransparency;
    private InGameScreen? _screen;
    private MappingTransparencyWidget? _widget;

    /// <summary>
    /// Hooks the controller into gameplay screen load events.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();

        var gameplayStateLoad = UIManager.GetUIController<GameplayStateLoadController>();
        gameplayStateLoad.OnScreenLoad += OnScreenLoad;
        gameplayStateLoad.OnScreenUnload += OnScreenUnload;
    }

    /// <summary>
    /// Starts mirroring state from the mapping transparency system.
    /// </summary>
    public void OnSystemLoaded(MappingTransparencySystem system)
    {
        _mappingTransparency = system;
        _mappingTransparency.StateChanged += SyncWidgetState;
        SyncWidgetState();
    }

    /// <summary>
    /// Stops mirroring state from the mapping transparency system.
    /// </summary>
    public void OnSystemUnloaded(MappingTransparencySystem system)
    {
        system.StateChanged -= SyncWidgetState;

        if (ReferenceEquals(_mappingTransparency, system))
            _mappingTransparency = null;

        SyncWidgetState();
    }

    private void OnScreenLoad()
    {
        OnScreenUnload();

        if (UIManager.ActiveScreen is not InGameScreen screen)
            return;

        _screen = screen;
        _screen.OnResized += UpdateWidgetPlacement;
        _screen.OnChatResized += OnChatResized;

        SyncWidgetState();
    }

    private void OnScreenUnload()
    {
        if (_screen != null)
        {
            _screen.OnResized -= UpdateWidgetPlacement;
            _screen.OnChatResized -= OnChatResized;
        }

        RemoveWidget();
        _screen = null;
    }

    private void OnChatResized(Vector2 _)
    {
        UpdateWidgetPlacement();
    }

    private void OnWidgetTransparencyChanged(int percent)
    {
        _mappingTransparency?.SetTransparencyPercent(percent);
    }

    private void SyncWidgetState()
    {
        if (_mappingTransparency == null ||
            _screen == null ||
            !_mappingTransparency.Enabled ||
            !_mappingTransparency.CanEnable)
        {
            RemoveWidget();
            return;
        }

        EnsureWidget();

        if (_widget == null)
            return;

        _widget.SetTransparencyPercent(_mappingTransparency.TransparencyPercent);
        UpdateWidgetPlacement();
    }

    private void UpdateWidgetPlacement()
    {
        if (_screen == null || _widget == null)
            return;

        MappingOverlayWidgetPlacementHelper.UpdateWidgetPlacement(_screen, _widget);
    }

    private void EnsureWidget()
    {
        if (_screen == null || _widget != null)
            return;

        _widget = _screen.GetOrAddWidget<MappingTransparencyWidget>();
        _widget.TransparencyChanged += OnWidgetTransparencyChanged;
        _widget.OnResized += UpdateWidgetPlacement;
        LayoutContainer.SetAnchorPreset(_widget, LayoutContainer.LayoutPreset.TopLeft);
        _widget.SetPositionInParent(_screen.ChildCount - 1);
    }

    private void RemoveWidget()
    {
        if (_widget == null)
            return;

        _widget.TransparencyChanged -= OnWidgetTransparencyChanged;
        _widget.OnResized -= UpdateWidgetPlacement;
        _widget.Parent?.RemoveChild(_widget);
        _widget = null;
    }
}

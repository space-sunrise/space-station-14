using System.Numerics;
using Content.Client._Sunrise.Sandbox;
using Content.Client._Sunrise.UserInterface.Systems.Sandbox.Widgets;
using Content.Client.UserInterface.Screens;
using Content.Client.UserInterface.Systems.Gameplay;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client._Sunrise.UserInterface.Systems.Sandbox;

/// <summary>
/// Owns the on-screen mapping access widget and synchronizes it with the overlay system.
/// </summary>
public sealed class MappingAccessWidgetController : UIController, IOnSystemChanged<MappingAccessOverlaySystem>
{
    private MappingAccessOverlaySystem? _mappingAccess;
    private InGameScreen? _screen;
    private MappingAccessWidget? _widget;

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
    /// Starts mirroring state from the mapping access overlay system.
    /// </summary>
    public void OnSystemLoaded(MappingAccessOverlaySystem system)
    {
        _mappingAccess = system;
        _mappingAccess.StateChanged += SyncWidgetState;
        SyncWidgetState();
    }

    /// <summary>
    /// Stops mirroring state from the mapping access overlay system.
    /// </summary>
    public void OnSystemUnloaded(MappingAccessOverlaySystem system)
    {
        system.StateChanged -= SyncWidgetState;

        if (ReferenceEquals(_mappingAccess, system))
            _mappingAccess = null;

        SyncWidgetState();
    }

    private void OnScreenLoad()
    {
        OnScreenUnload();

        if (UIManager.ActiveScreen is not InGameScreen screen)
            return;

        _screen = screen;
        _widget = screen.GetOrAddWidget<MappingAccessWidget>();
        _widget.ElectronicsOnlyChanged += OnWidgetElectronicsOnlyChanged;
        _widget.FilterChanged += OnWidgetFilterChanged;
        _widget.OnResized += UpdateWidgetPlacement;
        LayoutContainer.SetAnchorPreset(_widget, LayoutContainer.LayoutPreset.TopLeft);
        _widget.SetPositionInParent(screen.ChildCount - 1);

        screen.OnResized += UpdateWidgetPlacement;
        screen.OnChatResized += OnChatResized;

        SyncWidgetState();
    }

    private void OnScreenUnload()
    {
        if (_screen != null)
        {
            _screen.OnResized -= UpdateWidgetPlacement;
            _screen.OnChatResized -= OnChatResized;
        }

        if (_widget != null)
        {
            _widget.ElectronicsOnlyChanged -= OnWidgetElectronicsOnlyChanged;
            _widget.FilterChanged -= OnWidgetFilterChanged;
            _widget.OnResized -= UpdateWidgetPlacement;
        }

        _screen = null;
        _widget = null;
    }

    private void OnChatResized(Vector2 _)
    {
        UpdateWidgetPlacement();
    }

    private void OnWidgetFilterChanged(MappingAccessBodyFilter filter)
    {
        _mappingAccess?.SetBodyFilter(filter);
    }

    private void OnWidgetElectronicsOnlyChanged(bool electronicsOnly)
    {
        _mappingAccess?.SetElectronicsOnly(electronicsOnly);
    }

    private void SyncWidgetState()
    {
        if (_widget == null)
            return;

        if (_mappingAccess == null)
        {
            _widget.Visible = false;
            return;
        }

        _widget.Visible = _mappingAccess.Enabled && _mappingAccess.CanEnable;
        _widget.SetBodyFilter(_mappingAccess.BodyFilter);
        _widget.SetElectronicsOnly(_mappingAccess.ElectronicsOnly);
        UpdateWidgetPlacement();
    }

    private void UpdateWidgetPlacement()
    {
        if (_screen == null || _widget == null || !_widget.Visible)
            return;

        MappingOverlayWidgetPlacementHelper.UpdateWidgetPlacement(_screen, _widget);
    }
}

using System.Numerics;
using Content.Client._Sunrise.Sandbox;
using Content.Client._Sunrise.UserInterface.Systems.Sandbox.Widgets;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Screens;
using Content.Client.UserInterface.Systems.Gameplay;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.Controllers;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Client.UserInterface.Systems.Sandbox;

public sealed class MappingAccessWidgetController : UIController, IOnSystemChanged<MappingAccessOverlaySystem>
{
    private const float WidgetMargin = 10f;
    private const float ChatMargin = 8f;

    private MappingAccessOverlaySystem? _mappingAccess;
    private InGameScreen? _screen;
    private MappingAccessWidget? _widget;

    public override void Initialize()
    {
        base.Initialize();

        var gameplayStateLoad = UIManager.GetUIController<GameplayStateLoadController>();
        gameplayStateLoad.OnScreenLoad += OnScreenLoad;
        gameplayStateLoad.OnScreenUnload += OnScreenUnload;
    }

    public void OnSystemLoaded(MappingAccessOverlaySystem system)
    {
        _mappingAccess = system;
        _mappingAccess.StateChanged += SyncWidgetState;
        SyncWidgetState();
    }

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
        UpdateWidgetPlacement();
    }

    private void UpdateWidgetPlacement()
    {
        if (_screen == null || _widget == null || !_widget.Visible)
            return;

        if (_screen.GetWidget<MainViewport>() is not { } viewport)
            return;

        var widgetSize = _widget.Size;
        if (widgetSize == Vector2.Zero)
            widgetSize = _widget.DesiredSize;

        if (widgetSize == Vector2.Zero)
            return;

        var viewportRect = viewport.GlobalRect;
        var desiredGlobalPos = new Vector2(
            viewportRect.Right - widgetSize.X - WidgetMargin,
            viewportRect.Top + WidgetMargin);

        var targetGlobalPos = ClampToViewport(desiredGlobalPos, widgetSize, viewportRect);
        var chatRect = _screen.ChatBox.GlobalRect;
        if (viewportRect.Intersects(chatRect))
        {
            var desiredRect = UIBox2.FromDimensions(targetGlobalPos, widgetSize);
            if (desiredRect.Intersects(chatRect))
            {
                var leftOfChat = new Vector2(
                    chatRect.Left - widgetSize.X - ChatMargin,
                    viewportRect.Top + WidgetMargin);

                if (FitsViewport(leftOfChat, widgetSize, viewportRect))
                {
                    targetGlobalPos = leftOfChat;
                }
                else
                {
                    var belowChat = new Vector2(
                        desiredGlobalPos.X,
                        chatRect.Bottom + ChatMargin);

                    targetGlobalPos = FitsViewport(belowChat, widgetSize, viewportRect)
                        ? belowChat
                        : ClampToViewport(desiredGlobalPos, widgetSize, viewportRect);
                }
            }
        }

        LayoutContainer.SetPosition(_widget, targetGlobalPos - _screen.GlobalPosition);
    }

    private static bool FitsViewport(Vector2 position, Vector2 size, UIBox2 viewportRect)
    {
        return position.X >= viewportRect.Left + WidgetMargin &&
               position.Y >= viewportRect.Top + WidgetMargin &&
               position.X + size.X <= viewportRect.Right - WidgetMargin &&
               position.Y + size.Y <= viewportRect.Bottom - WidgetMargin;
    }

    private static Vector2 ClampToViewport(Vector2 position, Vector2 size, UIBox2 viewportRect)
    {
        var minX = viewportRect.Left + WidgetMargin;
        var minY = viewportRect.Top + WidgetMargin;
        var maxX = Math.Max(minX, viewportRect.Right - size.X - WidgetMargin);
        var maxY = Math.Max(minY, viewportRect.Bottom - size.Y - WidgetMargin);

        return new Vector2(
            Math.Clamp(position.X, minX, maxX),
            Math.Clamp(position.Y, minY, maxY));
    }
}

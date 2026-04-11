using System.Numerics;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Screens;
using Content.Client.UserInterface.Systems.Chat.Widgets;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._Sunrise.UserInterface.Systems.Sandbox;

/// <summary>
/// Positions mapping helper widgets so they remain visible without covering the chat window.
/// </summary>
public static class MappingOverlayWidgetPlacementHelper
{
    private const float WidgetMargin = 10f;
    private const float ChatMargin = 8f;

    /// <summary>
    /// Repositions a mapping helper widget within the active gameplay viewport.
    /// </summary>
    public static void UpdateWidgetPlacement(InGameScreen screen, Control widget)
    {
        if (!widget.Visible)
            return;

        if (screen.GetWidget<MainViewport>() is not { } viewport)
            return;

        var widgetSize = widget.Size;
        if (widgetSize == Vector2.Zero)
            widgetSize = widget.DesiredSize;

        if (widgetSize == Vector2.Zero)
            return;

        var viewportRect = viewport.GlobalRect;
        var desiredGlobalPos = new Vector2(
            viewportRect.Right - widgetSize.X - WidgetMargin,
            viewportRect.Top + WidgetMargin);

        var targetGlobalPos = ClampToViewport(desiredGlobalPos, widgetSize, viewportRect);
        if (TryGetBlockingChatRect(screen, out var chatRect))
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

        LayoutContainer.SetPosition(widget, targetGlobalPos - screen.GlobalPosition);
    }

    private static bool TryGetBlockingChatRect(InGameScreen screen, out UIBox2 chatRect)
    {
        chatRect = default;

        if (!screen.ChatBox.VisibleInTree)
            return false;

        chatRect = screen.ChatBox.GlobalRect;

        return true;
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

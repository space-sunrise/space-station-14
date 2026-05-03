using System.Diagnostics.CodeAnalysis;
using Content.Client.Paper.UI;
using Content.Client.Stylesheets;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Utility;

namespace Content.Client._Sunrise.UserInterface.RichText;

public abstract class PaperInteractiveTagHandler : IMarkupTagHandler
{
    public static float FontLineHeight { get; set; }

    public abstract string Name { get; }
    protected abstract string FallbackLabel { get; }
    protected virtual float MinHeightPadding => 2f;

    public bool TryCreateControl(MarkupNode node, [NotNullWhen(true)] out Control? control)
    {
        control = null;

        if (!TryGetIndex(node, out var index))
            return false;

        var button = new Button
        {
            Text = node.Value.TryGetString(out var value) ? value : FallbackLabel,
            HorizontalExpand = false,
            VerticalExpand = false,
        };

        button.AddStyleClass(StyleClass.ButtonSmall);
        button.AddStyleClass(StyleClass.ButtonSquare);

        if (FontLineHeight > 0f)
            button.MinHeight = FontLineHeight + MinHeightPadding;

        button.OnPressed += _ =>
        {
            if (!TryFindPaperWindow(button, out var paperWindow))
                return;

            HandlePress(paperWindow, index);
        };

        control = button;
        return true;
    }

    protected abstract void HandlePress(PaperWindow paperWindow, int index);

    private static bool TryFindPaperWindow(Control control, [NotNullWhen(true)] out PaperWindow? paperWindow)
    {
        paperWindow = null;

        Control? current = control;
        while (current != null)
        {
            if (current is PaperWindow window)
            {
                paperWindow = window;
                return true;
            }

            current = current.Parent;
        }

        return false;
    }

    private static bool TryGetIndex(MarkupNode node, out int index)
    {
        index = 0;

        if (!node.Attributes.TryGetValue("idx", out var param))
            return false;

        if (!param.TryGetLong(out var idxLong))
            return false;

        index = (int) idxLong.Value;
        return true;
    }
}

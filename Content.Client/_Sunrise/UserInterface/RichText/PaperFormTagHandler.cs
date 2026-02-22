using System.Diagnostics.CodeAnalysis;
using Content.Client.Stylesheets;
using Content.Client.Paper.UI;
using Content.Shared._Sunrise.Paperwork;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Utility;

namespace Content.Client._Sunrise.UserInterface.RichText;

public sealed class PaperFormTagHandler : IMarkupTagHandler
{
    public static float FontLineHeight { get; set; }

    public string Name => PaperInteractiveTagParsing.FormTagName;

    public bool TryCreateControl(MarkupNode node, [NotNullWhen(true)] out Control? control)
    {
        control = null;

        if (!TryGetIndex(node, out var index))
            return false;

        var label = node.Value.TryGetString(out var value)
            ? value
            : Loc.GetString("paper-form-fill-button");

        var button = new Button
        {
            Text = label,
            HorizontalExpand = false,
            VerticalExpand = false,
        };

        button.AddStyleClass(StyleClass.ButtonSmall);
        button.AddStyleClass(StyleClass.ButtonSquare);

        if (FontLineHeight > 0f)
            button.MinHeight = FontLineHeight + 2f;

        button.OnPressed += _ =>
        {
            if (!TryFindPaperWindow(button, out var paperWindow))
                return;

            paperWindow.OnFormPressed(index);
        };

        control = button;
        return true;
    }

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

        index = (int)idxLong.Value;
        return true;
    }
}

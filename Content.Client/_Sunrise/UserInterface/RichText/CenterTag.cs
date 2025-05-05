using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Utility;

namespace Content.Client._Sunrise.UserInterface.RichText;

[UsedImplicitly]
public sealed class CenterTag : IMarkupTag
{
    public string Name => "center";

    public bool TryGetControl(MarkupNode node, [NotNullWhen(true)] out Control? control)
    {
        if (!node.Value.TryGetString(out var text))
        {
            control = null;
            return false;
        }

        var label = new Label
        {
            Text = text,
            Align = Label.AlignMode.Center,
            HorizontalAlignment = Control.HAlignment.Stretch,
            HorizontalExpand = true,
        };

        control = label;
        return true;
    }
}

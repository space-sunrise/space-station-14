using System.Diagnostics.CodeAnalysis;
using Robust.Client.UserInterface;
using Robust.Shared.Utility;

namespace Content.Client._Sunrise.UserInterface.RichText;

public sealed class EntityTextureTag : BaseTextureTag
{
    public override string Name => "enttex";

    public override bool TryCreateControl(MarkupNode node, [NotNullWhen(true)] out Control? control)
    {
        control = null;

        if (!node.Attributes.TryGetValue("id", out var id) || !id.TryGetString(out var uid))
            return false;

        if (!node.Attributes.TryGetValue("scale", out var scale) || !scale.TryGetLong(out var scaleValue))
            scaleValue = 1;

        if (!TryDrawIconEntity(uid, scaleValue.Value, out var texture))
            return false;

        control = texture;

        return true;
    }
}

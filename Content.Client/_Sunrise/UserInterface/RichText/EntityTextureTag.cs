using System.Diagnostics.CodeAnalysis;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._Sunrise.UserInterface.RichText;

public sealed class EntityTextureTag : BaseTextureTag
{
    public override string Name => "enttex";

    public override bool TryGetControl(MarkupNode node, [NotNullWhen(true)] out Control? control)
    {
        control = null;

        if (!node.Attributes.TryGetValue("id", out var entProtoId))
            return false;

        if (!node.Attributes.TryGetValue("scale", out var scale) || !scale.TryGetLong(out var scaleValue))
        {
            scaleValue = 1;
        }

        if (!TryDrawIconEntity((EntProtoId) entProtoId.ToString(), scaleValue.Value, out var texture))
            return false;

        control = texture;

        return true;
    }
}

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.RichText;

using Robust.Shared.Utility;

namespace Content.Client._Sunrise.UserInterface.RichText;

public sealed class TextureTag : IMarkupTag
{
    public string Name => "tex";

    /// <inheritdoc/>
    public bool TryGetControl(MarkupNode node, [NotNullWhen(true)] out Control? control)
    {
        if (!node.Attributes.TryGetValue("path", out var rawPath))
        {
            control = null;
            return false;
        }

        if (!node.Attributes.TryGetValue("scale", out var scale) || !scale.TryGetLong(out var scaleValue))
        {
            scaleValue = 1;
        }

        var texture = new TextureRect();

        var path = rawPath.ToString();
        path = path.Replace("=", "");
        path = path.Replace(" ", "");
        path = path.Replace("\"", "");

        texture.TexturePath = path;
        texture.TextureScale = new Vector2(scaleValue.Value, scaleValue.Value);

        control = texture;
        return true;
    }
}

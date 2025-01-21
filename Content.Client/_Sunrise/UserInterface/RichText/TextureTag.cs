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

        var texture = new TextureRect();

        var path = rawPath.ToString();
        path = path.Replace("=", "");
        path = path.Replace(" ", "");
        path = path.Replace("\"", "");

        texture.TexturePath = path;
        texture.TextureScale = new Vector2(3, 3);

        control = texture;
        return true;
    }
}

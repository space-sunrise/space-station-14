using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Shared._Sunrise.SunriseCCVars;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Configuration;
using Robust.Shared.Utility;

namespace Content.Client._Sunrise.UserInterface.RichText;

public sealed class RadioIconTag : IMarkupTag
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IResourceCache _cache = default!;

    public string Name => "radicon";

    /// <inheritdoc/>
    public bool TryGetControl(MarkupNode node, [NotNullWhen(true)] out Control? control)
    {
        if (_cfg.GetCVar(SunriseCCVars.ChatIconsEnable))
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

            control = DrawIcon(rawPath.ToString(), scaleValue.Value);
        }
        else
        {
            if (!node.Attributes.TryGetValue("text", out var text))
            {
                control = null;
                return false;
            }

            if (!node.Attributes.TryGetValue("color", out var color))
            {
                control = null;
                return false;
            }

            control = DrawText(text.ToString(), color.ToString());
        }

        return true;
    }

    private Control DrawIcon(string path, long scaleValue)
    {
        var texture = new TextureRect();

        path = ClearString(path);

        texture.TexturePath = path;
        texture.TextureScale = new Vector2(scaleValue, scaleValue);

        return texture;
    }

    private Control DrawText(string text, string color)
    {
        var label = new Label();

        color = ClearString(color);
        text = ClearString(text);

        label.Text = text;
        label.FontColorOverride = Color.FromHex(color);
        label.FontOverride = new VectorFont(_cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Bold.ttf"), 13);

        return label;
    }

    /// <summary>
    /// Очищает строку от мусора, который приходит вместе с ней
    /// </summary>
    /// <remarks>
    /// Почему мне приходят строки в говне
    /// </remarks>
    private static string ClearString(string str)
    {
        str = str.Replace("=", "");
        str = str.Replace("\"", "");
        str = str.Trim();

        return str;
    }

}

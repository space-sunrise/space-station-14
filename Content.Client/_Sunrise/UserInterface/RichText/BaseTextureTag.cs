using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Client._Sunrise.UserInterface.Controls;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._Sunrise.UserInterface.RichText;

public abstract class BaseTextureTag : IMarkupTag
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

    public virtual string Name => "example";

    public abstract bool TryGetControl(MarkupNode node, [NotNullWhen(true)] out Control? control);

    protected static bool TryDrawIcon(string rawPath, long scaleValue, [NotNullWhen(true)] out Control? control)
    {
        var texture = new TextureRect();

        rawPath = ClearString(rawPath);

        texture.TexturePath = rawPath;
        texture.TextureScale = new Vector2(scaleValue, scaleValue);

        control = texture;
        return true;
    }

    protected bool TryDrawIcon(EntProtoId entProtoId, long scaleValue, [NotNullWhen(true)] out Control? control)
    {
        control = null;
        var texture = new TextureRect();

        entProtoId = ClearString(entProtoId);

        if (!_prototypeManager.TryIndex(entProtoId, out var prototype))
            return false;

        var spriteSystem = _entitySystemManager.GetEntitySystem<SpriteSystem>();
        texture.Texture = spriteSystem.Frame0(prototype);
        texture.TextureScale = new Vector2(scaleValue, scaleValue);

        control = texture;
        return true;
    }

    protected static bool TryDrawIconEntity(string stringUid, long scaleValue, [NotNullWhen(true)] out Control? control)
    {
        control = null;

        stringUid = ClearString(stringUid);

        if (!EntityUid.TryParse(stringUid, out var entityUid))
            return false;

        var spriteView = new SunriseStaticSpriteView(entityUid)
        {
            SetSize = new Vector2(48f, 32f),
            Scale = new Vector2(scaleValue, scaleValue),
        };

        control = spriteView;
        return true;
    }

    /// <summary>
    /// Очищает строку от мусора, который приходит вместе с ней
    /// </summary>
    /// <remarks>
    /// Почему мне приходят строки в говне
    /// </remarks>
    protected static string ClearString(string str)
    {
        str = str.Replace("=", "");
        str = str.Replace("\"", "");
        str = str.Trim();

        return str;
    }
}

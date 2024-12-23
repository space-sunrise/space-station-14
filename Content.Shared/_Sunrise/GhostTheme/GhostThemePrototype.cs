// © SUNRISE, An EULA/CLA with a hosting restriction, full text: https://github.com/space-sunrise/space-station-14/blob/master/CLA.txt
using System.Numerics;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Sunrise.GhostTheme;

[Prototype("ghostTheme")]
public sealed class GhostThemePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [DataField("name")]
    public string Name { get; private set; } = string.Empty;

    [DataField("scale")]
    public Vector2 Scale { get; private set; } = new(1, 1);

    [DataField("color")]
    public Color SpriteColor = Color.White;

    [DataField("sprite", required: true)]
    public SpriteSpecifier Sprite { get; private set; } = default!;
}

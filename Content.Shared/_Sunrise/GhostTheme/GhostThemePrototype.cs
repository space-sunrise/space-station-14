using System.Numerics;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Sunrise.GhostTheme;

[Prototype("ghostTheme")]
public sealed class GhostThemePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [DataField("scale")]
    public Vector2 Scale { get; private set; } = new(1, 1);

    [DataField("color")]
    public Color SpriteColor = Color.White;

    [DataField("sprites", required: true)]
    public List<SpriteSpecifier> Sprites { get; private set; } = default!;
}

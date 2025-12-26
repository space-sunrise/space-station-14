using System.Numerics;

namespace Content.Server._Sunrise.ScaleSprite;

[RegisterComponent]
public sealed partial class RescaleSpriteComponent : Component
{
    [DataField]
    public Vector2 Scale = Vector2.One;
}

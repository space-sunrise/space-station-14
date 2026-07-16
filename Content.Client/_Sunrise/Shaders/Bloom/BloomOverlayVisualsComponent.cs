using System.Numerics;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Content.Client._Sunrise.Shaders.Bloom;

/// <summary>
/// Marks a point light that should receive the client-side bloom effect.
/// </summary>
[RegisterComponent]
public sealed partial class BloomOverlayVisualsComponent : Component
{
    [DataField]
    public SpriteSpecifier Mask = new SpriteSpecifier.Rsi(
        new ResPath("_Sunrise/Effects/LightMasks/64.rsi"),
        "light_point");

    [DataField]
    public Vector2 Offset = new(0f, 0.45f);

    [DataField]
    public Color Color = Color.White;
}

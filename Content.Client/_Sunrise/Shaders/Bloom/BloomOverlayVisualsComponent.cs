using System.Numerics;
using Robust.Shared.Utility;
using static Robust.Shared.Utility.SpriteSpecifier;

namespace Content.Client._Sunrise.Shaders.Bloom;

/// <summary>
/// Marks a point light that should receive the client-side bloom effect.
/// </summary>
[RegisterComponent]
public sealed partial class BloomOverlayVisualsComponent : Component
{
    public static readonly SpriteSpecifier PointMask = new Rsi(
        new ResPath("_Sunrise/Effects/LightMasks/64.rsi"),
        "light_point");

    public static readonly Vector2 PointOffset = new(0f, 0.45f);
}

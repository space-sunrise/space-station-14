using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.ScaleSprite;

[RegisterComponent, NetworkedComponent]
public sealed partial class ScaleSpriteComponent : Component
{
    [DataField]
    public Vector2 Scale = Vector2.One;
}

[Serializable, NetSerializable]
public enum ScaleSpriteVisuals : byte
{
    Scale,
    OldScale,
}

using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Antags.Vampires.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class VampireBeamVisualComponent : Component
{
    [DataField(required: true)]
    public Angle AngleOffset;

    [DataField(required: true)]
    public bool SpriteIsVertical;

    [DataField(required: true)]
    public float Thickness;

    [DataField(required: true)]
    public float MinDistance;

    [DataField(required: true)]
    public float MinLength;
}

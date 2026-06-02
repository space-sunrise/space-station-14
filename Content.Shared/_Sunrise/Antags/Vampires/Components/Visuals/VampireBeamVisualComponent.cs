using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Antags.Vampires.Components.Visuals;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class VampireBeamVisualComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public Angle AngleOffset;

    [DataField(required: true), AutoNetworkedField]
    public bool SpriteIsVertical;

    [DataField(required: true), AutoNetworkedField]
    public float Thickness;

    [DataField(required: true), AutoNetworkedField]
    public float MinDistance;

    [DataField(required: true), AutoNetworkedField]
    public float MinLength;
}

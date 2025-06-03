using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Animations;

[RegisterComponent, NetworkedComponent]
public sealed partial class ContainerInteractionAnimationComponent : Component
{
    [DataField, ViewVariables] public float Variation = 0.1f;
    [DataField, ViewVariables] public float Duration = 0.2f;
}

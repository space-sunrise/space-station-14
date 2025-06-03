using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Animations;

[RegisterComponent, NetworkedComponent]
public sealed partial class ContainerInteractionAnimationComponent : Component
{
    private const float AnimationDuration = 0.2f;
    public const float Variation = 0.1f;

    [DataField]
    public float Duration = AnimationDuration;
}

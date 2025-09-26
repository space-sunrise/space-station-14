using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Drugs;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SeeingRainbowsWeakStatusEffectComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public float Intensity = 0.1f;
}

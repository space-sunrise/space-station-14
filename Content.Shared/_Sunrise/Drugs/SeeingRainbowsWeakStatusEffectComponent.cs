using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Drugs;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SeeingRainbowsWeakStatusEffectComponent : Component
{
    // по умолчанию для слабого эффекта:
    [DataField("intensity"), AutoNetworkedField] public float Intensity = 0.1f;
}

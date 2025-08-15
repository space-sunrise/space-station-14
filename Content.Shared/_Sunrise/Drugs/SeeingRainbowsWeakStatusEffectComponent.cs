using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Drugs;

[RegisterComponent, NetworkedComponent]
public sealed partial class SeeingRainbowsWeakStatusEffectComponent : Component
{
    // по умолчанию для слабого эффекта:
    [DataField("intensity")] public float Intensity = 0.1f;
}

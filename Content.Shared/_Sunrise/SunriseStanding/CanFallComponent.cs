using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.SunriseStanding;

[RegisterComponent, NetworkedComponent]
public sealed partial class CanFallComponent : Component
{
    [DataField]
    public float StaminaDamage = 0.2f;

    [DataField]
    public float MinimumStamina = 0.5f;

    [DataField]
    public float FallDistance = 1f;

    [DataField]
    public float FallVelocity = 5f;

    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(1);

    [ViewVariables]
    public bool IsMoving;
}

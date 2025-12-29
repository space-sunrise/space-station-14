using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.SunriseStanding;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CanFallComponent : Component
{
    [DataField, AutoNetworkedField]
    public float StaminaDamage = 0.2f;

    [DataField, AutoNetworkedField]
    public float MinimumStamina = 0.5f;

    [DataField, AutoNetworkedField]
    public float Friction = 0.3f;

    [DataField, AutoNetworkedField]
    public float VelocityModifier = 3f;

    [DataField, AutoNetworkedField]
    public TimeSpan Duration = TimeSpan.FromSeconds(1);
}

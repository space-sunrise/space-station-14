using Robust.Shared.GameStates;

namespace Content.Shared.Weapons;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EquipDelayComponent : Component
{
    /// <summary>
    /// Delay before the next attack after picking up a weapon, in seconds.
    /// </summary>
    [DataField("delay"), AutoNetworkedField]
    public float EquipDelayTime = 0.3f;
}
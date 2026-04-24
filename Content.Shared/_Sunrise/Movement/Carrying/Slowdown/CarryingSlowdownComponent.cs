using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Movement.Carrying.Slowdown;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), Access(typeof(CarryingSlowdownSystem))]
public sealed partial class CarryingSlowdownComponent : Component
{
    /// <summary>
    /// Movement speed modifier applied while walking.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public float WalkModifier = 1f;

    /// <summary>
    /// Movement speed modifier applied while sprinting.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public float SprintModifier = 1f;
}

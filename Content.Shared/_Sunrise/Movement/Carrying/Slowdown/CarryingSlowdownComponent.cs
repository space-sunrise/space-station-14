using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Movement.Carrying.Slowdown;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), Access(typeof(CarryingSlowdownSystem))]
public sealed partial class CarryingSlowdownComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public float WalkModifier = 1f;

    [ViewVariables, AutoNetworkedField]
    public float SprintModifier = 1f;
}

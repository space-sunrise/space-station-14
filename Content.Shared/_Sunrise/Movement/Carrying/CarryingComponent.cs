using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Movement.Carrying;

/// <summary>
/// Added to an entity when they are carrying somebody.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CarryingComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public EntityUid? Target;
}

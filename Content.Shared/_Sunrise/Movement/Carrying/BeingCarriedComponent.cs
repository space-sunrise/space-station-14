using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Movement.Carrying;

/// <summary>
/// Stores the carrier of an entity being carried.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BeingCarriedComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public EntityUid? Carrier;
}

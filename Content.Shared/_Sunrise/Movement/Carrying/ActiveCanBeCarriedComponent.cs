using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Movement.Carrying;

/// <summary>
/// Stores the carrier of an entity being carried.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class ActiveCanBeCarriedComponent : Component
{
    /// <summary>
    /// Entity currently carrying this entity.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? Carrier;
}

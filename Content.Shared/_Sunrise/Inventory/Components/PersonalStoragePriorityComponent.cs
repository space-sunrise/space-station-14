using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Inventory.Components;

/// <summary>
/// Tracks personal storage priorities for a player.
/// Maps storage entity to priority item entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class PersonalStoragePriorityComponent : Component
{
    /// <summary>
    /// Dictionary mapping storage entity to priority item entity.
    /// </summary>
    [AutoNetworkedField]
    public Dictionary<EntityUid, EntityUid> Priorities = new();
}

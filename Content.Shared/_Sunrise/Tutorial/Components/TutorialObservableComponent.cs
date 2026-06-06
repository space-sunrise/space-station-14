using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Tutorial.Components;

/// <summary>
/// Marks an entity whose tutorial-relevant events are being observed by players.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TutorialObservableComponent : Component
{
    /// <summary>
    /// Tutorial players currently listening for events from this entity.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public HashSet<EntityUid> Observers = [];
}

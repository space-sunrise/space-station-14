using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Tutorial.Components;

/// <summary>
/// Marker for the entity used as the player spawn point on a tutorial grid.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class TutorialSpawnPointComponent : Component
{
}

using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared._Sunrise.Tutorial.Components.Trackers;

/// <summary>
/// Tracks distance traveled by a tutorial player for distance-based conditions.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TutorialDistanceTrackerComponent : Component
{
    /// <summary>
    /// Last recorded world position used as the distance accumulation origin.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public Vector2? LastPosition;

    /// <summary>
    /// Total distance accumulated since the tracker was created or reset.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public float Distance;
}

using System.Collections.Generic;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Tutorial.Components.Trackers;

/// <summary>
/// Stores event counters and observed entities for the active tutorial step.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TutorialTrackerComponent : Component
{
    /// <summary>
    /// Recorded event counts keyed by condition counter and target prototype.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public Dictionary<(string Key, EntProtoId Target), int> Counters = new();

    /// <summary>
    /// Entity prototypes that should be observed for event-listened conditions.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public HashSet<EntProtoId> TargetPrototypes = [];

    /// <summary>
    /// Entities currently subscribed to this tutorial player's event tracking.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public HashSet<EntityUid> ObservedEntities = [];
}

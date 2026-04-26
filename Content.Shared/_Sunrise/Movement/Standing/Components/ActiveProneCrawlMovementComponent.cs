using System.Numerics;
using Content.Shared._Sunrise.Movement.Standing.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Movement.Standing.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true), AutoGenerateComponentPause]
[Access(typeof(ProneCrawlMovementController), typeof(SharedSunriseStandingStateSystem))]
public sealed partial class ActiveProneCrawlMovementComponent : Component
{
    /// <summary>
    /// Start time of the current prone pull.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public TimeSpan PullStartTime;

    /// <summary>
    /// End time of the current prone pull.
    /// </summary>
    [ViewVariables, AutoNetworkedField, AutoPausedField]
    public TimeSpan PullEndTime;

    /// <summary>
    /// Earliest time when the next prone pull can start.
    /// </summary>
    [ViewVariables, AutoNetworkedField, AutoPausedField]
    public TimeSpan NextPullTime;

    /// <summary>
    /// Direction chosen for the current prone pull.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public Vector2 PullDirection;

    /// <summary>
    /// Velocity applied during the current prone pull.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public Vector2 PullVelocity;

    /// <summary>
    /// Whether the entity is currently in the active part of the pull.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public bool IsPulling;
}

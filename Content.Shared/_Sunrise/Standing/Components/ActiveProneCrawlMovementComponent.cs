using System.Numerics;
using Content.Shared._Sunrise.Standing.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Sunrise.Standing.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
[Access(typeof(SharedProneCrawlMovementController), typeof(SharedSunriseStandingStateSystem))]
public sealed partial class ActiveProneCrawlMovementComponent : Component
{
    /// <summary>
    /// Start time of the current prone pull.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    public TimeSpan PullStartTime;

    /// <summary>
    /// End time of the current prone pull.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    public TimeSpan PullEndTime;

    /// <summary>
    /// Earliest time when the next prone pull can start.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    public TimeSpan NextPullTime;

    /// <summary>
    /// Direction chosen for the current prone pull.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Vector2 PullDirection = Vector2.Zero;

    /// <summary>
    /// Velocity applied during the current prone pull.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Vector2 PullVelocity = Vector2.Zero;

    /// <summary>
    /// Whether the entity is currently in the active part of the pull.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsPulling;
}

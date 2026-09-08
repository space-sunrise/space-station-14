using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Movement.Standing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true), AutoGenerateComponentPause]
[Access(typeof(ProneCrawlMovementController), typeof(ProneCrawlSystem))]
public sealed partial class ActiveProneCrawlMovementComponent : Component
{
    /// <summary>
    /// Когда закончится текущий рывок.
    /// </summary>
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan PullEnd;

    /// <summary>
    /// Когда можно начинать следующий рывок.
    /// </summary>
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan NextPull;

    /// <summary>
    /// Направление текущего рывка.
    /// </summary>
    [AutoNetworkedField]
    public Vector2 Direction;

    /// <summary>
    /// Скорость текущего рывка.
    /// </summary>
    [AutoNetworkedField]
    public Vector2 Velocity;

    /// <summary>
    /// Идёт ли рывок прямо сейчас.
    /// </summary>
    [AutoNetworkedField]
    public bool Pulling;
}

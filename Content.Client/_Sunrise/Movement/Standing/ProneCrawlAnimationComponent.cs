using System.Numerics;

namespace Content.Client._Sunrise.Movement.Standing;

[RegisterComponent, Access(typeof(ProneCrawlAnimationSystem))]
public sealed partial class ProneCrawlAnimationComponent : Component
{
    /// <summary>
    /// Whether the resting sprite offset/scale has already been captured for this entity.
    /// </summary>
    [ViewVariables]
    public bool BaseStateCaptured;

    /// <summary>
    /// Sprite offset captured before any prone-crawl animation was applied.
    /// Used to restore the sprite to its rest state.
    /// </summary>
    [ViewVariables]
    public Vector2 BaseOffset;

    /// <summary>
    /// Sprite scale captured before any prone-crawl animation was applied.
    /// </summary>
    [ViewVariables]
    public Vector2 BaseScale = Vector2.One;
}

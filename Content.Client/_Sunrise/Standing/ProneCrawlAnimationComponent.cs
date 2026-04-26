using System.Numerics;

namespace Content.Client._Sunrise.Standing;

[RegisterComponent, Access(typeof(ProneCrawlAnimationSystem))]
public sealed partial class ProneCrawlAnimationComponent : Component
{
    [ViewVariables]
    public bool BaseStateCaptured;

    [ViewVariables]
    public Vector2 BaseOffset;

    [ViewVariables]
    public Vector2 BaseScale = Vector2.One;
}

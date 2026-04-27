using System.Numerics;
using Content.Shared._Sunrise.Movement.Standing.Systems;
using Robust.Shared.Audio;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Shared.Stunnable;

public sealed partial class CrawlerComponent
{
    /// <summary>
    /// Distance of a single prone pull in tiles before slowdown clamps it.
    /// </summary>
    [DataField, AutoNetworkedField, Access(typeof(ProneCrawlMovementController))]
    public float PullDistance = 0.7f;

    /// <summary>
    /// Duration of a single prone pull.
    /// </summary>
    [DataField, AutoNetworkedField, Access(typeof(ProneCrawlMovementController))]
    public TimeSpan PullDuration = TimeSpan.FromSeconds(0.25f);

    /// <summary>
    /// Pause between prone pulls while movement input is held.
    /// </summary>
    [DataField, AutoNetworkedField, Access(typeof(ProneCrawlMovementController))]
    public TimeSpan PullPause = TimeSpan.FromSeconds(0.3f);

    /// <summary>
    /// Sound played right before the prone pull starts.
    /// </summary>
    [DataField, AutoNetworkedField, Access(typeof(ProneCrawlMovementController))]
    public SoundSpecifier? PullStartSound = new SoundPathSpecifier("/Audio/_Sunrise/Footstep/crawl.ogg",
        AudioParams.Default.WithVolume(-4f).WithMaxDistance(6f).WithVariation(0.3f));

    /// <summary>
    /// Sound played after the prone pull finishes.
    /// </summary>
    [DataField, AutoNetworkedField, Access(typeof(ProneCrawlMovementController))]
    public SoundSpecifier? PullEndSound;

    /// <summary>
    /// Animation key used for the prone pull animation.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string AnimationKey = "prone-crawl-pull";

    /// <summary>
    /// Backward animation offset distance in tiles during a prone pull.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float AnimationPullBackDistance = 0.08f;

    /// <summary>
    /// Vector scale multiplier applied to the sprite during the prone pull animation.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Vector2 AnimationPullScaleMultiplier = new(1.05f, 0.95f);
}

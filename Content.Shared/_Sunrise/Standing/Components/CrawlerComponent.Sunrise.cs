using Content.Shared._Sunrise.Standing.Systems;
using Robust.Shared.Audio;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Shared.Stunnable;

public sealed partial class CrawlerComponent
{
    /// <summary>
    /// Fallback sound used by the footstep system while this entity crawls in the downed state.
    /// </summary>
    [DataField, AutoNetworkedField, Access(typeof(SharedSunriseStandingStateSystem), typeof(SharedProneCrawlMovementController))]
    public SoundSpecifier? CrawlingSound;

    /// <summary>
    /// Distance of a single prone pull in tiles before slowdown clamps it.
    /// </summary>
    [DataField, AutoNetworkedField, Access(typeof(SharedProneCrawlMovementController))]
    public float CrawlPullDistance = 0.7f;

    /// <summary>
    /// Duration of a single prone pull.
    /// </summary>
    [DataField, AutoNetworkedField, Access(typeof(SharedProneCrawlMovementController))]
    public TimeSpan CrawlPullDuration = TimeSpan.FromSeconds(0.35f);

    /// <summary>
    /// Pause between prone pulls while movement input is held.
    /// </summary>
    [DataField, AutoNetworkedField, Access(typeof(SharedProneCrawlMovementController))]
    public TimeSpan CrawlPullPause = TimeSpan.FromSeconds(0.4f);

    /// <summary>
    /// Sound played at the start of each prone pull.
    /// </summary>
    [DataField, AutoNetworkedField, Access(typeof(SharedProneCrawlMovementController))]
    public SoundSpecifier? CrawlPullSound = new SoundPathSpecifier("/Audio/_Sunrise/Footstep/crawl.ogg",
        AudioParams.Default.WithVolume(-4f).WithMaxDistance(6f).WithVariation(0.3f));
}

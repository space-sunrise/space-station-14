namespace Content.Client._Sunrise.Movement.Standing;

[RegisterComponent, Access(typeof(SunriseStandingStateSystem))]
public sealed partial class ProneCrawlVisualsComponent : Component
{
    /// <summary>
    /// Whether the entity had a sprite direction override prior to entering prone-crawl visuals,
    /// used to restore the original state on shutdown.
    /// </summary>
    [ViewVariables]
    public bool HadDirectionOverride;

    /// <summary>
    /// Cached direction override that was active before prone-crawl visuals were applied.
    /// </summary>
    [ViewVariables]
    public Direction DirectionOverride;
}

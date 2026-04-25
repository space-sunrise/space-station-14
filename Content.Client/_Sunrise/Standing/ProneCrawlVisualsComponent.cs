namespace Content.Client._Sunrise.Standing;

[RegisterComponent, Access(typeof(SunriseStandingStateSystem))]
public sealed partial class ProneCrawlVisualsComponent : Component
{
    [ViewVariables]
    public bool HadDirectionOverride;

    [ViewVariables]
    public Direction DirectionOverride;
}

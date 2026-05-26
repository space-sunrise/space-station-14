namespace Content.Shared.VentCrawl.Components;

[RegisterComponent]
public sealed partial class VentCrawlJunctionComponent : Component
{
    /// <summary>
    ///     The angles to connect to.
    /// </summary>
    [DataField("degrees")] public List<Angle> Degrees = new();
}

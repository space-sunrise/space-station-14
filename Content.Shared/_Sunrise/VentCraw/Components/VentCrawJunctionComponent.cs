namespace Content.Shared._Sunrise.VentCraw.Components;

[RegisterComponent, Virtual]
public partial class VentCrawJunctionComponent : Component
{
    /// <summary>
    ///     The angles to connect to.
    /// </summary>
    [DataField("degrees")] public List<Angle> Degrees = new();
}

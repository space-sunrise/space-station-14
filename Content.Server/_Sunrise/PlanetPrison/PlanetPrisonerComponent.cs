namespace Content.Server._Sunrise.PlanetPrison
{
    [RegisterComponent]
    public sealed partial class PlanetPrisonerComponent : Component
    {
        [DataField("firstMindAdded")]
        public bool FirstMindAdded = false;
    }
}

namespace Content.Server._Sunrise.PlanetPrison
{
    using Robust.Shared.Map;

    [RegisterComponent]
    public sealed partial class PlanetPrisonerComponent : Component
    {
        [DataField("firstMindAdded")]
        public bool FirstMindAdded = false;

        /// <summary>
        /// ID карты, на которой находился игрок (для корректного выбора станции в NewLife)
        /// </summary>
        [DataField("mapId")]
        public MapId MapId = MapId.Nullspace;
    }
}

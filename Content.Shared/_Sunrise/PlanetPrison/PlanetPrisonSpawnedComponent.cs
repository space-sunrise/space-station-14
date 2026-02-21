using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared._Sunrise.PlanetPrison;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PlanetPrisonSpawnedComponent : Component
{
    [DataField("mapId"), AutoNetworkedField]
    public MapId MapId = MapId.Nullspace;
}

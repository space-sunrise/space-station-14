using Content.Server.Maps;
using Content.Shared.Maps;
using Content.Shared.Parallax.Biomes;
using Content.Shared.Whitelist;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.PlanetPrison;


[RegisterComponent]
public sealed partial class PlanetPrisonStationComponent : Component
{
    /// <summary>
    /// Game maps that can be used for the planet prison.
    /// </summary>
    [DataField(required: true)]
    public HashSet<ProtoId<GameMapPrototype>> Stations = [];

    public MapId MapId = MapId.Nullspace;

    [DataField]
    public EntityUid Entity = EntityUid.Invalid;

    [DataField(required: true)]
    public List<ProtoId<BiomeTemplatePrototype>> Biomes = [];

    [DataField]
    public EntityWhitelist? ShuttleWhitelist;

    [DataField]
    public EntityUid PrisonGrid = EntityUid.Invalid;
}

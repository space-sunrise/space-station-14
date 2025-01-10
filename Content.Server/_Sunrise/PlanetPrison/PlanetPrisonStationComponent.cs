using Content.Server.Maps;
using Content.Shared.Parallax.Biomes;
using Content.Shared.Whitelist;
using Robust.Shared.Map;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Server._Sunrise.PlanetPrison;
[RegisterComponent]
public sealed partial class PlanetPrisonStationComponent : Component
{
    [DataField(customTypeSerializer: typeof(PrototypeIdHashSetSerializer<GameMapPrototype>), required: true)]
    public HashSet<string> Stations = [];

    public MapId MapId = MapId.Nullspace;

    [DataField]
    public EntityUid Entity = EntityUid.Invalid;

    [DataField(customTypeSerializer: typeof(PrototypeIdListSerializer<BiomeTemplatePrototype>), required: true)]
    public List<string> Biomes = [];

    [DataField("shuttleWhitelist")]
    public EntityWhitelist? ShuttleWhitelist;

    [DataField]
    public EntityUid PrisonGrid = EntityUid.Invalid;
}

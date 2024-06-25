using Content.Server.Maps;
using Content.Shared.Parallax.Biomes;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Server._Sunrise.SpacePrison;
[RegisterComponent]
public sealed partial class SpacePrisonStationComponent : Component
{
    [DataField("stations", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<GameMapPrototype>), required: true)]
    public HashSet<string> Stations = new(0);

    public MapId MapId = MapId.Nullspace;

    [DataField]
    public EntityUid Entity = EntityUid.Invalid;

    [DataField(customTypeSerializer: typeof(PrototypeIdListSerializer<BiomeTemplatePrototype>))]
    public List<string> Bioms = new() { "Grasslands", "LowDesert", "Snow", "Asteroid", "Caves", "Shadow", "Lava", "Continental" }

}

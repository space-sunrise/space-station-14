using Content.Shared.Maps;
using Content.Shared.Parallax.Biomes;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.PlanetPrison;

[RegisterComponent]
public partial class PlanetPrisonSharedComponent : Component
{
    [DataField(required: true)]
    public HashSet<ProtoId<GameMapPrototype>> StationsModern = [];

    [DataField(required: true)]
    public HashSet<ProtoId<GameMapPrototype>> StationsOld = [];

    [DataField(required: true)]
    public List<ProtoId<BiomeTemplatePrototype>> Biomes = [];

    /// <summary>
    /// Минимальное количество игроков, необходимое для запуска любой карты тюрьмы
    /// </summary>
    [DataField]
    public int MinPlayersRequired = 2;
}

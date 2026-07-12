using Content.Server.Maps;
using Content.Shared.Maps;
using Content.Shared.Parallax.Biomes;
using Content.Shared.Whitelist;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.PlanetPrison;

/// <summary>
/// Configures creation and lifetime of a planet prison on a map separate from the main station.
/// </summary>
/// <remarks>
/// Attach this component to the main station prototype that owns the prison module. Each entry in
/// <see cref='Stations'/> is a game map whose station must reference a player-joinable map configuration.
/// </remarks>
[RegisterComponent]
public sealed partial class PlanetPrisonStationComponent : Component
{
    /// <summary>
    /// Game maps that can be used for the planet prison.
    /// </summary>
    [DataField(required: true)]
    public HashSet<ProtoId<GameMapPrototype>> Stations = [];

    /// <summary>
    /// Runtime map identifier assigned after the selected game map is loaded.
    /// </summary>
    public MapId MapId = MapId.Nullspace;

    /// <summary>
    /// Optional legacy entity owned by this module and removed during shutdown.
    /// </summary>
    [DataField]
    public EntityUid Entity = EntityUid.Invalid;

    /// <summary>
    /// Biomes from which the generated planet surrounding the prison grid is selected.
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<BiomeTemplatePrototype>> Biomes = [];

    /// <summary>
    /// Restricts which shuttles may select the generated planet as an FTL destination.
    /// </summary>
    [DataField]
    public EntityWhitelist? ShuttleWhitelist;

    /// <summary>
    /// Runtime grid loaded from the selected prison game map.
    /// </summary>
    [DataField]
    public EntityUid PrisonGrid = EntityUid.Invalid;
}

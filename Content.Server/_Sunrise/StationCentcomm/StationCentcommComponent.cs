using Content.Server.Maps;
using Content.Shared.Maps;
using Content.Shared.Whitelist;
using Robust.Shared.Map;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._Sunrise.StationCentComm;

/// <summary>
/// Configures creation and lifetime of Central Command on a map separate from the main station.
/// </summary>
/// <remarks>
/// Attach this component to the main station prototype that owns Central Command. The selected game map
/// must create a station carrying the matching player-joinable map configuration.
/// </remarks>
[RegisterComponent]
public sealed partial class StationCentCommComponent : Component
{
    /// <summary>
    /// Game map prototype loaded as the separate Central Command map.
    /// </summary>
    [DataField(customTypeSerializer:typeof(PrototypeIdSerializer<GameMapPrototype>), required: true)]
    public string Station = default!;

    /// <summary>
    /// Optional pre-existing entity whose map is adopted instead of loading <see cref='Station'/>.
    /// </summary>
    [DataField]
    public EntityUid Entity = EntityUid.Invalid;

    /// <summary>
    /// Restricts which shuttles may select Central Command as an FTL destination.
    /// </summary>
    [DataField]
    public EntityWhitelist? ShuttleWhitelist;

    /// <summary>
    /// Runtime map identifier resolved from <see cref='Entity'/> or assigned after loading <see cref='Station'/>.
    /// </summary>
    public MapId MapId = MapId.Nullspace;
}

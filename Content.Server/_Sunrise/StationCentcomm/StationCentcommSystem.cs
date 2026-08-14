using Content.Server.GameTicking;
using Content.Server._Sunrise.GameTicking.PlayerJoinableMaps;
using Content.Server.Maps;
using Content.Server.Shuttles.Systems;
using Content.Shared._Sunrise.AlwaysPoweredMap;
using Content.Shared._Sunrise.GameTicking.PlayerJoinableMaps;
using Content.Shared.Maps;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.StationCentComm;

/// <summary>
/// Creates Central Command as a separate map and manages its lifetime.
/// </summary>
/// <remarks>
/// The configured game map is loaded during component initialization. Its station prototype must carry
/// <see cref='PlayerJoinableMapComponent'/> when Central Command jobs should be available to players.
/// </remarks>
public sealed partial class StationCentCommSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly PlayerJoinableMapSystem _playerJoinableMap = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ShuttleSystem _shuttle = default!;
    [Dependency] private readonly MapSystem _map = default!;

    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        _sawmill = Logger.GetSawmill("station.centcomm");
        SubscribeLocalEvent<StationCentCommComponent, ComponentShutdown>(OnCentcommShutdown);
        SubscribeLocalEvent<StationCentCommComponent, ComponentInit>(OnCentcommInit);
    }

    private void OnCentcommShutdown(EntityUid uid, StationCentCommComponent component, ComponentShutdown args)
    {
        QueueDel(component.Entity);
        component.Entity = EntityUid.Invalid;

        if (_mapManager.MapExists(component.MapId))
            _mapManager.DeleteMap(component.MapId);

        component.MapId = MapId.Nullspace;
    }

    private void OnCentcommInit(EntityUid uid, StationCentCommComponent component, ComponentInit args)
    {
        // Post mapinit? fancy
        if (TryComp<TransformComponent>(component.Entity, out var xform))
        {
            component.MapId = xform.MapID;
            return;
        }

        AddCentcomm(component);
    }

    /// <summary>
    /// Loads the configured Central Command game map and configures its map-wide FTL and power behavior.
    /// </summary>
    /// <remarks>
    /// Loading is skipped when the referenced player-joinable station is not currently spawnable. Multiple
    /// owner components reuse the first Central Command map instead of creating duplicate maps.
    /// </remarks>
    /// <param name='component'>Main-station configuration that owns the external Central Command map.</param>
    private void AddCentcomm(StationCentCommComponent component)
    {
        var query = AllEntityQuery<StationCentCommComponent>();

        while (query.MoveNext(out var otherComp))
        {
            if (otherComp == component)
                continue;

            component.MapId = otherComp.MapId;
            return;
        }

        if (component.Station != null)
        {
            if (_prototypeManager.TryIndex<GameMapPrototype>(component.Station, out var gameMap))
            {
                if (!_playerJoinableMap.CanSpawnGameMap(gameMap))
                    return;

                _gameTicker.LoadGameMap(gameMap, out var mapId);

                var mapEnt = _map.GetMapOrInvalid(mapId);

                if (_shuttle.TryAddFTLDestination(mapId, true, out var ftlDestination))
                    ftlDestination.Whitelist = component.ShuttleWhitelist;

                EnsureComp<AlwaysPoweredMapComponent>(mapEnt);

                _map.InitializeMap(mapId);
            }
            else
            {
                _sawmill.Warning("No Centcomm map found, skipping setup.");
            }
        }
    }
}

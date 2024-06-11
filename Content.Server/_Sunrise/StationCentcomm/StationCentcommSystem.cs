using Content.Server._Sunrise.StationCentComm;
using Content.Server.GameTicking;
using Content.Server.Maps;
using Content.Server.Shuttles.Systems;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.StationCentComm;

public sealed partial class StationCentCommSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
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

        var mapId = _mapManager.CreateMap();
        _mapManager.AddUninitializedMap(mapId);
        component.MapId = mapId;

        if (_prototypeManager.TryIndex<GameMapPrototype>(component.Station, out var gameMap))
        {
            _gameTicker.LoadGameMap(gameMap, mapId, null);

             if (_shuttle.TryAddFTLDestination(mapId, true, out var ftlDestination))
                 ftlDestination.Whitelist = component.ShuttleWhitelist;

             _map.InitializeMap(mapId);
        }
        else
        {
            _sawmill.Warning("No Centcomm map found, skipping setup.");
        }
    }
}

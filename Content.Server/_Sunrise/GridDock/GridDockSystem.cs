using System.Numerics;
using Content.Server._Sunrise.RoundStartFtl;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Components;
using Content.Server.Station.Events;
using Content.Server.Station.Systems;
using Content.Shared.Station.Components;
using Robust.Shared.EntitySerialization.Systems;

namespace Content.Server._Sunrise.GridDock;

public sealed class GridDockSystem : EntitySystem
{
    [Dependency] private readonly MapLoaderSystem _loader = default!;
    [Dependency] private readonly ShuttleSystem _shuttles = default!;
    [Dependency] private readonly StationSystem _station = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SpawnGridAndDockToStationComponent, StationPostInitEvent>(OnStationPostInit);
    }

    private void OnStationPostInit(EntityUid uid, SpawnGridAndDockToStationComponent component, StationPostInitEvent args)
    {
        if (component.GridPath is null)
            return;

        var ftlMap = _shuttles.EnsureFTLMap();
        var xformMap = Transform(ftlMap);
        if (!_loader.TryLoadGrid(xformMap.MapID,
                component.GridPath.Value,
                out var rootUid,
                offset: new Vector2(500, 500)))
            return;

        if (!TryComp<ShuttleComponent>(rootUid.Value.Owner, out var shuttleComp))
            return;

        if (!TryComp<StationDataComponent>(uid, out var stationData))
            return;

        var target = _station.GetLargestGrid((uid, stationData));

        if (target == null)
        {
            Log.Error($"GridDockSystem: No target grid found for {ToPrettyString(uid)}");
            return;
        }

        _shuttles.FTLToDock(
            rootUid.Value.Owner,
            shuttleComp,
            target.Value,
            5f,
            5f,
            priorityTag: component.PriorityTag,
            ignored: true);
    }
}

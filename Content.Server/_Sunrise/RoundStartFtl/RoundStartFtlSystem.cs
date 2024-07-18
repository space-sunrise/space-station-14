using Content.Server.Chat.Systems;
using Content.Server.Pinpointer;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Systems;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;

namespace Content.Server._Sunrise.RoundStartFtl;

/// <summary>
/// This handles...
/// </summary>
public sealed class RoundStartFtlSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly MapLoaderSystem _loader = default!;
    [Dependency] private readonly ShuttleSystem _shuttles = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RoundstartFtlTargetComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid targetUid, RoundstartFtlTargetComponent ftlTargetComponent, MapInitEvent args)
    {
        var mapId = _mapManager.CreateMap();
        if (!_loader.TryLoad(mapId,
                ftlTargetComponent.Path,
                out var rootUids,
                new MapLoadOptions()
                {
                    DoMapInit = true,
                }))
            return;

        if (!TryComp<ShuttleComponent>(rootUids[0], out var shuttleComp))
            return;
        var xform = Transform(targetUid);
        if (!TryComp(rootUids[0], out PhysicsComponent? shuttlePhysics))
            return;
        var targetCoordinates = new EntityCoordinates(xform.MapUid!.Value, _transform.GetWorldPosition(xform)).Offset(Angle.Zero.RotateVec(-shuttlePhysics.LocalCenter));
        _shuttles.FTLToCoordinates(rootUids[0], shuttleComp, targetCoordinates, Angle.Zero);
        Log.Debug($"onmapinit, ftlsuccessful: {rootUids[0]}, {targetCoordinates}");
    }
}

using System.Numerics;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;

namespace Content.Server._Sunrise.RoundStartFtl;

/// <summary>
/// This handles...
/// </summary>
public sealed class RoundStartFtlSystem : EntitySystem
{
    [Dependency] private readonly MapLoaderSystem _loader = default!;
    [Dependency] private readonly ShuttleSystem _shuttles = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RoundstartFtlTargetComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid targetUid, RoundstartFtlTargetComponent ftlTargetComponent, MapInitEvent args)
    {
        if (ftlTargetComponent.GridPath is null)
            return;

        var ftlMap = _shuttles.EnsureFTLMap();
        var xformMap = Transform(ftlMap);
        if (!_loader.TryLoadGrid(xformMap.MapID,
                ftlTargetComponent.GridPath.Value,
                out var rootUid,
                offset: new Vector2(500, 500)))
            return;

        if (!TryComp<ShuttleComponent>(rootUid.Value.Owner, out var shuttleComp))
            return;
        var xform = Transform(targetUid);
        if (!TryComp(rootUid.Value.Owner, out PhysicsComponent? shuttlePhysics))
            return;
        var targetCoordinates = new EntityCoordinates(xform.MapUid!.Value, _transform.GetWorldPosition(xform)).Offset(Angle.Zero.RotateVec(-shuttlePhysics.LocalCenter));
        _shuttles.FTLToCoordinates(rootUid.Value.Owner, shuttleComp, targetCoordinates, Angle.Zero, 0, 0);
        Log.Debug($"onmapinit, ftlsuccessful: {rootUid.Value.Owner}, {targetCoordinates}");
    }
}

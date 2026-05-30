using Content.Server.Fluids.EntitySystems;
using Content.Server.Polymorph.Components;
using Content.Server.Polymorph.Systems;
using Content.Shared._Sunrise.Antags.Vampires.Components;
using Content.Shared._Sunrise.Antags.Vampires.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Maps;
using Robust.Shared.Map.Components;

namespace Content.Server._Sunrise.Antags.Vampires.Systems;

public sealed class SanguinePoolSystem : SharedSanguinePoolSystem
{
    private const int MaxPoolsProcessedPerUpdate = 64;

    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly PuddleSystem _puddle = default!;

    private EntityQuery<MapGridComponent> _gridQuery;

    public override void Initialize()
    {
        base.Initialize();
        _gridQuery = GetEntityQuery<MapGridComponent>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var processed = 0;
        var query = EntityQueryEnumerator<SanguinePoolComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            if (processed++ >= MaxPoolsProcessedPerUpdate)
                break;

            Entity<SanguinePoolComponent, TransformComponent> ent = (uid, comp, xform);
            if (ShouldForceRevert(ent))
                continue;

            if (ent.Comp1.TrailPrototype is null)
                continue;

            // Spawn more frequently: once per entered tile (but don't duplicate if the tile already has a blood puddle).
            if (ent.Comp2.GridUid is not { } gridUid || !_gridQuery.TryComp(gridUid, out var gridComp))
                continue;

            var tile = _map.CoordinatesToTile(gridUid, gridComp, ent.Comp2.Coordinates);
            if (ent.Comp1.LastTrail is { } last && last.Grid == gridUid && last.Tile == tile)
                continue;

            ent.Comp1.LastTrail = (gridUid, tile);

            var tileCoords = _map.GridTileToLocal(gridUid, gridComp, tile);
            if (_puddle.TryGetPuddle(_map.GetTileRef((gridUid, gridComp), tile), out var puddle))
            {
                var solution = new Solution { Contents = [new ReagentQuantity(ent.Comp1.TrailReagent, ent.Comp1.TrailReagentQuantity)] };
                _puddle.TryAddSolution(puddle, solution);
                continue;
            }

            Spawn(ent.Comp1.TrailPrototype, tileCoords);
        }
    }

    private bool ShouldForceRevert(Entity<SanguinePoolComponent, TransformComponent> ent)
    {
        var gridUid = ent.Comp2.GridUid;
        var inSpace = gridUid is null;

        if (!inSpace && gridUid is not null)
        {
            if (!_gridQuery.TryComp(gridUid.Value, out var grid) ||
                !_map.TryGetTileRef(gridUid.Value, grid, ent.Comp2.Coordinates, out var tileRef) ||
                _turf.IsSpace(tileRef))
            {
                inSpace = true;
            }
        }

        if (!inSpace)
            return false;

        if (TryComp<PolymorphedEntityComponent>(ent, out var polymorph))
            _polymorph.Revert((ent.Owner, polymorph));

        return true;
    }
}

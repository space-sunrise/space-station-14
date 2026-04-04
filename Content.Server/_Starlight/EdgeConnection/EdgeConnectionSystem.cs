using Content.Shared._Starlight.EdgeConnection;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

namespace Content.Server._Starlight.EdgeConnection;

/// <summary>
/// Handles visual edge connections between entities placed adjacent to each other.
/// Updates appearance data based on neighboring entities with matching connection keys.
/// </summary>
public sealed class EdgeConnectionSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EdgeConnectionComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        SubscribeLocalEvent<EdgeConnectionComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<EdgeConnectionComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<EdgeConnectionComponent, EntityTerminatingEvent>(OnTerminating);
        SubscribeLocalEvent<EdgeConnectionComponent, MoveEvent>(OnMove);
    }

    private void OnInit(Entity<EdgeConnectionComponent> ent, ref ComponentInit args)
    {
        UpdateConnections(ent);
        UpdateNeighbors(ent);
    }

    private void OnAnchorChanged(Entity<EdgeConnectionComponent> ent, ref AnchorStateChangedEvent args)
    {
        UpdateConnections(ent);
        UpdateNeighbors(ent);
    }

    private void OnShutdown(Entity<EdgeConnectionComponent> ent, ref ComponentShutdown args)
    {
        // Update neighbors when this entity is removed
        UpdateNeighbors(ent);
    }

    private void OnTerminating(Entity<EdgeConnectionComponent> ent, ref EntityTerminatingEvent args)
    {
        // Update neighbors when entity is completely destroyed or deleted
        UpdateNeighbors(ent);
    }

    private void OnMove(Entity<EdgeConnectionComponent> ent, ref MoveEvent args)
    {
        if (args.NewRotation == args.OldRotation)
            return;

        UpdateConnections(ent);
        UpdateNeighbors(ent);
    }

    private void UpdateConnections(Entity<EdgeConnectionComponent> ent)
    {
        var xform = Transform(ent);

        if (!xform.Anchored || !TryComp<MapGridComponent>(xform.GridUid, out var grid))
        {
            _appearance.SetData(ent, EdgeConnectionVisuals.ConnectionMask, EdgeConnectionFlags.None);
            return;
        }

        var mask = EdgeConnectionFlags.None;
        var tile = _map.TileIndicesFor(xform.GridUid.Value, grid, xform.Coordinates);
        var allowed = ent.Comp.AllowedDirections;

        TrySetConnectionLocal(ent, ref mask, allowed, EdgeConnectionFlags.East, tile, xform.GridUid.Value, grid, xform.LocalRotation);
        TrySetConnectionLocal(ent, ref mask, allowed, EdgeConnectionFlags.West, tile, xform.GridUid.Value, grid, xform.LocalRotation);
        TrySetConnectionLocal(ent, ref mask, allowed, EdgeConnectionFlags.North, tile, xform.GridUid.Value, grid, xform.LocalRotation);
        TrySetConnectionLocal(ent, ref mask, allowed, EdgeConnectionFlags.South, tile, xform.GridUid.Value, grid, xform.LocalRotation);

        _appearance.SetData(ent, EdgeConnectionVisuals.ConnectionMask, mask);
    }

    private void TrySetConnectionLocal(
        Entity<EdgeConnectionComponent> ent,
        ref EdgeConnectionFlags mask,
        EdgeConnectionFlags allowedLocal,
        EdgeConnectionFlags localDirection,
        Vector2i tile,
        EntityUid gridUid,
        MapGridComponent grid,
        Angle localRotation)
    {
        if ((allowedLocal & localDirection) == 0)
            return;

        var worldDirection = LocalToWorldDirection(localDirection, localRotation);
        var offset = DirectionToOffset(worldDirection);
        var neighborTile = tile + offset;

        if (!HasMatchingNeighbor(ent, gridUid, grid, neighborTile, ent.Comp.ConnectionKey))
            return;

        mask |= localDirection;
    }

    private static Direction LocalToWorldDirection(EdgeConnectionFlags localDirection, Angle localRotation)
    {
        var localDir = localDirection switch
        {
            EdgeConnectionFlags.East => Direction.East,
            EdgeConnectionFlags.West => Direction.West,
            EdgeConnectionFlags.North => Direction.North,
            EdgeConnectionFlags.South => Direction.South,
            _ => Direction.Invalid,
        };

        if (localDir == Direction.Invalid)
            return Direction.Invalid;

        return localRotation.RotateDir(localDir);
    }

    private static Vector2i DirectionToOffset(Direction direction)
    {
        return direction switch
        {
            Direction.East => new Vector2i(1, 0),
            Direction.West => new Vector2i(-1, 0),
            Direction.North => new Vector2i(0, 1),
            Direction.South => new Vector2i(0, -1),
            _ => Vector2i.Zero
        };
    }

    private bool HasMatchingNeighbor(EntityUid entity, EntityUid gridUid, MapGridComponent grid, Vector2i tile, string key)
    {
        var anchored = _map.GetAnchoredEntitiesEnumerator(gridUid, grid, tile);

        while (anchored.MoveNext(out var other))
        {
            if (other == entity)
                continue;

            if (TryComp<EdgeConnectionComponent>(other, out var comp) &&
                comp.ConnectionKey == key)
            {
                return true;
            }
        }

        return false;
    }

    private void UpdateNeighbors(Entity<EdgeConnectionComponent> ent)
    {
        var xform = Transform(ent);

        if (!TryComp<MapGridComponent>(xform.GridUid, out var grid))
            return;

        var tile = _map.TileIndicesFor(xform.GridUid.Value, grid, xform.Coordinates);

        // Update all potentially affected neighbors
        UpdateNeighborsAtTile(xform.GridUid.Value, grid, tile + new Vector2i(1, 0));
        UpdateNeighborsAtTile(xform.GridUid.Value, grid, tile + new Vector2i(-1, 0));
        UpdateNeighborsAtTile(xform.GridUid.Value, grid, tile + new Vector2i(0, 1));
        UpdateNeighborsAtTile(xform.GridUid.Value, grid, tile + new Vector2i(0, -1));
    }

    private void UpdateNeighborsAtTile(EntityUid gridUid, MapGridComponent grid, Vector2i tile)
    {
        var anchored = _map.GetAnchoredEntitiesEnumerator(gridUid, grid, tile);

        while (anchored.MoveNext(out var other))
        {
            if (TryComp<EdgeConnectionComponent>(other, out var comp))
            {
                UpdateConnections((other.Value, comp));
            }
        }
    }
}

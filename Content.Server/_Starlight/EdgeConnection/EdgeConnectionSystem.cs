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

        // Check each allowed direction
        if ((allowed & EdgeConnectionFlags.East) != 0)
        {
            if (HasMatchingNeighbor(ent, xform.GridUid.Value, grid, tile + new Vector2i(1, 0), ent.Comp.ConnectionKey))
                mask |= EdgeConnectionFlags.East;
        }

        if ((allowed & EdgeConnectionFlags.West) != 0)
        {
            if (HasMatchingNeighbor(ent, xform.GridUid.Value, grid, tile + new Vector2i(-1, 0), ent.Comp.ConnectionKey))
                mask |= EdgeConnectionFlags.West;
        }

        if ((allowed & EdgeConnectionFlags.North) != 0)
        {
            if (HasMatchingNeighbor(ent, xform.GridUid.Value, grid, tile + new Vector2i(0, 1), ent.Comp.ConnectionKey))
                mask |= EdgeConnectionFlags.North;
        }

        if ((allowed & EdgeConnectionFlags.South) != 0)
        {
            if (HasMatchingNeighbor(ent, xform.GridUid.Value, grid, tile + new Vector2i(0, -1), ent.Comp.ConnectionKey))
                mask |= EdgeConnectionFlags.South;
        }

        _appearance.SetData(ent, EdgeConnectionVisuals.ConnectionMask, mask);
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

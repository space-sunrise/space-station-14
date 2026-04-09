#pragma warning disable IDE0130

using System.Numerics;
using Content.Server.Construction.Commands;
using Content.Shared.Decals;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Shared.Map.Components;

namespace Content.Server.Decals;

public sealed class WalledDecalRemovalSystem : EntitySystem
{
    [Dependency] private readonly DecalSystem _decal = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    public int RemoveWalledDecals(EntityUid gridUid, MapGridComponent? grid = null)
    {
        if (!Resolve(gridUid, ref grid))
            return 0;

        return RemoveWalledDecals((gridUid, grid));
    }

    public int RemoveWalledDecals(Entity<MapGridComponent> grid)
    {
        if (!TryComp<DecalGridComponent>(grid.Owner, out var decalGrid))
            return 0;

        var wallTiles = new HashSet<Vector2i>();
        var decalsToRemove = new HashSet<uint>();
        var childEnumerator = Transform(grid.Owner).ChildEnumerator;

        while (childEnumerator.MoveNext(out var child))
        {
            if (!Exists(child) ||
                !_tag.HasTag(child, TileWallsCommand.WallTag) ||
                _tag.HasTag(child, TileWallsCommand.ForceNoTileWallsTag) ||
                _tag.HasTag(child, TileWallsCommand.DiagonalTag))
            {
                continue;
            }

            var childTransform = Transform(child);
            if (!childTransform.Anchored)
                continue;

            wallTiles.Add(_map.GetTileRef(grid.Owner, grid.Comp, childTransform.Coordinates).GridIndices);
        }

        if (wallTiles.Count == 0)
            return 0;

        foreach (var chunk in decalGrid.ChunkCollection.ChunkCollection.Values)
        {
            foreach (var (decalId, decal) in chunk.Decals)
            {
                if (wallTiles.Contains(GetDecalTileIndices(decal, grid.Comp)))
                    decalsToRemove.Add(decalId);
            }
        }

        var removed = 0;
        foreach (var decalId in decalsToRemove)
        {
            if (_decal.RemoveDecal(grid.Owner, decalId))
                removed++;
        }

        return removed;
    }

    private static Vector2i GetDecalTileIndices(Decal decal, MapGridComponent grid)
    {
        var decalCenter = decal.Coordinates + grid.TileSizeHalfVector;
        return new Vector2i(
            (int)Math.Floor(decalCenter.X / grid.TileSize),
            (int)Math.Floor(decalCenter.Y / grid.TileSize));
    }
}

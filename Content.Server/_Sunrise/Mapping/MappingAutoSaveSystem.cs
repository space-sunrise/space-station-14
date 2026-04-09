#pragma warning disable IDE0130

using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Construction.Commands;
using Content.Server.Decals;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared.Maps;
using Robust.Shared.Console;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Map.Events;
using Robust.Shared.Timing;

namespace Content.Server.Mapping;

public sealed class MappingAutoSaveSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IConsoleHost _console = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly TileSystem _tile = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly WalledDecalRemovalSystem _walledDecalRemoval = default!;

    private PendingAutoSaveConsoleContext? _pendingAutoSaveConsoleContext;

    public override void Initialize()
    {
        base.Initialize();

        _console.AnyCommandExecuted += OnAnyCommandExecuted;
        SubscribeLocalEvent<BeforeSerializationEvent>(OnBeforeSerialization);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _console.AnyCommandExecuted -= OnAnyCommandExecuted;
    }

    public string? GetMapSaveAutoCommandsSummary(EntityUid mapUid)
    {
        if (!HasComp<MapComponent>(mapUid))
            return null;

        return GetEnabledAutoCommandsSummary();
    }

    private void OnAnyCommandExecuted(IConsoleShell shell, string name, string argStr, string[] args)
    {
        if (!string.Equals(name, "savemap", StringComparison.OrdinalIgnoreCase))
            return;

        _pendingAutoSaveConsoleContext = null;

        var autoCommands = GetEnabledAutoCommandsSummary();
        if (autoCommands is null ||
            args.Length < 2 ||
            !int.TryParse(args[0], out var mapInt))
        {
            return;
        }

        var mapId = new MapId(mapInt);
        if (mapId == MapId.Nullspace ||
            !_map.MapExists(mapId) ||
            _map.IsInitialized(mapId) && (args.Length < 3 || !bool.TryParse(args[2], out var force) || !force))
        {
            return;
        }

        _pendingAutoSaveConsoleContext = new PendingAutoSaveConsoleContext(shell, mapId, _timing.CurTick, autoCommands);
    }

    private void OnBeforeSerialization(BeforeSerializationEvent ev)
    {
        foreach (var entity in ev.Entities)
        {
            if (!TryComp(entity, out MapComponent? map))
                continue;

            RunMapSaveAutoCommands(map);
            TryWritePendingAutoSaveConsoleMessage(map.MapId);
        }
    }

    private void RunMapSaveAutoCommands(MapComponent map)
    {
        var runFixGridAtmos = _cfg.GetCVar(SunriseCCVars.MappingAutoFixGridAtmos);
        var runTileWalls = _cfg.GetCVar(SunriseCCVars.MappingAutoTileWalls);
        var runRemoveWalledDecals = _cfg.GetCVar(SunriseCCVars.MappingAutoRemoveWalledDecals);
        var runVariantize = _cfg.GetCVar(SunriseCCVars.MappingAutoVariantize);

        if (!runFixGridAtmos && !runTileWalls && !runRemoveWalledDecals && !runVariantize)
            return;

        foreach (var grid in _mapManager.GetAllGrids(map.MapId))
        {
            if (runFixGridAtmos)
                RunMapSaveAutoFixGridAtmos(grid);

            if (runTileWalls)
                RunMapSaveAutoTileWalls(grid);

            if (runRemoveWalledDecals)
                _walledDecalRemoval.RemoveWalledDecals(grid.Owner, grid.Comp);

            if (runVariantize)
                RunMapSaveAutoVariantize(grid);
        }
    }

    private string? GetEnabledAutoCommandsSummary()
    {
        List<string>? executedCommands = null;

        AddEnabledCommand(SunriseCCVars.MappingAutoFixGridAtmos, "fixgridatmos");
        AddEnabledCommand(SunriseCCVars.MappingAutoTileWalls, "tilewalls");
        AddEnabledCommand(SunriseCCVars.MappingAutoRemoveWalledDecals, "removewalleddecals");
        AddEnabledCommand(SunriseCCVars.MappingAutoVariantize, "variantize");

        return executedCommands is null
            ? null
            : string.Join(", ", executedCommands);

        void AddEnabledCommand(CVarDef<bool> cvar, string command)
        {
            if (!_cfg.GetCVar(cvar))
                return;

            executedCommands ??= new List<string>(3);
            executedCommands.Add(command);
        }
    }

    private void TryWritePendingAutoSaveConsoleMessage(MapId mapId)
    {
        if (_pendingAutoSaveConsoleContext is not { } pending)
            return;

        if (pending.Tick != _timing.CurTick)
        {
            _pendingAutoSaveConsoleContext = null;
            return;
        }

        if (pending.MapId != mapId)
            return;

        pending.Shell.WriteLine(Loc.GetString("mapping-save-auto-commands", ("commands", pending.AutoCommands)));
        _pendingAutoSaveConsoleContext = null;
    }

    private void RunMapSaveAutoFixGridAtmos(Entity<MapGridComponent> grid)
    {
        if (!TryComp(grid, out GridAtmosphereComponent? gridAtmosphere))
            return;

        _atmosphere.RebuildGridAtmosphere((grid.Owner, gridAtmosphere, grid.Comp));
    }

    private void RunMapSaveAutoTileWalls(Entity<MapGridComponent> grid)
    {
        var underplating = _tileDefinitionManager[TileWallsCommand.TilePrototypeId];
        var underplatingTile = new Tile(underplating.TileId);
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

            var tile = _map.GetTileRef(grid.Owner, grid.Comp, childTransform.Coordinates);
            var tileDefinition = (ContentTileDefinition) _tileDefinitionManager[tile.Tile.TypeId];

            if (tileDefinition.ID == TileWallsCommand.TilePrototypeId)
                continue;

            _map.SetTile(grid.Owner, grid.Comp, childTransform.Coordinates, underplatingTile);
        }
    }

    private void RunMapSaveAutoVariantize(Entity<MapGridComponent> grid)
    {
        foreach (var tile in _map.GetAllTiles(grid.Owner, grid.Comp))
        {
            var tileDefinition = _turf.GetContentTileDefinition(tile);
            var variantTile = new Tile(
                tile.Tile.TypeId,
                tile.Tile.Flags,
                _tile.PickVariant(tileDefinition),
                tile.Tile.RotationMirroring);

            _map.SetTile(grid.Owner, grid.Comp, tile.GridIndices, variantTile);
        }
    }

    private sealed record PendingAutoSaveConsoleContext(
        IConsoleShell Shell,
        MapId MapId,
        GameTick Tick,
        string AutoCommands);
}

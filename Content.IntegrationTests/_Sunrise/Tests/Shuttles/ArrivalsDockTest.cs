using System.Numerics;
using Content.IntegrationTests.Pair;
using Content.Server._Sunrise.Shuttles.Components;
using Content.Server.Shuttles;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.IntegrationTests._Sunrise.Tests.Shuttles;

public sealed class ArrivalsDockTest
{
    private static readonly Vector2i[] StationTiles =
    {
        new(0, 0), new(0, 1), new(0, 2), new(-1, 2), new(1, 2),
    };

    private static readonly Vector2i[] ShuttleTiles =
    {
        new(0, 0), new(0, 1), new(0, 2),
    };

    [Test]
    public async Task ArrivalsShuttleSkipsTakenDock()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.EntMan;
        var dockSys = server.System<DockingSystem>();
        var shuttleSys = server.System<ShuttleSystem>();
        var map = await pair.CreateTestMap();

        var firstDock = EntityUid.Invalid;
        var secondDock = EntityUid.Invalid;
        DockingConfig? firstConfig = null;
        DockingConfig? secondConfig = null;

        await server.WaitPost(() =>
        {
            entMan.DeleteEntity(map.Grid);

            var (station, _) = MakeGrid(pair, map.MapId, new Vector2(100f, 100f), StationTiles);
            var (first, firstDockUid) = MakeGrid(pair, map.MapId, Vector2.Zero, ShuttleTiles);
            var (second, secondDockUid) = MakeGrid(pair, map.MapId, new Vector2(50f, 0f), ShuttleTiles);

            firstDock = firstDockUid;
            secondDock = secondDockUid;

            var arrivals = entMan.AddComponent<SunriseArrivalsShuttleComponent>(second);
            arrivals.SpawnTime = server.Timing.CurTime;

            firstConfig = dockSys.GetDockingConfig(first, station);
            secondConfig = dockSys.GetDockingConfig(second, station);

            if (firstConfig == null || secondConfig == null)
                return;

            shuttleSys.FTLDock((first, entMan.GetComponent<TransformComponent>(first)), firstConfig);

            var shuttle = entMan.GetComponent<ShuttleComponent>(second);
            shuttleSys.FTLToCoordinates(second, shuttle, secondConfig.Coordinates, secondConfig.Angle,
                startupTime: 0f, hyperspaceTime: 0f);
        });

        Assert.Multiple(() =>
        {
            Assert.That(firstConfig, Is.Not.Null);
            Assert.That(secondConfig, Is.Not.Null);
        });

        await pair.RunSeconds(4);

        await server.WaitAssertion(() =>
        {
            Assert.That(entMan.GetComponent<DockingComponent>(firstDock).Docked, Is.True);
            Assert.That(entMan.GetComponent<DockingComponent>(secondDock).Docked, Is.False);
        });

        await pair.CleanReturnAsync();
    }

    private static (EntityUid Grid, EntityUid Dock) MakeGrid(TestPair pair, MapId mapId, Vector2 offset, Vector2i[] tiles)
    {
        var server = pair.Server;
        var mapSys = server.System<SharedMapSystem>();
        var xformSys = server.System<SharedTransformSystem>();

        var grid = server.MapMan.CreateGridEntity(mapId);
        foreach (var tile in tiles)
        {
            mapSys.SetTile(grid.Owner, grid.Comp, tile, new Tile(1));
        }

        xformSys.SetLocalPosition(grid.Owner, offset);
        var dock = server.EntMan.SpawnEntity("AirlockShuttle", new EntityCoordinates(grid.Owner, new Vector2(0.5f, 0.5f)));

        return (grid.Owner, dock);
    }
}

using System.Numerics;
using Content.IntegrationTests.Pair;
using Content.Server.Polymorph.Systems;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Shared.Polymorph;
using Content.Shared.Shuttles.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.IntegrationTests._Sunrise.Tests.Shuttles;

public sealed class FtlSmashTest
{
    private const string MobProto = "MobHuman";

    private static readonly Vector2 Landing = new(50f, 50f);

    [TestCase(true)]
    [TestCase(false)]
    public async Task MobGibbedUnlessImmune(bool immune)
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.EntMan;
        var map = await pair.CreateTestMap();

        var mob = EntityUid.Invalid;

        await server.WaitPost(() =>
        {
            mob = entMan.SpawnEntity(MobProto, new MapCoordinates(Landing, map.MapId));
            if (immune)
                entMan.EnsureComponent<FTLSmashImmuneComponent>(mob);
        });

        await FtlOnto(pair, map.MapId, new EntityCoordinates(map.MapUid, Landing));

        await server.WaitAssertion(() =>
        {
            Assert.That(entMan.Deleted(mob), Is.EqualTo(!immune));
        });

        await pair.CleanReturnAsync();
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task MobOnOtherShuttleSurvives(bool immune)
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.EntMan;
        var mapSys = server.System<SharedMapSystem>();
        var xformSys = server.System<SharedTransformSystem>();
        var map = await pair.CreateTestMap();

        var mob = EntityUid.Invalid;

        await server.WaitPost(() =>
        {
            var other = server.MapMan.CreateGridEntity(map.MapId);
            mapSys.SetTile(other.Owner, other.Comp, Vector2i.Zero, new Tile(1));
            xformSys.SetLocalPosition(other.Owner, Landing);

            mob = entMan.SpawnEntity(MobProto, new EntityCoordinates(other.Owner, 0.5f, 0.5f));
            if (immune)
                entMan.EnsureComponent<FTLSmashImmuneComponent>(mob);
        });

        await FtlOnto(pair, map.MapId, new EntityCoordinates(map.MapUid, Landing));

        await server.WaitAssertion(() =>
        {
            Assert.That(entMan.Deleted(mob), Is.False);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task PolymorphKeepsImmunity()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.EntMan;
        var polymorph = server.System<PolymorphSystem>();
        var map = await pair.CreateTestMap();

        var morphed = EntityUid.Invalid;

        await server.WaitPost(() =>
        {
            var mob = entMan.SpawnEntity(MobProto, new MapCoordinates(Landing, map.MapId));
            entMan.EnsureComponent<FTLSmashImmuneComponent>(mob);

            var config = new PolymorphConfiguration { Entity = MobProto };
            morphed = polymorph.PolymorphEntity(mob, config) ?? EntityUid.Invalid;
        });

        Assert.That(morphed, Is.Not.EqualTo(EntityUid.Invalid));

        await FtlOnto(pair, map.MapId, new EntityCoordinates(map.MapUid, Landing));

        await server.WaitAssertion(() =>
        {
            Assert.That(entMan.Deleted(morphed), Is.False);
        });

        await pair.CleanReturnAsync();
    }

    private static async Task FtlOnto(TestPair pair, MapId mapId, EntityCoordinates target)
    {
        var server = pair.Server;
        var entMan = server.EntMan;
        var mapSys = server.System<SharedMapSystem>();
        var xformSys = server.System<SharedTransformSystem>();
        var shuttleSys = server.System<ShuttleSystem>();

        await server.WaitPost(() =>
        {
            var grid = server.MapMan.CreateGridEntity(mapId);
            for (var x = -2; x <= 2; x++)
            {
                for (var y = -2; y <= 2; y++)
                {
                    mapSys.SetTile(grid.Owner, grid.Comp, new Vector2i(x, y), new Tile(1));
                }
            }

            xformSys.SetLocalPosition(grid.Owner, new Vector2(-200f, -200f));

            var shuttle = entMan.GetComponent<ShuttleComponent>(grid.Owner);
            shuttleSys.FTLToCoordinates(grid.Owner, shuttle, target, Angle.Zero, startupTime: 0f, hyperspaceTime: 0f);
        });

        await pair.RunSeconds(4);
    }
}

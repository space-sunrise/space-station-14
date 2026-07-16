using Content.Server._Sunrise.GameTicking.PlayerJoinableMaps;
using Content.Shared._Sunrise.GameTicking.PlayerJoinableMaps;
using Content.Shared._Sunrise.Shuttles;
using Content.Shared.Light.Components;
using Content.Shared.Parallax.Biomes;
using Content.Shared.Salvage;
using Content.Shared.Shuttles.Components;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Sunrise.GameTicking;

[TestFixture]
public sealed class PlayerJoinableMapLoadingTest
{
    private static readonly ProtoId<PlayerJoinableMapPrototype> PlanetPrisonMap = "SunrisePlanetPrison";

    [Test]
    public async Task ManagedPlanetMapLoadsExactlyOnceAndAppliesConfiguration()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Dirty = true,
            InLobby = true,
        });
        var server = pair.Server;
        var loader = server.System<PlayerJoinableMapSystem>();

        PlayerJoinableMapInstance first = default;
        await server.WaitAssertion(() =>
        {
            Assert.That(loader.TryGetLoadedMap(PlanetPrisonMap, out first), Is.True);
            Assert.That(first.Grids, Has.Count.EqualTo(1));

            Assert.Multiple(() =>
            {
                Assert.That(server.EntMan.HasComponent<RestrictedRangeComponent>(first.MapEntity), Is.True);
                Assert.That(server.EntMan.HasComponent<LightCycleComponent>(first.MapEntity), Is.True);
                Assert.That(server.EntMan.HasComponent<BiomeComponent>(first.MapEntity), Is.True);
                Assert.That(server.EntMan.HasComponent<IgnoreFtlCheckComponent>(first.Grids[0]), Is.True);
            });

            var destination = server.EntMan.GetComponent<FTLDestinationComponent>(first.MapEntity);
            Assert.Multiple(() =>
            {
                Assert.That(destination.Enabled, Is.True);
                Assert.That(destination.RequireCoordinateDisk, Is.False);
                Assert.That(destination.BeaconsOnly, Is.True);
                Assert.That(destination.Whitelist?.Components,
                    Is.EquivalentTo(new[] { "SecurityShuttle", "PrisonShuttle", "ErtShuttle" }));
            });
        });

        await server.WaitPost(() => Assert.That(loader.TryLoadManagedMap(PlanetPrisonMap), Is.True));
        await server.WaitAssertion(() =>
        {
            Assert.That(loader.TryGetLoadedMap(PlanetPrisonMap, out var second), Is.True);
            Assert.That(second.MapId, Is.EqualTo(first.MapId));
            Assert.That(second.MapEntity, Is.EqualTo(first.MapEntity));
        });

        await pair.CleanReturnAsync();
    }
}

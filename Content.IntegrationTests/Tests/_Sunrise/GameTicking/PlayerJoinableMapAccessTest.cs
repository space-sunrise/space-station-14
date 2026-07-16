using Content.Server._Sunrise.StationCentComm;
using Content.Shared._Sunrise.GameTicking.PlayerJoinableMaps;
using Content.Shared.Maps;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using SunriseCVars = Content.Shared._Sunrise.SunriseCCVars.SunriseCCVars;

namespace Content.IntegrationTests.Tests._Sunrise.GameTicking;

[TestFixture]
public sealed class PlayerJoinableMapAccessTest
{
    private static readonly ProtoId<PlayerJoinableMapPrototype> CentCommMap = "SunriseCentComm";
    private static readonly ProtoId<PlayerJoinableMapPrototype> PlanetPrisonMap = "SunrisePlanetPrison";
    private static ProtoId<GameMapPrototype> PlanetPrisonGameMap => "PlanetPrison";
    private static ProtoId<GameMapPrototype> PlanetPrisonOldGameMap => "PlanetPrisonOld";
    private static readonly EntProtoId StandardNanotrasenStation = "StandardNanotrasenStation";

    [Test]
    public async Task ManagedMapAndCentCommOwnershipAreExplicit()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var prototypes = server.ResolveDependency<IPrototypeManager>();
        await server.WaitAssertion(() =>
        {
            var centComm = prototypes.Index(CentCommMap);
            var planetPrison = prototypes.Index(PlanetPrisonMap);
            var load = planetPrison.Load;

            Assert.Multiple(() =>
            {
                Assert.That(centComm.Load, Is.Null, "Central Command must remain owned by StationCentcommSystem.");
                Assert.That(load, Is.Not.Null);
                Assert.That(load!.GameMap, Is.EqualTo(PlanetPrisonGameMap));
                Assert.That(load.Environment, Is.EqualTo(PlayerJoinableMapEnvironmentType.Planet));
                Assert.That(load.Biomes, Is.Not.Empty);
                Assert.That(load.MapComponents.ContainsKey("RestrictedRange"), Is.True);
                Assert.That(load.MapComponents.ContainsKey("LightCycle"), Is.True);
                Assert.That(load.GridComponents.ContainsKey("IgnoreFtlCheck"), Is.True);
                Assert.That(load.Ftl, Is.Not.Null);
                Assert.That(load.Ftl!.RequireCoordinateDisk, Is.False);
                Assert.That(load.Ftl.BeaconsOnly, Is.True);
                Assert.That(load.Ftl.ShuttleWhitelist?.Components,
                    Is.EquivalentTo(new[] { "SecurityShuttle", "PrisonShuttle", "ErtShuttle" }));
                Assert.That(prototypes.HasIndex(PlanetPrisonOldGameMap), Is.True);
            });

            var centCommOwner = prototypes.Index(StandardNanotrasenStation);
            Assert.That(
                centCommOwner.TryGetComponent<StationCentCommComponent>(out var owner, server.EntMan.ComponentFactory),
                Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(owner!.Station, Is.EqualTo("SunriseCentComm"));
                Assert.That(owner.ShuttleWhitelist?.Components,
                    Is.EquivalentTo(new[] { "CentCommShuttle", "ErtShuttle" }));
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task MapAccessCVarsAreIndependentAndReplicated()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true });
        var server = pair.Server;
        var client = pair.Client;

        var serverCfg = server.ResolveDependency<IConfigurationManager>();
        var clientCfg = client.ResolveDependency<IConfigurationManager>();
        var serverPrototypes = server.ResolveDependency<IPrototypeManager>();
        var clientPrototypes = client.ResolveDependency<IPrototypeManager>();

        var serverCentComm = serverPrototypes.Index(CentCommMap);
        var serverPlanetPrison = serverPrototypes.Index(PlanetPrisonMap);
        var clientCentComm = clientPrototypes.Index(CentCommMap);
        var clientPlanetPrison = clientPrototypes.Index(PlanetPrisonMap);

        var originalCentCommEnabled = serverCfg.GetCVar(SunriseCVars.PlayerJoinableMapCentCommEnabled);
        var originalCentCommMinPlayers = serverCfg.GetCVar(SunriseCVars.PlayerJoinableMapCentCommMinPlayers);
        var originalPlanetPrisonEnabled = serverCfg.GetCVar(SunriseCVars.PlayerJoinableMapPlanetPrisonEnabled);
        var originalPlanetPrisonMinPlayers = serverCfg.GetCVar(SunriseCVars.PlayerJoinableMapPlanetPrisonMinPlayers);

        try
        {
            await server.WaitPost(() =>
            {
                serverCfg.SetCVar(SunriseCVars.PlayerJoinableMapCentCommEnabled, false);
                serverCfg.SetCVar(SunriseCVars.PlayerJoinableMapCentCommMinPlayers, 0);
                serverCfg.SetCVar(SunriseCVars.PlayerJoinableMapPlanetPrisonMinPlayers, 30);
                serverCfg.SetCVar(SunriseCVars.PlayerJoinableMapPlanetPrisonEnabled, true);
            });

            await pair.RunTicksSync(5);

            await server.WaitAssertion(() =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(PlayerJoinableMapAccess.IsEnabled(serverCentComm, serverCfg, 30), Is.False);
                    Assert.That(PlayerJoinableMapAccess.IsEnabled(serverPlanetPrison, serverCfg, 29), Is.False);
                    Assert.That(PlayerJoinableMapAccess.IsEnabled(serverPlanetPrison, serverCfg, 30), Is.True);
                });
            });

            await client.WaitAssertion(() =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(PlayerJoinableMapAccess.IsEnabled(clientCentComm, clientCfg, 30), Is.False);
                    Assert.That(PlayerJoinableMapAccess.IsEnabled(clientPlanetPrison, clientCfg, 29), Is.False);
                    Assert.That(PlayerJoinableMapAccess.IsEnabled(clientPlanetPrison, clientCfg, 30), Is.True);
                });
            });
        }
        finally
        {
            await server.WaitPost(() =>
            {
                serverCfg.SetCVar(SunriseCVars.PlayerJoinableMapCentCommEnabled, originalCentCommEnabled);
                serverCfg.SetCVar(SunriseCVars.PlayerJoinableMapCentCommMinPlayers, originalCentCommMinPlayers);
                serverCfg.SetCVar(SunriseCVars.PlayerJoinableMapPlanetPrisonEnabled, originalPlanetPrisonEnabled);
                serverCfg.SetCVar(SunriseCVars.PlayerJoinableMapPlanetPrisonMinPlayers, originalPlanetPrisonMinPlayers);
            });

            await pair.RunTicksSync(5);
        }

        await pair.CleanReturnAsync();
    }
}

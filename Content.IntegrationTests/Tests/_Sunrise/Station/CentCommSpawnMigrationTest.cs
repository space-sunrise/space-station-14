using System.IO;
using Content.Shared._Sunrise.GameTicking.PlayerJoinableMaps;
using Content.Shared._Sunrise.SunriseCCVars;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Sunrise.Station;

[TestFixture]
public sealed class CentCommSpawnMigrationTest
{
    private static readonly ProtoId<PlayerJoinableMapPrototype> SunriseCentCommMap = "SunriseCentComm";
    private static readonly ProtoId<PlayerJoinableMapPrototype> SunrisePlanetPrisonMap = "SunrisePlanetPrison";

    [Test]
    public void PlayerJoinableMapFiles_Exist()
    {
        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(RepoPath("Content.Server/_Sunrise/GameTicking/PlayerJoinableMaps/PlayerJoinableMapSystem.cs")), Is.True);
            Assert.That(File.Exists(RepoPath("Content.Shared/_Sunrise/GameTicking/PlayerJoinableMaps/PlayerJoinableMapComponent.cs")), Is.True);
            Assert.That(File.Exists(RepoPath("Content.Shared/_Sunrise/GameTicking/PlayerJoinableMaps/PlayerJoinableMapPrototype.cs")), Is.True);
            Assert.That(File.Exists(RepoPath("Resources/Prototypes/_Sunrise/GameTicking/player_joinable_maps.yml")), Is.True);
        });
    }

    [Test]
    public void CentCommMap_UsesPlayerJoinableMapStationPrototype()
    {
        var centCommMap = ReadRepoFile("Resources/Prototypes/_Sunrise/Maps/centcomm.yml");
        var stations = ReadRepoFile("Resources/Prototypes/_Sunrise/Entities/Stations/base.yml");
        var playerJoinableMaps = ReadRepoFile("Resources/Prototypes/_Sunrise/GameTicking/player_joinable_maps.yml");

        Assert.Multiple(() =>
        {
            Assert.That(centCommMap, Does.Contain("stationProto: SunriseNanotrasenCentralCommand"));
            Assert.That(stations, Does.Contain("id: SunriseNanotrasenCentralCommand"));
            Assert.That(stations, Does.Contain("- type: PlayerJoinableMap"));
            Assert.That(stations, Does.Contain("map: SunriseCentComm"));
            Assert.That(stations, Does.Not.Contain("playerAccessEnabledCVar: centcomm.enabled"));
            Assert.That(stations, Does.Not.Contain("spawnWhenPlayerAccessDisabled: true"));
            Assert.That(playerJoinableMaps, Does.Contain("id: SunriseCentComm"));
            Assert.That(playerJoinableMaps, Does.Contain("access: CentComm"));
            Assert.That(playerJoinableMaps, Does.Not.Contain("playerAccessEnabledCVar"));
            Assert.That(playerJoinableMaps, Does.Not.Contain("playerAccessMinPlayersCVar"));
            Assert.That(playerJoinableMaps, Does.Contain("spawnWhenPlayerAccessDisabled: true"));
            Assert.That(playerJoinableMaps, Does.Contain("- CentCommOperator"));
            Assert.That(playerJoinableMaps, Does.Not.Contain("CentCommOfficial"));
        });
    }

    [Test]
    public async Task PlayerJoinableMapPrototypes_LoadAccessAndTypedCVars()
    {
        await using var pair = await PoolManager.GetServerClient();

        try
        {
            var centComm = pair.Server.ProtoMan.Index(SunriseCentCommMap);
            var planetPrison = pair.Server.ProtoMan.Index(SunrisePlanetPrisonMap);

            Assert.Multiple(() =>
            {
                Assert.That(centComm.Access, Is.EqualTo(PlayerJoinableMapAccessType.CentComm));
                Assert.That(PlayerJoinableMapAccess.GetEnabledCVar(centComm), Is.SameAs(SunriseCCVars.CentCommEnabled));
                Assert.That(PlayerJoinableMapAccess.GetMinPlayersCVar(centComm), Is.Null);

                Assert.That(planetPrison.Access, Is.EqualTo(PlayerJoinableMapAccessType.PlanetPrison));
                Assert.That(PlayerJoinableMapAccess.GetEnabledCVar(planetPrison), Is.SameAs(SunriseCCVars.PlanetPrisonEnabled));
                Assert.That(PlayerJoinableMapAccess.GetMinPlayersCVar(planetPrison), Is.SameAs(SunriseCCVars.MinPlayersPlanetPrison));
            });
        }
        finally
        {
            await pair.CleanReturnAsync();
        }
    }

    [Test]
    public void StationCentCommSystem_OnlyOwnsCentCommMapLifecycle()
    {
        var stationCentComm = ReadRepoFile("Content.Server/_Sunrise/StationCentcomm/StationCentcommSystem.cs");

        Assert.Multiple(() =>
        {
            Assert.That(stationCentComm, Does.Contain("SubscribeLocalEvent<StationCentCommComponent, ComponentInit>(OnCentcommInit)"));
            Assert.That(stationCentComm, Does.Contain("SubscribeLocalEvent<StationCentCommComponent, ComponentShutdown>(OnCentcommShutdown)"));
            Assert.That(stationCentComm, Does.Contain("_gameTicker.LoadGameMap(gameMap, out var mapId);"));
            Assert.That(stationCentComm, Does.Contain("EnsureComp<AlwaysPoweredMapComponent>(mapEnt);"));
            Assert.That(stationCentComm, Does.Not.Contain("SunriseCCVars.CentCommEnabled"));
            Assert.That(stationCentComm, Does.Not.Contain("ResolveCentCommJoinStation"));
            Assert.That(stationCentComm, Does.Not.Contain("StationJobsGetCandidatesEvent"));
        });
    }

    [Test]
    public void PlayerJoinableMapRouting_ReplacesCentCommSpecificJoinFlow()
    {
        var gameTicker = ReadRepoFile("Content.Server/_Sunrise/GameTicking/GameTicker.PlayerJoinableMapJoin.cs");
        var joinCommand = ReadRepoFile("Content.Server/_Sunrise/GameTicking/Commands/JoinGameCommand.JoinGate.cs");
        var stationJobs = ReadRepoFile("Content.Server/_Sunrise/Station/Systems/StationJobsSystem.PlayerJoinableMaps.cs");
        var profileEditor = ReadRepoFile("Content.Client/Lobby/UI/HumanoidProfileEditor.xaml.cs");

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(RepoPath("Content.Server/_Sunrise/GameTicking/GameTicker.CentCommJoin.cs")), Is.False);
            Assert.That(File.Exists(RepoPath("Content.Shared/_Sunrise/Roles/CentCommJobHelper.cs")), Is.False);

            Assert.That(gameTicker, Does.Contain("TryPreparePlayerJoinableMapJoin"));
            Assert.That(gameTicker, Does.Contain("TryResolveJoinableStationForJob"));
            Assert.That(gameTicker, Does.Not.Contain("CentCommJobHelper"));
            Assert.That(gameTicker, Does.Not.Contain("SunriseCCVars.CentCommEnabled"));
            Assert.That(gameTicker, Does.Not.Contain("game-ticker-player-centcomm-disabled"));

            Assert.That(joinCommand, Does.Contain("TryPreparePlayerJoinableMapJoin"));
            Assert.That(stationJobs, Does.Contain("PlayerJoinableMapSystem"));
            Assert.That(stationJobs, Does.Contain("PlayerJoinableMapPrototype"));
            Assert.That(stationJobs, Does.Contain("FilterRoundStartJobSelectionPortal"));
            Assert.That(stationJobs, Does.Not.Contain("CentCommJobHelper"));

            Assert.That(profileEditor, Does.Contain("PlayerJoinableMapPrototype"));
            Assert.That(profileEditor, Does.Contain("player-joinable-map-additional-title"));
            Assert.That(profileEditor, Does.Not.Contain("CentCommJobHelper"));
        });
    }

    [Test]
    public void VanillaSpawnFiles_ExposeOnlyGenericPortals()
    {
        var gameTicker = ReadRepoFile("Content.Server/GameTicking/GameTicker.Spawning.cs");
        var stationJobs = ReadRepoFile("Content.Server/Station/Systems/StationJobsSystem.cs");
        var roundStart = ReadRepoFile("Content.Server/Station/Systems/StationJobsSystem.Roundstart.cs");

        Assert.Multiple(() =>
        {
            Assert.That(gameTicker, Does.Contain("FilterFallbackSpawnableStationsPortal("));
            Assert.That(gameTicker, Does.Contain("ResolveDirectSpawnStationPortal("));
            Assert.That(stationJobs, Does.Contain("FilterJobsAvailablePortal(EntityUid station"));
            Assert.That(stationJobs, Does.Contain("FilterRoundStartJobSelectionPortal(EntityUid station"));
            Assert.That(roundStart, Does.Contain("FilterRoundStartJobSelectionPortal(station"));

            Assert.That(gameTicker, Does.Not.Contain("CentComm"));
            Assert.That(stationJobs, Does.Not.Contain("CentComm"));
            Assert.That(roundStart, Does.Not.Contain("CentComm"));
            Assert.That(stationJobs, Does.Not.Contain("Content.Server._Sunrise"));
            Assert.That(roundStart, Does.Not.Contain("Content.Server._Sunrise"));
        });
    }

    [Test]
    public void CentCommCVar_IsReplicatedForPlayerFacingUi()
    {
        var cvars = ReadRepoFile("Content.Shared/_Sunrise/SunriseCCVars/SunriseCCVars.PlayerJoinableMaps.cs");

        Assert.Multiple(() =>
        {
            Assert.That(cvars, Does.Contain("CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE"));
            Assert.That(cvars, Does.Not.Contain("CVar.SERVERONLY | CVar.ARCHIVE"));
        });
    }

    private static string ReadRepoFile(string path)
    {
        return File.ReadAllText(RepoPath(path));
    }

    private static string RepoPath(string path)
    {
        return Path.Combine(GetRepoRoot(), path.Replace('/', Path.DirectorySeparatorChar));
    }

    private static string GetRepoRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "Content.Server")) &&
                Directory.Exists(Path.Combine(directory.FullName, "Content.Shared")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        Assert.Fail("Could not find repository root for CentComm spawn migration tests.");
        return string.Empty;
    }
}

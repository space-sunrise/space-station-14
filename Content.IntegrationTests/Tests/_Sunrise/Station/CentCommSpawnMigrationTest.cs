using System.IO;

namespace Content.IntegrationTests.Tests._Sunrise.Station;

[TestFixture]
public sealed class CentCommSpawnMigrationTest
{
    private static readonly string[] VanillaFiles =
    [
        "Content.Server/GameTicking/Commands/JoinGameCommand.cs",
        "Content.Server/GameTicking/GameTicker.Spawning.cs",
        "Content.Server/Station/Systems/StationJobsSystem.cs",
        "Content.Server/Station/Systems/StationSpawningSystem.cs",
        "Content.Shared/Roles/JobPrototype.cs",
    ];

    private static readonly string[] ForbiddenVanillaTokens =
    [
        "Content.Server._Sunrise",
        "Content.Shared._Sunrise",
        "Content.Sunrise",
        "SunriseCCVars",
        "NewLifeSystem",
        "StationAntagsTargetsComponent",
        "AntagTargetComponent",
        "OwOAccentComponent",
        "ISharedSponsorsManager",
        "JoinNotifyCrew",
        "AlwaysUseSpawner",
        "RadioIsBold",
        "SpeciesBlacklist",
        "AlternativeTitles",
    ];

    private static readonly string[] RequiredGameTickerPortals =
    [
        "FilterFallbackSpawnableStationsPortal(",
        "ResolveDirectSpawnStationPortal(",
        "SelectSpawnPointTypePortal(",
        "BeforePlayerSpawnProfilePortal(",
        "AfterPlayerMobSpawnedPortal(",
        "DispatchLateJoinAnnouncementPortal(",
    ];

    private static readonly string[] RequiredStationSpawningPortals =
    [
        "InitializeStationSpawningPortal(",
        "GetEffectiveRoleLoadoutPortal(",
        "GetDefaultLoadoutPrototypeIdsPortal(",
        "TryApplyFlavorTextPortal(",
    ];

    private static readonly string[] SunrisePortalFiles =
    [
        "Content.Server/_Sunrise/GameTicking/GameTicker.CentCommJoin.cs",
        "Content.Server/_Sunrise/GameTicking/GameTicker.SpawnStationSelection.cs",
        "Content.Server/_Sunrise/GameTicking/GameTicker.SpawnPointType.cs",
        "Content.Server/_Sunrise/GameTicking/GameTicker.NewLife.cs",
        "Content.Server/_Sunrise/GameTicking/GameTicker.SpawnedMob.cs",
        "Content.Server/_Sunrise/GameTicking/GameTicker.JoinAnnouncements.cs",
        "Content.Server/_Sunrise/GameTicking/Commands/JoinGameCommand.JoinGate.cs",
        "Content.Server/_Sunrise/Station/Systems/StationSpawningSystem.Sponsors.cs",
        "Content.Shared/_Sunrise/Roles/JobPrototype.Sunrise.cs",
    ];

    [Test]
    public void VanillaSpawnFiles_DoNotReferenceSunriseImplementation()
    {
        Assert.Multiple(() =>
        {
            foreach (var file in VanillaFiles)
            {
                var text = ReadRepoFile(file);
                foreach (var token in ForbiddenVanillaTokens)
                {
                    Assert.That(text, Does.Not.Contain(token),
                        $"{file} must not reference Sunrise implementation token `{token}`.");
                }
            }
        });
    }

    [Test]
    public void VanillaSpawnFiles_ExposeGenericPortals()
    {
        var gameTicker = ReadRepoFile("Content.Server/GameTicking/GameTicker.Spawning.cs");
        var stationSpawning = ReadRepoFile("Content.Server/Station/Systems/StationSpawningSystem.cs");

        Assert.Multiple(() =>
        {
            foreach (var portal in RequiredGameTickerPortals)
            {
                Assert.That(gameTicker, Does.Contain(portal),
                    $"GameTicker.Spawning.cs must keep generic portal `{portal}`.");
            }

            foreach (var portal in RequiredStationSpawningPortals)
            {
                Assert.That(stationSpawning, Does.Contain(portal),
                    $"StationSpawningSystem.cs must keep generic portal `{portal}`.");
            }
        });
    }

    [Test]
    public void SunrisePortalImplementations_AreInSunriseFolders()
    {
        Assert.Multiple(() =>
        {
            foreach (var file in SunrisePortalFiles)
            {
                var fullPath = RepoPath(file);
                Assert.That(File.Exists(fullPath), Is.True, $"{file} must exist.");
                Assert.That(File.ReadAllText(fullPath), Does.Contain("partial"),
                    $"{file} must be a partial portal implementation.");
            }
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

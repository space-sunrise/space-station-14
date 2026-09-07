#nullable enable

using Content.Server.AlertLevel;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests._Sunrise.AlertLevel;

[TestFixture]
[TestOf(typeof(AlertLevelSystem))]
public sealed class AdditionalAlertLevelTest
{
    [Test]
    public async Task AdditionalLevelsDoNotReplacePrimaryLevel()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entityManager = server.EntMan;
        var prototypeManager = server.ResolveDependency<IPrototypeManager>();
        var alertLevelSystem = server.System<AlertLevelSystem>();

        EntityUid station = default;
        AlertLevelComponent alertLevel = null!;
        var yellowEnabled = false;
        var primaryStayedGreen = false;
        var additionalIgnoredPrimaryCooldown = false;
        var violetEnabled = false;
        var yellowDisabled = false;
        var disablingDidNotStartCooldown = false;

        await server.WaitPost(() =>
        {
            station = entityManager.SpawnEntity(null, MapCoordinates.Nullspace);
            alertLevel = entityManager.AddComponent<AlertLevelComponent>(station);
            alertLevel.AlertLevels = prototypeManager.Index<AlertLevelPrototype>(AlertLevelSystem.DefaultAlertLevelSet);
            alertLevel.CurrentLevel = "green";
            alertLevel.CurrentDelay = 30;

            alertLevelSystem.SetLevel(station, "yellow", false, false, component: alertLevel);
            yellowEnabled = alertLevel.ActiveAdditionalLevels.Contains("yellow");
            primaryStayedGreen = alertLevel.CurrentLevel == "green";
            additionalIgnoredPrimaryCooldown = alertLevel.CurrentDelay == 30;
            violetEnabled = alertLevelSystem.TrySetAdditionalLevel(
                station,
                "violet",
                true,
                playSound: false,
                announce: false,
                component: alertLevel);
            alertLevel.CurrentDelay = 0;
            alertLevelSystem.SetLevel(station, "red", false, false, true, component: alertLevel);
            yellowDisabled = alertLevelSystem.TrySetAdditionalLevel(
                station,
                "yellow",
                false,
                playSound: false,
                announce: false,
                component: alertLevel);
            disablingDidNotStartCooldown = alertLevel.CurrentDelay == 0;
        });

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(yellowEnabled, Is.True);
                Assert.That(primaryStayedGreen, Is.True);
                Assert.That(additionalIgnoredPrimaryCooldown, Is.True);
                Assert.That(violetEnabled, Is.True);
                Assert.That(yellowDisabled, Is.True);
                Assert.That(disablingDidNotStartCooldown, Is.True);
                Assert.That(alertLevel!.CurrentLevel, Is.EqualTo("red"));
                Assert.That(alertLevel.ActiveAdditionalLevels, Is.EquivalentTo(new[] { "violet" }));
                var stationAlertLevel = new Entity<AlertLevelComponent?>(station, alertLevel);
                Assert.That(alertLevelSystem.IsLevelActive(stationAlertLevel, "red"), Is.True);
                Assert.That(alertLevelSystem.IsLevelActive(stationAlertLevel, "violet"), Is.True);
                Assert.That(alertLevelSystem.IsLevelActive(stationAlertLevel, "yellow"), Is.False);
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task AdditionalLevelKeepsPrimaryEmergencyAccesses()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entityManager = server.EntMan;
        var prototypeManager = server.ResolveDependency<IPrototypeManager>();
        var alertLevelSystem = server.System<AlertLevelSystem>();
        var accessReaderSystem = server.System<AccessReaderSystem>();

        AlertLevelComponent alertLevel = null!;
        AccessReaderComponent accessReader = null!;
        var additionalLevelEnabled = false;

        await server.WaitPost(() =>
        {
            var station = entityManager.SpawnEntity(null, MapCoordinates.Nullspace);
            alertLevel = entityManager.AddComponent<AlertLevelComponent>(station);
            alertLevel.AlertLevels = prototypeManager.Index<AlertLevelPrototype>(AlertLevelSystem.DefaultAlertLevelSet);
            alertLevel.CurrentLevel = "red";

            var reader = entityManager.SpawnEntity("DoorElectronicsLawyer", MapCoordinates.Nullspace);
            accessReader = entityManager.GetComponent<AccessReaderComponent>(reader);
            accessReaderSystem.UpdateAccess((reader, accessReader), alertLevel.CurrentLevel);

            additionalLevelEnabled = alertLevelSystem.TrySetAdditionalLevel(
                station,
                "yellow",
                true,
                playSound: false,
                announce: false,
                force: true,
                component: alertLevel);
        });

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(additionalLevelEnabled, Is.True);
                Assert.That(alertLevel.CurrentLevel, Is.EqualTo("red"));
                Assert.That(accessReader.Group,
                    Is.EqualTo(new ProtoId<AccessGroupPrototype>("RedAlertAccesses")));
            });
        });

        await pair.CleanReturnAsync();
    }
}

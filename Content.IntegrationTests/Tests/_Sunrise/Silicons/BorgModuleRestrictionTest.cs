using System.Collections.Generic;
using Content.Server.Silicons.Borgs;
using Content.Shared.Interaction;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Sunrise.Silicons;

[TestFixture]
public sealed class BorgModuleRestrictionTest
{
    private static readonly string[] RestrictedChassis =
    [
        "BorgChassisSecurity",
        "BorgChassisPeace",
    ];

    private static readonly string[] SyndicateEmagModules =
    [
        "BorgModuleSyndicateCombatAdvanced",
        "BorgModuleSyndicateHypoAdvanced",
        "BorgModuleSyndicatePenetration",
    ];

    [Test]
    public async Task SyndicateEmagModulesCannotBeInsertedIntoRestrictedBorgs()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.EntMan;
        var map = await pair.CreateTestMap();
        var borgSystem = server.System<BorgSystem>();

        var rejected = new List<(EntityUid Chassis, EntityUid Module, AfterInteractUsingEvent Interaction)>();
        var allowed = new List<(EntityUid Chassis, EntityUid Module)>();

        await server.WaitPost(() =>
        {
            var user = entMan.SpawnEntity("MobHuman", map.GridCoords);

            foreach (var chassisPrototype in RestrictedChassis)
            {
                var chassis = entMan.SpawnEntity(chassisPrototype, map.GridCoords);

                foreach (var modulePrototype in SyndicateEmagModules)
                {
                    var module = entMan.SpawnEntity(modulePrototype, map.GridCoords);
                    var interaction = new AfterInteractUsingEvent(user, module, chassis, map.GridCoords, true);
                    entMan.EventBus.RaiseLocalEvent(chassis, interaction);
                    rejected.Add((chassis, module, interaction));
                }

                var allowedModule = entMan.SpawnEntity("BorgModuleAdvancedCombat", map.GridCoords);
                allowed.Add((chassis, allowedModule));
            }
        });

        await pair.RunTicksSync(1);

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                foreach (var (chassis, module, interaction) in rejected)
                {
                    var chassisComponent = entMan.GetComponent<BorgChassisComponent>(chassis);
                    Assert.That(interaction.Handled, Is.True);
                    Assert.That(chassisComponent.ModuleContainer.ContainedEntities, Does.Not.Contain(module),
                        $"{entMan.ToPrettyString(module)} must not be installed into {entMan.ToPrettyString(chassis)}.");
                }

                foreach (var (chassis, module) in allowed)
                {
                    var chassisComponent = entMan.GetComponent<BorgChassisComponent>(chassis);
                    var moduleComponent = entMan.GetComponent<BorgModuleComponent>(module);
                    Entity<BorgChassisComponent> chassisEntity = (chassis, chassisComponent);
                    Entity<BorgModuleComponent> moduleEntity = (module, moduleComponent);
                    Assert.That(
                        borgSystem.CanInsertModule(chassisEntity.AsNullable(), moduleEntity.AsNullable()),
                        Is.True,
                        $"{entMan.ToPrettyString(module)} must remain compatible with {entMan.ToPrettyString(chassis)}.");
                }
            });
        });

        await pair.CleanReturnAsync();
    }
}

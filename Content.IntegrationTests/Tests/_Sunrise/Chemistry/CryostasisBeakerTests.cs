using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;

namespace Content.IntegrationTests.Tests._Sunrise.Chemistry;

[TestFixture]
[TestOf(typeof(CryostasisBeakerSystem))]
public sealed class CryostasisBeakerTests
{
    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  id: TestCryostasisBeaker
  components:
  - type: SolutionContainerManager
    solutions:
      beaker:
        maxVol: 50
        canReact: false
  - type: CryostasisBeaker
    maxTemperature: 293.15

- type: reagent
  id: TestReagent
  name: reagent-name-nothing
  desc: reagent-desc-nothing
  physicalDesc: reagent-physical-desc-nothing
";

    [Test]
    public async Task CryostasisBeakerPreventsHeating()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var testMap = await pair.CreateTestMap();

        await server.WaitPost(() =>
        {
            var solutionSystem = server.System<SharedSolutionContainerSystem>();

            var beaker = server.EntMan.SpawnEntity("TestCryostasisBeaker", testMap.GridCoords);

            Assert.That(solutionSystem.TryGetSolution(beaker, "beaker", out var solutionEntity, out var solution));

            solutionSystem.TryAddReagent(solutionEntity.Value, "TestReagent", FixedPoint2.New(10));

            solutionSystem.SetTemperature(solutionEntity.Value, 500.0f);

            Assert.That(solution!.Temperature, Is.LessThanOrEqualTo(293.15f));

            solutionSystem.AddThermalEnergy(solutionEntity.Value, 10000.0f);

            Assert.That(solution.Temperature, Is.LessThanOrEqualTo(293.15f));
        });
    }

    [Test]
    public async Task NormalBeakerAllowsHeating()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var testMap = await pair.CreateTestMap();

        await server.WaitPost(() =>
        {
            var solutionSystem = server.System<SharedSolutionContainerSystem>();

            var beaker = server.EntMan.SpawnEntity("TestCryostasisBeaker", testMap.GridCoords);
            if (server.EntMan.HasComponent<CryostasisBeakerComponent>(beaker))
                server.EntMan.RemoveComponent<CryostasisBeakerComponent>(beaker);

            Assert.That(solutionSystem.TryGetSolution(beaker, "beaker", out var solutionEntity, out var solution));

            solutionSystem.TryAddReagent(solutionEntity.Value, "TestReagent", FixedPoint2.New(10));

            solutionSystem.SetTemperature(solutionEntity.Value, 500.0f);

            Assert.That(solution!.Temperature, Is.EqualTo(500.0f));
        });
    }
}

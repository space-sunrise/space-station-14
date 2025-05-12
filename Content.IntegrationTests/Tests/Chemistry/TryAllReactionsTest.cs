using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System.Linq;
using Content.Shared.Chemistry.EntitySystems;

namespace Content.IntegrationTests.Tests.Chemistry
{
    [TestFixture]
    [TestOf(typeof(ReactionPrototype))]
    public sealed class TryAllReactionsTest
    {
        [TestPrototypes]
        private const string Prototypes = @"
- type: entity
  id: TestSolutionContainer
  components:
  - type: SolutionContainerManager
    solutions:
      beaker:
        maxVol: 50
        canMix: true";

        [Test]
        public async Task TryAllTest()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;

            var entityManager = server.ResolveDependency<IEntityManager>();
            var prototypeManager = server.ResolveDependency<IPrototypeManager>();
            var testMap = await pair.CreateTestMap();
            var coordinates = testMap.GridCoords;
            var solutionContainerSystem = entityManager.System<SharedSolutionContainerSystem>();

            foreach (var reactionPrototype in prototypeManager.EnumeratePrototypes<ReactionPrototype>())
            {
                Console.WriteLine($"\n=== Testing reaction: {reactionPrototype.ID} ===");
                Console.WriteLine($"Reactants:");
                foreach (var (id, reactant) in reactionPrototype.Reactants)
                {
                    Console.WriteLine($"  {id}: {reactant.Amount} (catalyst: {reactant.Catalyst})");
                }

                EntityUid beaker = default;
                Entity<SolutionComponent>? solutionEnt = default!;
                Solution solution = null;

                await server.WaitAssertion(() =>
                {
                    beaker = entityManager.SpawnEntity("TestSolutionContainer", coordinates);
                    Assert.That(solutionContainerSystem
                        .TryGetSolution(beaker, "beaker", out solutionEnt, out solution));
                    foreach (var (id, reactant) in reactionPrototype.Reactants)
                    {
#pragma warning disable NUnit2045
                        Assert.That(solutionContainerSystem
                            .TryAddReagent(solutionEnt.Value, id, reactant.Amount, out var quantity));
                        Assert.That(reactant.Amount, Is.EqualTo(quantity));
#pragma warning restore NUnit2045
                    }

                    solutionContainerSystem.SetTemperature(solutionEnt.Value, reactionPrototype.MinimumTemperature);

                    if (reactionPrototype.MixingCategories != null)
                    {
                        var dummyEntity = entityManager.SpawnEntity(null, MapCoordinates.Nullspace);
                        var mixerComponent = entityManager.AddComponent<ReactionMixerComponent>(dummyEntity);
                        mixerComponent.ReactionTypes = reactionPrototype.MixingCategories;
                        solutionContainerSystem.UpdateChemicals(solutionEnt.Value, true, mixerComponent);
                    }
                });

                await server.WaitIdleAsync();

                await server.WaitAssertion(() =>
                {
                    var foundProductsMap = reactionPrototype.Products
                        .Concat(reactionPrototype.Reactants.Where(x => x.Value.Catalyst).ToDictionary(x => x.Key, x => x.Value.Amount))
                        .ToDictionary(x => x, _ => false);

                    Console.WriteLine($"\nExpected products for {reactionPrototype.ID}:");
                    foreach (var (reagent, quantity) in foundProductsMap)
                    {
                        Console.WriteLine($"  {reagent.Key}: {reagent.Value}");
                    }

                    Console.WriteLine($"\nActual solution contents for {reactionPrototype.ID}:");
                    foreach (var (reagent, quantity) in solution.Contents)
                    {
                        Console.WriteLine($"  {reagent.Prototype}: {quantity}");
                    }

                    foreach (var (reagent, quantity) in solution.Contents)
                    {
                        var found = foundProductsMap.TryFirstOrNull(x => x.Key.Key == reagent.Prototype && x.Key.Value == quantity, out var foundProduct);
                        if (!found)
                        {
                            Console.WriteLine($"\nERROR: Failed to find match for {reagent.Prototype} x{quantity}");
                            var possibleMatches = foundProductsMap.Where(x => x.Key.Key == reagent.Prototype).ToList();
                            if (possibleMatches.Any())
                            {
                                Console.WriteLine("Possible matches with different quantities:");
                                foreach (var match in possibleMatches)
                                {
                                    Console.WriteLine($"  {match.Key.Key} x{match.Key.Value}");
                                }
                            }
                            else
                            {
                                Console.WriteLine("No matches found for this reagent type");
                            }
                        }
                        Assert.That(found, Is.True, $"Failed to find match for {reagent.Prototype} x{quantity} in reaction {reactionPrototype.ID}");
                        foundProductsMap[foundProduct.Value.Key] = true;
                    }

                    var missingProducts = foundProductsMap.Where(x => !x.Value).ToList();
                    if (missingProducts.Any())
                    {
                        Console.WriteLine($"\nERROR: Missing expected products in {reactionPrototype.ID}:");
                        foreach (var (reagent, _) in missingProducts)
                        {
                            Console.WriteLine($"  {reagent.Key}: {reagent.Value}");
                        }
                    }

                    Assert.That(foundProductsMap.All(x => x.Value), $"Not all expected products were found in reaction {reactionPrototype.ID}");
                });
            }
            await pair.CleanReturnAsync();
        }
    }
}

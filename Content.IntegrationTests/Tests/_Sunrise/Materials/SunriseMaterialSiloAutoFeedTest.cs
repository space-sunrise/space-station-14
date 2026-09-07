#nullable enable
using Content.Server._Sunrise.Materials.MaterialSilo;
using Content.Server.Lathe;
using Content.Server.Lathe.Components;
using Content.Server.Power.Components;
using Content.Shared.Lathe;
using Content.Shared.Materials;
using Content.Shared.Materials.OreSilo;
using Content.Shared.Stacks;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Sunrise.Materials;

/// <summary>
/// Регрессионный тест на дублирование материалов: переработчик руды, подключённый к силосу через
/// ванильный OreSilo, должен сразу передавать продукцию в силос и не оставлять физический стак,
/// который потом сливается с уже существующим соседним стаком (см. SunriseOreProcessorAutoFeedSystem
/// и правку в LatheSystem, проверяющую EntityManager.IsQueuedForDeletion перед TryMergeToContacts).
/// </summary>
[TestFixture]
[TestOf(typeof(SunriseOreProcessorAutoFeedSystem))]
public sealed class SunriseMaterialSiloAutoFeedTest
{
    [Test]
    public async Task AutoFeedDoesNotDuplicateMaterialWhenNearbyStackExists()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.ResolveDependency<IEntityManager>();
        var lathe = server.System<LatheSystem>();
        var materialStorage = server.System<SharedMaterialStorageSystem>();

        var testMap = await pair.CreateTestMap();
        var coords = testMap.GridCoords;

        EntityUid silo = default;
        EntityUid processor = default;
        EntityUid preexisting = default;

        await server.WaitAssertion(() =>
        {
            // Наш грид-сило — это ванильный MachineMaterialSilo с увеличенным range (см. silo.yml),
            // поэтому линковка идёт через штатный OreSilo-механизм без каких-либо Sunrise-компонентов.
            silo = entMan.SpawnEntity("SunriseMachineMaterialSilo", coords);
            processor = entMan.SpawnEntity("OreProcessor", coords);

            var siloPower = entMan.GetComponent<ApcPowerReceiverComponent>(silo);
            siloPower.NeedsPower = false;
            siloPower.Powered = true;
            var processorPower = entMan.GetComponent<ApcPowerReceiverComponent>(processor);
            processorPower.NeedsPower = false;
            processorPower.Powered = true;

            // Связываем клиента с силосом тем же путём, что и UI (Silo/Clients доступны на запись
            // только из SharedOreSiloSystem, см. [Access] на ванильных компонентах).
            var toggleMsg = new ToggleOreSiloClientMessage(entMan.GetNetEntity(processor));
            entMan.EventBus.RaiseLocalEvent(silo, toggleMsg);

            // Заранее кладём такой же стак рядом с переработчиком: если продукция дублируется,
            // это будет видно по тому, что она "слилась" в этот стак вместо того, чтобы просто исчезнуть.
            preexisting = entMan.SpawnEntity("SheetSteel1", coords);
        });

        await server.WaitAssertion(() =>
        {
            var latheComp = entMan.GetComponent<LatheComponent>(processor);
            latheComp.CurrentRecipe = "SheetSteel";
            entMan.EnsureComponent<LatheProducingComponent>(processor);
            lathe.FinishProducing(processor, latheComp);
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            Assert.That(entMan.TryGetComponent<StackComponent>(preexisting, out var preexistingStack));
            Assert.That(preexistingStack!.Count, Is.EqualTo(1),
                "Уже существовавший стак не должен был впитать в себя продукцию, уже учтённую в силосе — иначе материал задублирован.");

            var siloMaterials = materialStorage.GetStoredMaterials(silo);
            siloMaterials.TryGetValue("Steel", out var steelAmount);
            Assert.That(steelAmount, Is.EqualTo(100),
                "Продукция переработчика должна была уйти напрямую в подключённый силос.");
        });

        await pair.CleanReturnAsync();
    }
}

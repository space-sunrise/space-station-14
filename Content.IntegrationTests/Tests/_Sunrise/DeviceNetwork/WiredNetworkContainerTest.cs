using Content.IntegrationTests.Tests.DeviceNetwork;
using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests._Sunrise.DeviceNetwork;

/// <summary>
/// Проверяет поведение <see cref="WiredNetworkSystem"/> с учётом фикса:
/// устройства в контейнерах (надетые бодикамеры) обходят грид-проверку.
/// </summary>
[TestFixture]
[TestOf(typeof(WiredNetworkComponent))]
public sealed class WiredNetworkContainerTest
{
    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  id: WiredContainerTestReceiver
  components:
    - type: DeviceNetwork
      deviceNetId: Wired
      transmitFrequency: 555
      receiveFrequency: 555
    - type: WiredNetworkConnection

- type: entity
  id: WiredContainerTestSender
  components:
    - type: DeviceNetwork
      deviceNetId: Wired
      transmitFrequency: 555
      receiveFrequency: 555
";

    /// <summary>
    /// Отправитель в контейнере на другом гриде — пакет должен дойти до получателя.
    /// </summary>
    [Test]
    public async Task WiredSender_InContainer_DifferentGrid_DeliversPacket()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.EntMan;
        var mapMan = server.MapMan;
        var mapSys = entMan.System<SharedMapSystem>();
        var devNetSys = entMan.System<DeviceNetworkSystem>();
        var devNetTestSys = entMan.System<DeviceNetworkTestSystem>();
        var containerSys = entMan.System<SharedContainerSystem>();

        var testMap = await pair.CreateTestMap();

        Entity<MapGridComponent> grid1 = default;
        Entity<MapGridComponent> grid2 = default;
        EntityUid receiver = default;
        EntityUid sender = default;
        DeviceNetworkComponent receiverNet = default!;
        var payload = new NetworkPayload { ["test"] = "container_bypass" };

        await server.WaitPost(() =>
        {
            grid1 = mapMan.CreateGridEntity(testMap.MapId);
            grid2 = mapMan.CreateGridEntity(testMap.MapId);
            mapSys.SetTile(grid1, grid1.Comp, Vector2i.Zero, new Tile(1));
            mapSys.SetTile(grid2, grid2.Comp, Vector2i.Zero, new Tile(1));

            receiver = entMan.SpawnEntity("WiredContainerTestReceiver", new EntityCoordinates(grid1, 0.5f, 0.5f));
            entMan.TryGetComponent(receiver, out receiverNet);

            // Держатель на grid2, sender вставляется в его контейнер
            var holder = entMan.SpawnEntity(null, new EntityCoordinates(grid2, 0.5f, 0.5f));
            sender = entMan.SpawnEntity("WiredContainerTestSender", new EntityCoordinates(grid2, 0.5f, 0.5f));
            var container = containerSys.EnsureContainer<ContainerSlot>(holder, "test_slot");
            containerSys.Insert(sender, container);
        });

        await server.WaitRunTicks(2);

        await server.WaitPost(() =>
        {
            devNetTestSys.LastPayload = default!;
            devNetSys.QueuePacket(sender, receiverNet.Address, payload, receiverNet.ReceiveFrequency!.Value);
        });

        await server.WaitRunTicks(2);

        await server.WaitAssertion(() =>
        {
            // Отправитель в контейнере — грид-проверка обходится, пакет доходит
            Assert.That(payload, Is.EqualTo(devNetTestSys.LastPayload).AsCollection);
        });

        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// Отправитель НЕ в контейнере на другом гриде — пакет должен быть заблокирован.
    /// </summary>
    [Test]
    public async Task WiredSender_NotInContainer_DifferentGrid_BlocksPacket()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.EntMan;
        var mapMan = server.MapMan;
        var mapSys = entMan.System<SharedMapSystem>();
        var devNetSys = entMan.System<DeviceNetworkSystem>();
        var devNetTestSys = entMan.System<DeviceNetworkTestSystem>();

        var testMap = await pair.CreateTestMap();

        Entity<MapGridComponent> grid1 = default;
        Entity<MapGridComponent> grid2 = default;
        EntityUid receiver = default;
        EntityUid sender = default;
        DeviceNetworkComponent receiverNet = default!;
        var payload = new NetworkPayload { ["test"] = "should_be_blocked" };

        await server.WaitPost(() =>
        {
            grid1 = mapMan.CreateGridEntity(testMap.MapId);
            grid2 = mapMan.CreateGridEntity(testMap.MapId);
            mapSys.SetTile(grid1, grid1.Comp, Vector2i.Zero, new Tile(1));
            mapSys.SetTile(grid2, grid2.Comp, Vector2i.Zero, new Tile(1));

            receiver = entMan.SpawnEntity("WiredContainerTestReceiver", new EntityCoordinates(grid1, 0.5f, 0.5f));
            entMan.TryGetComponent(receiver, out receiverNet);

            // Отправитель на grid2 без контейнера
            sender = entMan.SpawnEntity("WiredContainerTestSender", new EntityCoordinates(grid2, 0.5f, 0.5f));
        });

        await server.WaitRunTicks(2);

        await server.WaitPost(() =>
        {
            devNetTestSys.LastPayload = default!;
            devNetSys.QueuePacket(sender, receiverNet.Address, payload, receiverNet.ReceiveFrequency!.Value);
        });

        await server.WaitRunTicks(2);

        await server.WaitAssertion(() =>
        {
            // Отправитель не в контейнере — WiredNetworkSystem блокирует пакет
            Assert.That(payload, Is.Not.EqualTo(devNetTestSys.LastPayload).AsCollection);
        });

        await pair.CleanReturnAsync();
    }
}

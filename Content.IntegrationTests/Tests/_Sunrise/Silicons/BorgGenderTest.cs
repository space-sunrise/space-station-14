using System.Linq;
using Content.Shared._Sunrise.Silicons.Borgs;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Inventory;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Client.GameObjects;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests._Sunrise.Silicons;

[TestFixture]
public sealed class BorgGenderTest
{
    [TestPrototypes]
    private const string Prototypes = @"
- type: borgType
  id: SunriseTestBorgGenderOverride
  dummyPrototype: BorgChassisSelectable
  spriteBodyState: robot
  spriteHasMindState: robot_e
  spriteNoMindState: robot_e_r
  spriteToggleLightState: robot_l
  genderSprites:
    Female:
      body:
        state: medical
      hasMind:
        state: medical_e
      noMind:
        state: medical_e_r
      toggleLight:
        state: medical_l
  job: Borg

- type: entity
  parent: BorgChassisSelectable
  id: SunriseTestBorgGenderOverrideChassis
  components:
  - type: BorgSwitchableType
    selectedBorgType: SunriseTestBorgGenderOverride

- type: borgType
  id: SunriseTestBorgGenderNoOverride
  dummyPrototype: BorgChassisSelectable
  spriteBodyState: engineer
  spriteHasMindState: engineer_e
  spriteNoMindState: engineer_e_r
  spriteToggleLightState: engineer_l
  job: Borg

- type: entity
  parent: BorgChassisSelectable
  id: SunriseTestBorgGenderNoOverrideChassis
  components:
  - type: BorgSwitchableType
    selectedBorgType: SunriseTestBorgGenderNoOverride
";

    [Test]
    public async Task BorgGenderActionOpensUiWithCurrentGender()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = true,
        });

        var server = pair.Server;
        var client = pair.Client;
        var entMan = server.EntMan;
        EntityUid borg = default;
        EntityUid clientAction = default;

        await server.WaitPost(() =>
        {
            borg = entMan.SpawnEntity("BorgChassisSelectable", MapCoordinates.Nullspace);
            entMan.RemoveComponent<InventoryComponent>(borg);

            var session = server.ResolveDependency<IPlayerManager>().Sessions.Single();
            server.PlayerMan.SetAttachedEntity(session, borg);
        });

        await pair.RunTicksSync(5);
        await pair.SyncTicks(targetDelta: 1);

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(entMan.HasComponent<BorgGenderComponent>(borg), Is.True);
                Assert.That(TryFindAction(entMan, borg, "ActionChangeBorgGender", out _), Is.True);
                Assert.That(entMan.GetComponent<BorgGenderComponent>(borg).SelectedGender, Is.EqualTo(BorgGender.Male));
            });
        });

        await client.WaitAssertion(() =>
        {
            var clientBorg = pair.ToClientUid(borg);
            Assert.That(client.ResolveDependency<Robust.Client.Player.IPlayerManager>().LocalEntity, Is.EqualTo(clientBorg));
            Assert.That(TryFindAction(client.EntMan, clientBorg, "ActionChangeBorgGender", out clientAction), Is.True);
        });

        await client.WaitPost(() =>
        {
            client.EntMan.System<Content.Client.Actions.ActionsSystem>().TriggerAction(
                (clientAction, client.EntMan.GetComponent<ActionComponent>(clientAction)));
        });

        await pair.RunTicksSync(15);

        await server.WaitAssertion(() =>
        {
            Assert.That(
                entMan.System<SharedUserInterfaceSystem>().TryGetUiState<BorgGenderBuiState>(
                    borg,
                    BorgGenderUiKey.Key,
                    out var state),
                Is.True);
            Assert.That(state.SelectedGender, Is.EqualTo(BorgGender.Male));
        });

        await client.WaitAssertion(() =>
        {
            var clientBorg = pair.ToClientUid(borg);
            Assert.That(
                client.EntMan.System<SharedUserInterfaceSystem>().TryGetOpenUi(clientBorg, BorgGenderUiKey.Key, out var bui),
                Is.True);
            Assert.That(
                bui,
                Is.TypeOf<Content.Client._Sunrise.Silicons.Borgs.BorgGenderBoundUserInterface>());
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task BorgGenderBuiMessageChangesSelectedGender()
    {
        await using var pair = await PoolManager.GetServerClient();

        var server = pair.Server;
        var entMan = server.EntMan;
        EntityUid borg = default;

        await server.WaitPost(() =>
        {
            borg = entMan.SpawnEntity("BorgChassisSelectable", MapCoordinates.Nullspace);

            var message = new BorgGenderChangeMessage(BorgGender.Female)
            {
                Actor = borg,
                Entity = entMan.GetNetEntity(borg),
                UiKey = BorgGenderUiKey.Key,
            };

            entMan.EventBus.RaiseLocalEvent(borg, message);
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            Assert.That(entMan.GetComponent<BorgGenderComponent>(borg).SelectedGender, Is.EqualTo(BorgGender.Female));
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task FemaleGenderWithoutOverrideKeepsSelectedTypeBodyLayer()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = true,
        });

        var server = pair.Server;
        var client = pair.Client;
        var entMan = server.EntMan;
        EntityUid borg = default;

        await server.WaitPost(() =>
        {
            borg = entMan.SpawnEntity("SunriseTestBorgGenderNoOverrideChassis", MapCoordinates.Nullspace);
            entMan.RemoveComponent<InventoryComponent>(borg);

            var session = server.ResolveDependency<IPlayerManager>().Sessions.Single();
            server.PlayerMan.SetAttachedEntity(session, borg);
        });

        await pair.RunTicksSync(10);
        await pair.SyncTicks(targetDelta: 1);

        await server.WaitPost(() =>
        {
            entMan.System<SharedBorgGenderSystem>().TrySetGender(
                (borg, entMan.GetComponent<BorgGenderComponent>(borg)),
                BorgGender.Female);
        });

        await pair.RunTicksSync(10);
        await pair.SyncTicks(targetDelta: 1);

        await client.WaitAssertion(() =>
        {
            Assert.That(GetClientBodyLayerState(client.EntMan, pair.ToClientUid(borg)), Is.EqualTo("engineer"));
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task GenderSelectedBeforeBorgTypeAppliesWhenTypeSelected()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = true,
        });

        var server = pair.Server;
        var client = pair.Client;
        var entMan = server.EntMan;
        EntityUid borg = default;

        await server.WaitPost(() =>
        {
            borg = entMan.SpawnEntity("BorgChassisSelectable", MapCoordinates.Nullspace);
            entMan.RemoveComponent<InventoryComponent>(borg);

            var session = server.ResolveDependency<IPlayerManager>().Sessions.Single();
            server.PlayerMan.SetAttachedEntity(session, borg);

            Assert.That(entMan.System<SharedBorgGenderSystem>().TrySetGender(
                (borg, entMan.GetComponent<BorgGenderComponent>(borg)),
                BorgGender.Female), Is.True);

            var selectMessage = new BorgSelectTypeMessage("SunriseTestBorgGenderOverride")
            {
                Actor = borg,
                Entity = entMan.GetNetEntity(borg),
                UiKey = BorgSwitchableTypeUiKey.SelectBorgType,
            };

            entMan.EventBus.RaiseLocalEvent(borg, selectMessage);
        });

        await pair.RunTicksSync(15);
        await pair.SyncTicks(targetDelta: 1);

        await server.WaitAssertion(() =>
        {
            Assert.That(entMan.GetComponent<BorgGenderComponent>(borg).SelectedGender, Is.EqualTo(BorgGender.Female));
            Assert.That(
                entMan.GetComponent<BorgSwitchableTypeComponent>(borg).SelectedBorgType?.Id,
                Is.EqualTo("SunriseTestBorgGenderOverride"));
        });

        await client.WaitAssertion(() =>
        {
            Assert.That(GetClientBodyLayerState(client.EntMan, pair.ToClientUid(borg)), Is.EqualTo("medical"));
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task FemaleGenderOverrideChangesClientBodyLayer()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = true,
        });

        var server = pair.Server;
        var client = pair.Client;
        var entMan = server.EntMan;
        EntityUid borg = default;
        var changedGender = false;

        await server.WaitPost(() =>
        {
            borg = entMan.SpawnEntity("SunriseTestBorgGenderOverrideChassis", MapCoordinates.Nullspace);
            entMan.RemoveComponent<InventoryComponent>(borg);

            var session = server.ResolveDependency<IPlayerManager>().Sessions.Single();
            server.PlayerMan.SetAttachedEntity(session, borg);
        });

        await pair.RunTicksSync(10);
        await pair.SyncTicks(targetDelta: 1);

        await server.WaitPost(() =>
        {
            changedGender = entMan.System<SharedBorgGenderSystem>().TrySetGender(
                (borg, entMan.GetComponent<BorgGenderComponent>(borg)),
                BorgGender.Female);
        });

        await pair.RunTicksSync(10);
        await pair.SyncTicks(targetDelta: 1);

        await server.WaitAssertion(() =>
        {
            Assert.That(changedGender, Is.True);
            Assert.That(entMan.GetComponent<BorgGenderComponent>(borg).SelectedGender, Is.EqualTo(BorgGender.Female));
        });

        await client.WaitAssertion(() =>
        {
            var clientBorg = pair.ToClientUid(borg);
            var sprite = client.EntMan.GetComponent<SpriteComponent>(clientBorg);
            var spriteSystem = client.EntMan.System<SpriteSystem>();
            var bodyLayer = spriteSystem.LayerMapGet((clientBorg, sprite), BorgVisualLayers.Body);
            var bodyState = spriteSystem.LayerGetRsiState((clientBorg, sprite), bodyLayer);

            Assert.That(bodyState.Name, Is.EqualTo("medical"));
        });

        await pair.CleanReturnAsync();
    }

    private static bool TryFindAction(IEntityManager entMan, EntityUid performer, string prototypeId, out EntityUid action)
    {
        action = default;

        if (!entMan.TryGetComponent<ActionsComponent>(performer, out var actions))
            return false;

        foreach (var actionUid in actions.Actions)
        {
            if (entMan.GetComponent<MetaDataComponent>(actionUid).EntityPrototype?.ID != prototypeId)
                continue;

            action = actionUid;
            return true;
        }

        return false;
    }

    private static string GetClientBodyLayerState(IEntityManager entMan, EntityUid borg)
    {
        var sprite = entMan.GetComponent<SpriteComponent>(borg);
        var spriteSystem = entMan.System<SpriteSystem>();
        var bodyLayer = spriteSystem.LayerMapGet((borg, sprite), BorgVisualLayers.Body);

        return spriteSystem.LayerGetRsiState((borg, sprite), bodyLayer).Name!;
    }
}

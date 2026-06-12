using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Content.IntegrationTests.Tests.Helpers;
using Content.Server.Destructible;
using Content.Server.Hands.Systems;
using Content.Server.Power.Components;
using Content.Server.Silicons.Laws;
using Content.Server.Silicons.Borgs;
using Content.Shared._Sunrise.Silicons.Borgs;
using Content.Server._Sunrise.Silicons.StationAi;
using Content.Shared._Sunrise.Silicons.StationAi;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Alert;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Doors.Components;
using Content.Shared.Electrocution;
using Content.Shared.Holopad;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Telephone;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Server.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using StationAiServerSystem = Content.Server.Silicons.StationAi.StationAiSystem;

namespace Content.IntegrationTests.Tests._Sunrise.Silicons;

[TestFixture]
public sealed class StationAiBodyTest
{
    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  id: SunriseStationAiBodyTestAirlock
  name: AI body test airlock
  components:
  - type: StationAiWhitelist
  - type: Appearance
  - type: DoorBolt
  - type: Airlock
    powered: true
  - type: Electrified
  - type: AccessReader
    access:
    - [ Captain ]

- type: entity
  id: SunriseStationAiBodyTestUser
  components:
  - type: DoAfter
  - type: Hands
    hands:
      hand_right:
        location: Right
    sortedHands:
    - hand_right
  - type: ComplexInteraction

- type: entity
  id: SunriseStationAiBodyTestHolopad
  name: test holopad
  components:
  - type: Transform
  - type: Holopad
  - type: Telephone
    transmissionRange: Unlimited
    compatibleRanges:
    - Grid
    - Map
    - Unlimited
  - type: UserInterface
    interfaces:
      enum.HolopadUiKey.InteractionWindow:
        type: HolopadBoundUserInterface
";

    [Test]
    public async Task StationAiBodyManagementActionOpensBodyUi()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = true,
        });
        var testMap = await pair.CreateTestMap();
        var server = pair.Server;
        var client = pair.Client;
        var entMan = server.EntMan;

        EntityUid aiBrain = default;
        EntityUid clientAction = default;

        await server.WaitPost(() =>
        {
            aiBrain = SpawnStationAiCoreWithMind(entMan, "Astra", out _, testMap.MapCoords);
            entMan.EnsureComponent<TestListenerComponent>(aiBrain);

            var session = server.ResolveDependency<IPlayerManager>().Sessions.Single();
            server.PlayerMan.SetAttachedEntity(session, aiBrain);
        });

        await pair.RunTicksSync(5);
        await pair.SyncTicks(targetDelta: 1);

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(entMan.HasComponent<StationAiBodyControllerComponent>(aiBrain), Is.True);
                Assert.That(TryFindAction(entMan, aiBrain, "ActionStationAiBodyMenu", out _), Is.True);
            });
        });

        await client.WaitAssertion(() =>
        {
            var clientAiBrain = pair.ToClientUid(aiBrain);
            Assert.That(client.ResolveDependency<Robust.Client.Player.IPlayerManager>().LocalEntity, Is.EqualTo(clientAiBrain));
            Assert.That(TryFindAction(client.EntMan, clientAiBrain, "ActionStationAiBodyMenu", out clientAction), Is.True);
        });

        await client.WaitPost(() =>
        {
            client.EntMan.System<Content.Client.Actions.ActionsSystem>().TriggerAction(
                (clientAction, client.EntMan.GetComponent<ActionComponent>(clientAction)));
        });

        await pair.RunTicksSync(15);

        await server.WaitAssertion(() =>
        {
            var listener = entMan.System<StationAiBodyUiOpenedTestSystem>();

            Assert.That(
                listener.Count(aiBrain, ev => ev.UiKey.Equals(StationAiBodyUiKey.Key) && ev.Actor == aiBrain),
                Is.EqualTo(1));
        });

        await client.WaitAssertion(() =>
        {
            var clientAiBrain = pair.ToClientUid(aiBrain);
            Assert.That(
                client.EntMan.System<SharedUserInterfaceSystem>().TryGetOpenUi(clientAiBrain, StationAiBodyUiKey.Key, out var bui),
                Is.True);
            Assert.That(
                bui,
                Is.TypeOf<Content.Client._Sunrise.Silicons.StationAi.StationAiBodyBoundUserInterface>());
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task ControlledBodyManagementActionOpensBodyUiWhenCoreIsOutOfPvs()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = true,
        });
        var testMap = await pair.CreateTestMap();
        var server = pair.Server;
        var client = pair.Client;
        var entMan = server.EntMan;
        var coreCoords = new MapCoordinates(Vector2.Zero, testMap.MapId);
        var bodyCoords = new MapCoordinates(new Vector2(128f, 0f), testMap.MapId);

        EntityUid body = default;
        EntityUid aiBrain = default;
        NetEntity aiBrainNet = default;
        EntityUid clientAction = default;
        var entered = false;

        await server.WaitPost(() =>
        {
            body = SpawnPreparedBody(entMan, bodyCoords);
            entMan.RemoveComponent<InventoryComponent>(body);
            aiBrain = SpawnStationAiCoreWithMind(entMan, "Astra", out var mindId, coreCoords);
            aiBrainNet = entMan.GetNetEntity(aiBrain);
            entMan.EnsureComponent<TestListenerComponent>(aiBrain);

            var session = server.ResolveDependency<IPlayerManager>().Sessions.Single();
            entMan.System<SharedMindSystem>().SetUserId(mindId, session.UserId);

            entered = entMan.System<StationAiBodySystem>().TryEnterBody(aiBrain, body);
        });

        await pair.RunTicksSync(20);
        await pair.SyncTicks(targetDelta: 1);

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(entered, Is.True);
                Assert.That(server.ResolveDependency<IPlayerManager>().Sessions.Single().AttachedEntity, Is.EqualTo(body));
                Assert.That(TryFindAction(entMan, body, "ActionStationAiBodyMenu", out _), Is.True);
            });
        });

        await client.WaitAssertion(() =>
        {
            var clientBody = pair.ToClientUid(body);
            Assert.That(client.ResolveDependency<Robust.Client.Player.IPlayerManager>().LocalEntity, Is.EqualTo(clientBody));
            Assert.That(TryFindAction(client.EntMan, clientBody, "ActionStationAiBodyMenu", out clientAction), Is.True);
        });

        await client.WaitPost(() =>
        {
            client.EntMan.System<Content.Client.Actions.ActionsSystem>().TriggerAction(
                (clientAction, client.EntMan.GetComponent<ActionComponent>(clientAction)));
        });

        await pair.RunTicksSync(20);

        await server.WaitAssertion(() =>
        {
            var listener = entMan.System<StationAiBodyUiOpenedTestSystem>();

            Assert.That(
                listener.Count(aiBrain, ev => ev.UiKey.Equals(StationAiBodyUiKey.Key) && ev.Actor == body),
                Is.EqualTo(1));
        });

        await client.WaitAssertion(() =>
        {
            var clientAiBrain = client.EntMan.GetEntity(aiBrainNet);
            Assert.That(
                client.EntMan.System<SharedUserInterfaceSystem>().TryGetOpenUi(clientAiBrain, StationAiBodyUiKey.Key, out var bui),
                Is.True);
            Assert.That(
                bui,
                Is.TypeOf<Content.Client._Sunrise.Silicons.StationAi.StationAiBodyBoundUserInterface>());
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task FreeBodyAlternativeVerbLetsStationAiEnterBody()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.EntMan;

        EntityUid body = default;
        EntityUid aiBrain = default;
        EntityUid mindId = default;
        AlternativeVerb enterVerb = null!;

        await server.WaitPost(() =>
        {
            body = SpawnPreparedBody(entMan);
            aiBrain = SpawnStationAiWithMind(entMan, "Astra", out mindId);

            enterVerb = entMan.System<SharedVerbSystem>()
                .GetLocalVerbs(body, aiBrain, typeof(AlternativeVerb), force: true)
                .OfType<AlternativeVerb>()
                .SingleOrDefault(verb => verb.Text == Loc.GetString("station-ai-body-enter-verb"));

            enterVerb?.Act?.Invoke();
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(enterVerb, Is.Not.Null);
                Assert.That(
                    enterVerb!.Icon,
                    Is.EqualTo(new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/in.svg.192dpi.png"))));
                Assert.That(entMan.System<SharedMindSystem>().TryGetMind(body, out var bodyMindId, out _), Is.True);
                Assert.That(bodyMindId, Is.EqualTo(mindId));
                Assert.That(entMan.GetComponent<StationAiBodyComponent>(body).LinkedAi, Is.EqualTo(aiBrain));
                Assert.That(entMan.GetComponent<StationAiBodyControllerComponent>(aiBrain).CurrentBody, Is.EqualTo(body));
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task CommunicationBoardInsertedIntoEmptyBorgCreatesFreeAiBody()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.EntMan;

        EntityUid chassis = default;
        EntityUid board = default;

        await server.WaitPost(() =>
        {
            chassis = entMan.SpawnEntity("BorgChassisSelectable", MapCoordinates.Nullspace);
            board = entMan.SpawnEntity("SunriseStationAiCommunicationBoard", MapCoordinates.Nullspace);

            var borg = entMan.GetComponent<BorgChassisComponent>(chassis);
            entMan.System<SharedContainerSystem>().Insert(board, borg.BrainContainer);
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(entMan.TryGetComponent<StationAiBodyComponent>(chassis, out var body), Is.True);
                Assert.That(body!.BodyNumber, Is.EqualTo(1));
                Assert.That(body.Board, Is.EqualTo(board));
                Assert.That(body.LinkedAi, Is.Null);
                Assert.That(entMan.HasComponent<BorgBrainComponent>(board), Is.True);
            });

            var accessReader = entMan.GetComponent<AccessReaderComponent>(chassis);
            Assert.That(accessReader.AccessLists, Is.EquivalentTo(new[]
            {
                new HashSet<ProtoId<AccessLevelPrototype>> { "Captain" },
                new HashSet<ProtoId<AccessLevelPrototype>> { "ResearchDirector" },
                new HashSet<ProtoId<AccessLevelPrototype>> { "CentralCommand" },
            }));
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task StationAiCanEnterFreeBodyTransfersMindIntoChassis()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.EntMan;

        EntityUid chassis = default;
        EntityUid aiBrain = default;
        EntityUid mindId = default;
        var entered = false;

        await server.WaitPost(() =>
        {
            chassis = SpawnPreparedBody(entMan);
            aiBrain = entMan.SpawnEntity("StationAiBrain", MapCoordinates.Nullspace);
            entMan.System<MetaDataSystem>().SetEntityName(aiBrain, "Astra");

            var mind = entMan.System<SharedMindSystem>().CreateMind(null, "Astra");
            mindId = mind.Owner;
            entMan.System<SharedMindSystem>().TransferTo(mindId, aiBrain, mind: mind.Comp);

            entered = entMan.System<StationAiBodySystem>().TryEnterBody(aiBrain, chassis);
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(entered, Is.True);
                Assert.That(entMan.System<SharedMindSystem>().TryGetMind(chassis, out var chassisMindId, out _), Is.True);
                Assert.That(chassisMindId, Is.EqualTo(mindId));
                Assert.That(entMan.System<SharedMindSystem>().TryGetMind(aiBrain, out _, out _), Is.False);
                Assert.That(
                    entMan.GetComponent<MetaDataComponent>(chassis).EntityName,
                    Is.EqualTo(entMan.GetComponent<MetaDataComponent>(aiBrain).EntityName));
            });

            var body = entMan.GetComponent<StationAiBodyComponent>(chassis);
            Assert.Multiple(() =>
            {
                Assert.That(body.LinkedAi, Is.EqualTo(aiBrain));
                Assert.That(entMan.GetComponent<StationAiBodyControllerComponent>(aiBrain).CurrentBody, Is.EqualTo(chassis));
                Assert.That(entMan.GetComponent<AccessReaderComponent>(chassis).Enabled, Is.True);
                Assert.That(entMan.GetComponent<IntrinsicRadioTransmitterComponent>(chassis).Channels, Is.EquivalentTo(StationAiRadioChannels));
                Assert.That(entMan.GetComponent<ActiveRadioComponent>(chassis).Channels, Is.EquivalentTo(StationAiRadioChannels));
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task EnteringSecondFreeBodyReleasesFirstBody()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.EntMan;

        EntityUid firstBody = default;
        EntityUid secondBody = default;
        EntityUid aiBrain = default;
        EntityUid mindId = default;
        var enteredFirst = false;
        var enteredSecond = false;

        await server.WaitPost(() =>
        {
            firstBody = SpawnPreparedBody(entMan);
            secondBody = SpawnPreparedBody(entMan);
            aiBrain = SpawnStationAiWithMind(entMan, "Astra", out mindId);

            var bodySystem = entMan.System<StationAiBodySystem>();
            enteredFirst = bodySystem.TryEnterBody(aiBrain, firstBody);
            enteredSecond = bodySystem.TryEnterBody(aiBrain, secondBody);
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(enteredFirst, Is.True);
                Assert.That(enteredSecond, Is.True);
                Assert.That(entMan.System<SharedMindSystem>().TryGetMind(secondBody, out var bodyMindId, out _), Is.True);
                Assert.That(bodyMindId, Is.EqualTo(mindId));
                Assert.That(entMan.System<SharedMindSystem>().TryGetMind(firstBody, out _, out _), Is.False);
            });

            var first = entMan.GetComponent<StationAiBodyComponent>(firstBody);
            var second = entMan.GetComponent<StationAiBodyComponent>(secondBody);

            Assert.Multiple(() =>
            {
                Assert.That(first.LinkedAi, Is.Null);
                Assert.That(second.LinkedAi, Is.EqualTo(aiBrain));
                Assert.That(entMan.GetComponent<StationAiBodyControllerComponent>(aiBrain).CurrentBody, Is.EqualTo(secondBody));
                Assert.That(entMan.GetComponent<MetaDataComponent>(secondBody).EntityName, Is.EqualTo(entMan.GetComponent<MetaDataComponent>(aiBrain).EntityName));
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task SecondStationAiCannotEnterOccupiedBody()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.EntMan;

        EntityUid body = default;
        EntityUid firstAi = default;
        EntityUid secondAi = default;
        EntityUid firstMindId = default;
        EntityUid secondMindId = default;
        var firstEntered = false;
        var secondEntered = true;

        await server.WaitPost(() =>
        {
            body = SpawnPreparedBody(entMan);
            firstAi = SpawnStationAiWithMind(entMan, "Astra", out firstMindId);
            secondAi = SpawnStationAiWithMind(entMan, "Borealis", out secondMindId);

            var bodySystem = entMan.System<StationAiBodySystem>();
            firstEntered = bodySystem.TryEnterBody(firstAi, body);
            secondEntered = bodySystem.TryEnterBody(secondAi, body);
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(firstEntered, Is.True);
                Assert.That(secondEntered, Is.False);
                Assert.That(entMan.System<SharedMindSystem>().TryGetMind(body, out var bodyMindId, out _), Is.True);
                Assert.That(bodyMindId, Is.EqualTo(firstMindId));
                Assert.That(entMan.System<SharedMindSystem>().TryGetMind(secondAi, out var secondAiMindId, out _), Is.True);
                Assert.That(secondAiMindId, Is.EqualTo(secondMindId));
            });

            var bodyComponent = entMan.GetComponent<StationAiBodyComponent>(body);
            Assert.Multiple(() =>
            {
                Assert.That(bodyComponent.LinkedAi, Is.EqualTo(firstAi));
                Assert.That(entMan.GetComponent<StationAiBodyControllerComponent>(firstAi).CurrentBody, Is.EqualTo(body));
                Assert.That(entMan.GetComponent<StationAiBodyControllerComponent>(secondAi).CurrentBody, Is.Null);
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task ManualExitReturnsMindToStationAiBrainAndFreesBody()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.EntMan;

        EntityUid body = default;
        EntityUid aiBrain = default;
        EntityUid mindId = default;
        var entered = false;
        var exited = false;

        await server.WaitPost(() =>
        {
            body = SpawnPreparedBody(entMan);
            aiBrain = SpawnStationAiWithMind(entMan, "Astra", out mindId);

            var bodySystem = entMan.System<StationAiBodySystem>();
            entered = bodySystem.TryEnterBody(aiBrain, body);
            exited = bodySystem.TryExitBody(aiBrain);
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(entered, Is.True);
                Assert.That(exited, Is.True);
                Assert.That(entMan.System<SharedMindSystem>().TryGetMind(aiBrain, out var aiMindId, out _), Is.True);
                Assert.That(aiMindId, Is.EqualTo(mindId));
                Assert.That(entMan.System<SharedMindSystem>().TryGetMind(body, out _, out _), Is.False);
            });

            var bodyComponent = entMan.GetComponent<StationAiBodyComponent>(body);
            Assert.Multiple(() =>
            {
                Assert.That(bodyComponent.LinkedAi, Is.Null);
                Assert.That(entMan.GetComponent<StationAiBodyControllerComponent>(aiBrain).CurrentBody, Is.Null);
                Assert.That(entMan.GetComponent<AccessReaderComponent>(body).Enabled, Is.False);
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task DestroyingControlledBodyReturnsMindToStationAiBrainAndEjectsBoard()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.EntMan;

        EntityUid body = default;
        EntityUid board = default;
        EntityUid aiBrain = default;
        EntityUid mindId = default;
        var entered = false;

        await server.WaitPost(() =>
        {
            body = SpawnPreparedBody(entMan);
            board = entMan.GetComponent<StationAiBodyComponent>(body).Board!.Value;
            aiBrain = SpawnStationAiWithMind(entMan, "Astra", out mindId);

            entered = entMan.System<StationAiBodySystem>().TryEnterBody(aiBrain, body);
            entMan.DeleteEntity(body);
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(entered, Is.True);
                Assert.That(entMan.EntityExists(body), Is.False);
                Assert.That(entMan.EntityExists(board), Is.True);
                Assert.That(entMan.System<SharedMindSystem>().TryGetMind(aiBrain, out var aiMindId, out _), Is.True);
                Assert.That(aiMindId, Is.EqualTo(mindId));
            });

            Assert.Multiple(() =>
            {
                Assert.That(entMan.GetComponent<StationAiBodyControllerComponent>(aiBrain).CurrentBody, Is.Null);
                Assert.That(entMan.GetComponent<TransformComponent>(board).ParentUid, Is.Not.EqualTo(body));
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task RemovingCommunicationBoardFromControlledBodyReturnsMindAndClearsBody()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.EntMan;

        EntityUid body = default;
        EntityUid board = default;
        EntityUid aiBrain = default;
        EntityUid mindId = default;
        var entered = false;
        var removed = false;

        await server.WaitPost(() =>
        {
            body = SpawnPreparedBody(entMan);
            board = entMan.GetComponent<StationAiBodyComponent>(body).Board!.Value;
            aiBrain = SpawnStationAiWithMind(entMan, "Astra", out mindId);

            entered = entMan.System<StationAiBodySystem>().TryEnterBody(aiBrain, body);

            var borg = entMan.GetComponent<BorgChassisComponent>(body);
            removed = entMan.System<SharedContainerSystem>().Remove(board, borg.BrainContainer);
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(entered, Is.True);
                Assert.That(removed, Is.True);
                Assert.That(entMan.System<SharedMindSystem>().TryGetMind(aiBrain, out var aiMindId, out _), Is.True);
                Assert.That(aiMindId, Is.EqualTo(mindId));
                Assert.That(entMan.System<SharedMindSystem>().TryGetMind(body, out _, out _), Is.False);
                Assert.That(entMan.System<SharedMindSystem>().TryGetMind(board, out _, out _), Is.False);
            });

            Assert.Multiple(() =>
            {
                Assert.That(entMan.HasComponent<StationAiBodyComponent>(body), Is.False);
                Assert.That(entMan.GetComponent<StationAiBodyControllerComponent>(aiBrain).CurrentBody, Is.Null);
                Assert.That(entMan.GetComponent<AccessReaderComponent>(body).Enabled, Is.False);
                Assert.That(entMan.GetComponent<TransformComponent>(board).ParentUid, Is.Not.EqualTo(body));
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task BodyUiStateListsFreeOccupiedAndCurrentBodies()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.EntMan;

        EntityUid firstBody = default;
        EntityUid secondBody = default;
        EntityUid aiBrain = default;
        EntityUid mindId = default;
        var entered = false;

        await server.WaitPost(() =>
        {
            firstBody = SpawnPreparedBody(entMan);
            secondBody = SpawnPreparedBody(entMan);
            aiBrain = SpawnStationAiWithMind(entMan, "Astra", out mindId);

            entered = entMan.System<StationAiBodySystem>().TryEnterBody(aiBrain, firstBody);
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            var state = entMan.System<StationAiBodySystem>().GetBodyUiState(aiBrain);
            var firstNet = entMan.GetNetEntity(firstBody);
            var secondNet = entMan.GetNetEntity(secondBody);
            var firstEntry = FindBodyEntry(state, firstNet);
            var secondEntry = FindBodyEntry(state, secondNet);

            Assert.Multiple(() =>
            {
                Assert.That(entered, Is.True);
                Assert.That(state.CurrentBody, Is.EqualTo(firstNet));
                Assert.That(state.Bodies.Count, Is.EqualTo(2));
            });

            Assert.Multiple(() =>
            {
                Assert.That(firstEntry.Body, Is.EqualTo(firstNet));
                Assert.That(firstEntry.BodyNumber, Is.EqualTo(1));
                Assert.That(firstEntry.Name, Is.EqualTo(entMan.GetComponent<MetaDataComponent>(firstBody).EntityName));
                Assert.That(firstEntry.LinkedAi, Is.EqualTo(entMan.GetNetEntity(aiBrain)));
                Assert.That(firstEntry.IsCurrent, Is.True);
            });

            Assert.Multiple(() =>
            {
                Assert.That(secondEntry.Body, Is.EqualTo(secondNet));
                Assert.That(secondEntry.BodyNumber, Is.EqualTo(2));
                Assert.That(secondEntry.Name, Is.EqualTo(entMan.GetComponent<MetaDataComponent>(secondBody).EntityName));
                Assert.That(secondEntry.LinkedAi, Is.Null);
                Assert.That(secondEntry.IsCurrent, Is.False);
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task BodyUiMessagesEnterAndExitBody()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.EntMan;

        EntityUid body = default;
        EntityUid aiBrain = default;
        EntityUid mindId = default;

        await server.WaitPost(() =>
        {
            body = SpawnPreparedBody(entMan);
            aiBrain = SpawnStationAiWithMind(entMan, "Astra", out mindId);

            var enterMessage = new StationAiBodyEnterMessage(entMan.GetNetEntity(body))
            {
                Actor = aiBrain,
                Entity = entMan.GetNetEntity(aiBrain),
                UiKey = StationAiBodyUiKey.Key,
            };

            entMan.EventBus.RaiseLocalEvent(aiBrain, enterMessage);
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(entMan.HasComponent<StationAiBodyControllerComponent>(aiBrain), Is.True);
                Assert.That(entMan.System<SharedMindSystem>().TryGetMind(body, out var bodyMindId, out _), Is.True);
                Assert.That(bodyMindId, Is.EqualTo(mindId));
                Assert.That(entMan.GetComponent<StationAiBodyComponent>(body).LinkedAi, Is.EqualTo(aiBrain));
                Assert.That(entMan.GetComponent<StationAiBodyControllerComponent>(aiBrain).CurrentBody, Is.EqualTo(body));
            });
        });

        await server.WaitPost(() =>
        {
            var exitMessage = new StationAiBodyExitMessage
            {
                Actor = body,
                Entity = entMan.GetNetEntity(aiBrain),
                UiKey = StationAiBodyUiKey.Key,
            };

            entMan.EventBus.RaiseLocalEvent(aiBrain, exitMessage);
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(entMan.System<SharedMindSystem>().TryGetMind(aiBrain, out var aiMindId, out _), Is.True);
                Assert.That(aiMindId, Is.EqualTo(mindId));
                Assert.That(entMan.GetComponent<StationAiBodyComponent>(body).LinkedAi, Is.Null);
                Assert.That(entMan.GetComponent<StationAiBodyControllerComponent>(aiBrain).CurrentBody, Is.Null);
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task BodyUiStateRefreshesAfterControlledBodyExitAction()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.EntMan;

        EntityUid body = default;
        EntityUid aiBrain = default;
        EntityUid mindId = default;
        NetEntity bodyNet = default;
        var entered = false;
        var openedBodyUi = false;
        var hadCurrentBodyBeforeExit = false;
        var performedExitAction = false;

        await server.WaitPost(() =>
        {
            body = SpawnPreparedBody(entMan);
            aiBrain = SpawnStationAiWithMind(entMan, "Astra", out mindId);
            bodyNet = entMan.GetNetEntity(body);

            entered = entMan.System<StationAiBodySystem>().TryEnterBody(aiBrain, body);

            if (TryFindAction(entMan, body, "ActionStationAiBodyMenu", out var menuAction))
            {
                entMan.System<SharedActionsSystem>().PerformAction(
                    (body, entMan.GetComponent<ActionsComponent>(body)),
                    (menuAction, entMan.GetComponent<ActionComponent>(menuAction)),
                    predicted: false);
                openedBodyUi = true;
            }

            if (entMan.System<SharedUserInterfaceSystem>().TryGetUiState<StationAiBodyBuiState>(
                    aiBrain,
                    StationAiBodyUiKey.Key,
                    out var beforeExitState))
            {
                hadCurrentBodyBeforeExit = beforeExitState.CurrentBody == bodyNet;
            }

            if (TryFindAction(entMan, body, "ActionStationAiBodyExit", out var exitAction))
            {
                entMan.System<SharedActionsSystem>().PerformAction(
                    (body, entMan.GetComponent<ActionsComponent>(body)),
                    (exitAction, entMan.GetComponent<ActionComponent>(exitAction)),
                    predicted: false);
                performedExitAction = true;
            }
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(entered, Is.True);
                Assert.That(openedBodyUi, Is.True);
                Assert.That(hadCurrentBodyBeforeExit, Is.True);
                Assert.That(performedExitAction, Is.True);
                Assert.That(entMan.System<SharedMindSystem>().TryGetMind(aiBrain, out var aiMindId, out _), Is.True);
                Assert.That(aiMindId, Is.EqualTo(mindId));
                Assert.That(entMan.GetComponent<StationAiBodyControllerComponent>(aiBrain).CurrentBody, Is.Null);
            });

            Assert.That(
                entMan.System<SharedUserInterfaceSystem>().TryGetUiState<StationAiBodyBuiState>(
                    aiBrain,
                    StationAiBodyUiKey.Key,
                    out var afterExitState),
                Is.True);
            Assert.That(afterExitState!.CurrentBody, Is.Null);
            Assert.That(FindBodyEntry(afterExitState, bodyNet).IsCurrent, Is.False);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task ControlledBodyLawsFollowStationAiBrainLawset()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.EntMan;

        EntityUid body = default;
        EntityUid aiBrain = default;
        var entered = false;

        await server.WaitPost(() =>
        {
            body = SpawnPreparedBody(entMan);
            aiBrain = SpawnStationAiWithMind(entMan, "Astra", out _);

            entered = entMan.System<StationAiBodySystem>().TryEnterBody(aiBrain, body);
            entMan.System<SiliconLawSystem>().SetLaws(new List<SiliconLaw>
            {
                new()
                {
                    LawString = "station-ai-body-test-law",
                    Order = 1,
                },
            }, aiBrain);
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            var lawSystem = entMan.System<SiliconLawSystem>();
            var brainLaws = lawSystem.GetLaws(aiBrain).Laws;
            var bodyLaws = lawSystem.GetLaws(body).Laws;

            Assert.Multiple(() =>
            {
                Assert.That(entered, Is.True);
                Assert.That(bodyLaws.Count, Is.EqualTo(brainLaws.Count));
                Assert.That(bodyLaws[0].LawString, Is.EqualTo(brainLaws[0].LawString));
                Assert.That(bodyLaws[0].Order, Is.EqualTo(brainLaws[0].Order));
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task DownloadingControlledAiToIntellicardFreesBodyAndMovesMindToCard()
    {
        await using var pair = await PoolManager.GetServerClient();
        var testMap = await pair.CreateTestMap();
        var server = pair.Server;
        var entMan = server.EntMan;

        EntityUid body = default;
        EntityUid core = default;
        EntityUid intellicard = default;
        EntityUid aiBrain = default;
        EntityUid mindId = default;
        var entered = false;
        var removedFromCore = false;
        var insertedIntoCard = false;

        await server.WaitPost(() =>
        {
            body = SpawnPreparedBody(entMan, testMap.MapCoords);
            core = entMan.SpawnEntity("PlayerStationAiEmpty", testMap.MapCoords);
            intellicard = entMan.SpawnEntity("Intellicard", testMap.MapCoords);
            aiBrain = SpawnStationAiWithMind(entMan, "Astra", out mindId, testMap.MapCoords);

            var itemSlots = entMan.System<ItemSlotsSystem>();
            var coreHolder = entMan.GetComponent<StationAiHolderComponent>(core);
            var cardHolder = entMan.GetComponent<StationAiHolderComponent>(intellicard);

            Assert.That(itemSlots.TryInsert(core, coreHolder.Slot, aiBrain, null), Is.True);

            entered = entMan.System<StationAiBodySystem>().TryEnterBody(aiBrain, body);
            removedFromCore = itemSlots.TryEject(core, coreHolder.Slot, null, out var downloadedBrain);
            insertedIntoCard = downloadedBrain != null &&
                               itemSlots.TryInsert(intellicard, cardHolder.Slot, downloadedBrain.Value, null);
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(entered, Is.True);
                Assert.That(removedFromCore, Is.True);
                Assert.That(insertedIntoCard, Is.True);
                Assert.That(entMan.System<SharedMindSystem>().TryGetMind(aiBrain, out var aiMindId, out _), Is.True);
                Assert.That(aiMindId, Is.EqualTo(mindId));
                Assert.That(entMan.System<SharedMindSystem>().TryGetMind(body, out _, out _), Is.False);
            });

            var bodyComponent = entMan.GetComponent<StationAiBodyComponent>(body);
            Assert.Multiple(() =>
            {
                Assert.That(bodyComponent.LinkedAi, Is.Null);
                Assert.That(entMan.GetComponent<StationAiBodyControllerComponent>(aiBrain).CurrentBody, Is.Null);
                Assert.That(entMan.GetComponent<AccessReaderComponent>(body).Enabled, Is.False);
                Assert.That(entMan.GetComponent<StationAiHolderComponent>(intellicard).Slot.Item, Is.EqualTo(aiBrain));
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task IntellicardDownloadWarningTargetsControlledBody()
    {
        await using var pair = await PoolManager.GetServerClient();
        var testMap = await pair.CreateTestMap();
        var server = pair.Server;
        var entMan = server.EntMan;

        EntityUid body = default;
        EntityUid core = default;
        EntityUid intellicard = default;
        EntityUid user = default;
        EntityUid aiBrain = default;
        var entered = false;
        var handled = false;

        await server.WaitPost(() =>
        {
            body = SpawnPreparedBody(entMan, testMap.MapCoords);
            core = entMan.SpawnEntity("PlayerStationAiEmpty", testMap.MapCoords);
            intellicard = entMan.SpawnEntity("Intellicard", testMap.MapCoords);
            user = entMan.SpawnEntity("SunriseStationAiBodyTestUser", testMap.MapCoords);
            aiBrain = SpawnStationAiWithMind(entMan, "Astra", out _, testMap.MapCoords);

            entMan.EnsureComponent<TestListenerComponent>(body);
            entMan.EnsureComponent<TestListenerComponent>(aiBrain);

            var holder = entMan.GetComponent<StationAiHolderComponent>(core);
            Assert.That(entMan.System<ItemSlotsSystem>().TryInsert(core, holder.Slot, aiBrain, null), Is.True);

            entered = entMan.System<StationAiBodySystem>().TryEnterBody(aiBrain, body);

            var interaction = new AfterInteractEvent(
                user,
                intellicard,
                core,
                entMan.GetComponent<TransformComponent>(core).Coordinates,
                canReach: true);

            entMan.EventBus.RaiseLocalEvent(intellicard, interaction);
            handled = interaction.Handled;
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            var listener = entMan.System<StationAiBodyChatNotificationTestSystem>();

            Assert.Multiple(() =>
            {
                Assert.That(entered, Is.True);
                Assert.That(handled, Is.True);
                Assert.That(
                    listener.Count(body, ev => ev.ChatNotification == IntellicardDownloadNotification),
                    Is.EqualTo(1));
                Assert.That(
                    listener.Count(aiBrain, ev => ev.ChatNotification == IntellicardDownloadNotification),
                    Is.Zero);
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task CoreBatteryAlertTargetsControlledBody()
    {
        await using var pair = await PoolManager.GetServerClient();
        var testMap = await pair.CreateTestMap();
        var server = pair.Server;
        var entMan = server.EntMan;

        EntityUid body = default;
        EntityUid core = default;
        EntityUid aiBrain = default;
        var entered = false;

        await server.WaitPost(() =>
        {
            body = SpawnPreparedBody(entMan, testMap.MapCoords);
            core = entMan.SpawnEntity("PlayerStationAiEmpty", testMap.MapCoords);
            aiBrain = SpawnStationAiWithMind(entMan, "Astra", out _, testMap.MapCoords);

            var holder = entMan.GetComponent<StationAiHolderComponent>(core);
            Assert.That(entMan.System<ItemSlotsSystem>().TryInsert(core, holder.Slot, aiBrain, null), Is.True);

            entered = entMan.System<StationAiBodySystem>().TryEnterBody(aiBrain, body);

            var battery = entMan.GetComponent<BatteryComponent>(core);
            entMan.System<SharedBatterySystem>().SetCharge((core, battery), battery.MaxCharge / 2f);
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            var alerts = entMan.System<AlertsSystem>();

            Assert.Multiple(() =>
            {
                Assert.That(entered, Is.True);
                Assert.That(entMan.HasComponent<AlertsComponent>(body), Is.True);
                Assert.That(alerts.IsShowingAlert(body, AiBatteryAlert), Is.True);
                Assert.That(alerts.IsShowingAlert(aiBrain, AiBatteryAlert), Is.False);
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task CoreDamageAlertTargetsControlledBody()
    {
        await using var pair = await PoolManager.GetServerClient();
        var testMap = await pair.CreateTestMap();
        var server = pair.Server;
        var entMan = server.EntMan;

        EntityUid body = default;
        EntityUid core = default;
        EntityUid aiBrain = default;
        var entered = false;
        var changedDamage = false;

        await server.WaitPost(() =>
        {
            body = SpawnPreparedBody(entMan, testMap.MapCoords);
            core = entMan.SpawnEntity("PlayerStationAiEmpty", testMap.MapCoords);
            aiBrain = SpawnStationAiWithMind(entMan, "Astra", out _, testMap.MapCoords);

            var holder = entMan.GetComponent<StationAiHolderComponent>(core);
            Assert.That(entMan.System<ItemSlotsSystem>().TryInsert(core, holder.Slot, aiBrain, null), Is.True);

            entered = entMan.System<StationAiBodySystem>().TryEnterBody(aiBrain, body);
            changedDamage = entMan.System<DamageableSystem>().TryChangeDamage(
                core,
                new DamageSpecifier
                {
                    DamageDict =
                    {
                        ["Structural"] = 100,
                    },
                },
                ignoreResistances: true,
                ignoreVariance: true);
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            var alerts = entMan.System<AlertsSystem>();

            Assert.Multiple(() =>
            {
                Assert.That(entered, Is.True);
                Assert.That(changedDamage, Is.True);
                Assert.That(entMan.HasComponent<AlertsComponent>(body), Is.True);
                Assert.That(alerts.IsShowingAlert(body, BorgHealthAlert), Is.True);
                Assert.That(alerts.IsShowingAlert(aiBrain, BorgHealthAlert), Is.False);
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task IntellicardCannotDownloadStationAiDirectlyFromBody()
    {
        await using var pair = await PoolManager.GetServerClient();
        var testMap = await pair.CreateTestMap();
        var server = pair.Server;
        var entMan = server.EntMan;

        EntityUid body = default;
        EntityUid intellicard = default;
        EntityUid user = default;
        EntityUid aiBrain = default;
        EntityUid mindId = default;
        var entered = false;
        var pickedUpCard = false;

        await server.WaitPost(() =>
        {
            body = SpawnPreparedBody(entMan, testMap.MapCoords);
            intellicard = entMan.SpawnEntity("Intellicard", testMap.MapCoords);
            user = entMan.SpawnEntity("SunriseStationAiBodyTestUser", testMap.MapCoords);
            aiBrain = SpawnStationAiWithMind(entMan, "Astra", out mindId, testMap.MapCoords);

            entered = entMan.System<StationAiBodySystem>().TryEnterBody(aiBrain, body);
            pickedUpCard = entMan.System<HandsSystem>().TryPickupAnyHand(user, intellicard, checkActionBlocker: false, animate: false);

            var coordinates = entMan.GetComponent<TransformComponent>(body).Coordinates;
            entMan.System<SharedInteractionSystem>().UserInteraction(
                user,
                coordinates,
                body,
                checkCanInteract: false,
                checkAccess: false,
                checkCanUse: false);
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            var bodyComponent = entMan.GetComponent<StationAiBodyComponent>(body);
            var cardHolder = entMan.GetComponent<StationAiHolderComponent>(intellicard);
            var activeDoAfters = entMan.TryGetComponent<DoAfterComponent>(user, out var doAfter)
                ? doAfter.DoAfters.Values.Count(doAfterEntry => !doAfterEntry.Cancelled && !doAfterEntry.Completed)
                : 0;

            Assert.Multiple(() =>
            {
                Assert.That(entered, Is.True);
                Assert.That(pickedUpCard, Is.True);
                Assert.That(entMan.HasComponent<StationAiHolderComponent>(body), Is.False);
                Assert.That(cardHolder.Slot.Item, Is.Null);
                Assert.That(activeDoAfters, Is.EqualTo(0));
                Assert.That(bodyComponent.LinkedAi, Is.EqualTo(aiBrain));
                Assert.That(entMan.System<SharedMindSystem>().TryGetMind(body, out var bodyMindId, out _), Is.True);
                Assert.That(bodyMindId, Is.EqualTo(mindId));
                Assert.That(entMan.System<SharedMindSystem>().TryGetMind(aiBrain, out _, out _), Is.False);
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task CorePowerLossReturnsMindAndReleasesControlledBody()
    {
        await using var pair = await PoolManager.GetServerClient();
        var testMap = await pair.CreateTestMap();
        var server = pair.Server;
        var entMan = server.EntMan;

        EntityUid body = default;
        EntityUid core = default;
        EntityUid aiBrain = default;
        EntityUid mindId = default;
        var entered = false;

        await server.WaitPost(() =>
        {
            body = SpawnPreparedBody(entMan, testMap.MapCoords);
            core = entMan.SpawnEntity("PlayerStationAiEmpty", testMap.MapCoords);
            aiBrain = SpawnStationAiWithMind(entMan, "Astra", out mindId, testMap.MapCoords);

            var holder = entMan.GetComponent<StationAiHolderComponent>(core);
            Assert.That(entMan.System<ItemSlotsSystem>().TryInsert(core, holder.Slot, aiBrain, null), Is.True);

            entered = entMan.System<StationAiBodySystem>().TryEnterBody(aiBrain, body);

            var ev = new PowerChangedEvent(false, 0f);
            entMan.EventBus.RaiseLocalEvent(core, ref ev);
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            var bodyComponent = entMan.GetComponent<StationAiBodyComponent>(body);
            var controller = entMan.GetComponent<StationAiBodyControllerComponent>(aiBrain);

            Assert.Multiple(() =>
            {
                Assert.That(entered, Is.True);
                Assert.That(entMan.GetComponent<MobStateComponent>(aiBrain).CurrentState, Is.EqualTo(MobState.Dead));
                Assert.That(entMan.System<SharedMindSystem>().TryGetMind(aiBrain, out var aiMindId, out _), Is.True);
                Assert.That(aiMindId, Is.EqualTo(mindId));
                Assert.That(entMan.System<SharedMindSystem>().TryGetMind(body, out _, out _), Is.False);
                Assert.That(bodyComponent.LinkedAi, Is.Null);
                Assert.That(controller.CurrentBody, Is.Null);
                Assert.That(entMan.GetComponent<AccessReaderComponent>(body).Enabled, Is.False);
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task CoreBreakageReturnsMindAndReleasesControlledBody()
    {
        await using var pair = await PoolManager.GetServerClient();
        var testMap = await pair.CreateTestMap();
        var server = pair.Server;
        var entMan = server.EntMan;

        EntityUid body = default;
        EntityUid core = default;
        EntityUid aiBrain = default;
        EntityUid mindId = default;
        var entered = false;

        await server.WaitPost(() =>
        {
            body = SpawnPreparedBody(entMan, testMap.MapCoords);
            core = entMan.SpawnEntity("PlayerStationAiEmpty", testMap.MapCoords);
            aiBrain = SpawnStationAiWithMind(entMan, "Astra", out mindId, testMap.MapCoords);

            var holder = entMan.GetComponent<StationAiHolderComponent>(core);
            Assert.That(entMan.System<ItemSlotsSystem>().TryInsert(core, holder.Slot, aiBrain, null), Is.True);

            entered = entMan.System<StationAiBodySystem>().TryEnterBody(aiBrain, body);
            entMan.System<DestructibleSystem>().BreakEntity(core);
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            var bodyComponent = entMan.GetComponent<StationAiBodyComponent>(body);
            var controller = entMan.GetComponent<StationAiBodyControllerComponent>(aiBrain);

            Assert.Multiple(() =>
            {
                Assert.That(entered, Is.True);
                Assert.That(entMan.GetComponent<MobStateComponent>(aiBrain).CurrentState, Is.EqualTo(MobState.Dead));
                Assert.That(entMan.System<SharedMindSystem>().TryGetMind(aiBrain, out var aiMindId, out _), Is.True);
                Assert.That(aiMindId, Is.EqualTo(mindId));
                Assert.That(entMan.System<SharedMindSystem>().TryGetMind(body, out _, out _), Is.False);
                Assert.That(bodyComponent.LinkedAi, Is.Null);
                Assert.That(controller.CurrentBody, Is.Null);
                Assert.That(entMan.GetComponent<AccessReaderComponent>(body).Enabled, Is.False);
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task StationAiLookupReturnsControlledBodyAsActiveActor()
    {
        await using var pair = await PoolManager.GetServerClient();
        var testMap = await pair.CreateTestMap();
        var server = pair.Server;
        var entMan = server.EntMan;

        EntityUid body = default;
        EntityUid aiBrain = default;
        EntityUid mindId = default;
        var entered = false;

        await server.WaitPost(() =>
        {
            body = SpawnPreparedBody(entMan, testMap.MapCoords);
            aiBrain = SpawnStationAiCoreWithMind(entMan, "Astra", out mindId, testMap.MapCoords);

            entered = entMan.System<StationAiBodySystem>().TryEnterBody(aiBrain, body);
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            var stationAis = entMan.System<StationAiServerSystem>().GetStationAIs(testMap.Grid.Owner);

            Assert.Multiple(() =>
            {
                Assert.That(entered, Is.True);
                Assert.That(stationAis, Does.Contain(body));
                Assert.That(stationAis, Does.Not.Contain(aiBrain));
                Assert.That(entMan.System<SharedMindSystem>().TryGetMind(body, out var bodyMindId, out _), Is.True);
                Assert.That(bodyMindId, Is.EqualTo(mindId));
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task ControlledBodyUsesStationAiDoorRadialRules()
    {
        await using var pair = await PoolManager.GetServerClient();
        var testMap = await pair.CreateTestMap();
        var server = pair.Server;
        var entMan = server.EntMan;

        EntityUid body = default;
        EntityUid aiBrain = default;
        EntityUid airlock = default;
        var entered = false;
        BoundUserInterfaceMessageAttempt disabledAttempt = default!;
        var disabledAttemptRaised = false;

        await server.WaitPost(() =>
        {
            body = SpawnPreparedBody(entMan, testMap.MapCoords);
            aiBrain = SpawnStationAiCoreWithMind(entMan, "Astra", out _, testMap.MapCoords);
            airlock = entMan.SpawnEntity("SunriseStationAiBodyTestAirlock", testMap.MapCoords);

            entered = entMan.System<StationAiBodySystem>().TryEnterBody(aiBrain, body);

            var stationAi = entMan.System<StationAiServerSystem>();
            stationAi.SetWhitelistEnabled((airlock, entMan.GetComponent<StationAiWhitelistComponent>(airlock)), false);

            var blockedMessage = CreateDoorRadialMessage(entMan, body, airlock, new StationAiBoltEvent
            {
                Bolted = true,
            });

            disabledAttempt = new BoundUserInterfaceMessageAttempt(body, airlock, AiUi.Key, blockedMessage);
            disabledAttemptRaised = true;
            entMan.EventBus.RaiseLocalEvent(airlock, disabledAttempt);

            stationAi.SetWhitelistEnabled((airlock, entMan.GetComponent<StationAiWhitelistComponent>(airlock)), true);

            entMan.EventBus.RaiseEvent(EventSource.Local, CreateDoorRadialMessage(entMan, body, airlock, new StationAiBoltEvent
            {
                Bolted = true,
            }));
            entMan.EventBus.RaiseEvent(EventSource.Local, CreateDoorRadialMessage(entMan, body, airlock, new StationAiElectrifiedEvent
            {
                Electrified = true,
            }));
            entMan.EventBus.RaiseEvent(EventSource.Local, CreateDoorRadialMessage(entMan, body, airlock, new StationAiEmergencyAccessEvent
            {
                EmergencyAccess = true,
            }));
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(entered, Is.True);
                Assert.That(disabledAttemptRaised, Is.True);
                Assert.That(disabledAttempt.Cancelled, Is.True);
                Assert.That(entMan.GetComponent<DoorBoltComponent>(airlock).BoltsDown, Is.True);
                Assert.That(entMan.GetComponent<ElectrifiedComponent>(airlock).Enabled, Is.True);
                Assert.That(entMan.GetComponent<AirlockComponent>(airlock).EmergencyAccess, Is.True);
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task BodyAppearanceCustomizationDoesNotApplyOverlayToControlledBody()
    {
        await using var pair = await PoolManager.GetServerClient();
        var testMap = await pair.CreateTestMap();
        var server = pair.Server;
        var entMan = server.EntMan;

        EntityUid body = default;
        EntityUid core = default;
        EntityUid aiBrain = default;
        var entered = false;

        await server.WaitPost(() =>
        {
            body = SpawnPreparedBody(entMan, testMap.MapCoords);
            entMan.EnsureComponent<AppearanceComponent>(body);
            aiBrain = SpawnStationAiCoreWithMind(entMan, "Astra", out _, testMap.MapCoords);

            Assert.That(entMan.System<StationAiServerSystem>().TryGetCore(aiBrain, out var stationAiCore), Is.True);
            core = stationAiCore.Owner;

            entered = entMan.System<StationAiBodySystem>().TryEnterBody(aiBrain, body);

            var message = new StationAiCustomizationMessage(
                "SunriseStationAiBodyAppearance",
                "SunriseStationAiBodyFemale")
            {
                Actor = body,
                Entity = entMan.GetNetEntity(core),
                UiKey = StationAiCustomizationUiKey.Key,
            };

            entMan.EventBus.RaiseLocalEvent(core, message);
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            var appearance = entMan.GetComponent<AppearanceComponent>(body);
            var hasData = entMan.System<SharedAppearanceSystem>()
                .TryGetData<PrototypeLayerData>(body, StationAiBodyVisuals.BodyAppearance, out _, appearance);
            var customization = entMan.GetComponent<StationAiCustomizationComponent>(aiBrain);

            Assert.Multiple(() =>
            {
                Assert.That(entered, Is.True);
                Assert.That(customization.ProtoIds["SunriseStationAiBodyAppearance"].Id, Is.EqualTo("SunriseStationAiBodyFemale"));
                Assert.That(hasData, Is.False);
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task SelectingBodyChassisAppliesModulesWithoutResettingBodyGender()
    {
        await using var pair = await PoolManager.GetServerClient();
        var testMap = await pair.CreateTestMap();
        var server = pair.Server;
        var entMan = server.EntMan;
        var engineeringType = new ProtoId<BorgTypePrototype>("engineering");

        EntityUid body = default;
        EntityUid aiBrain = default;
        EntityUid genericModule = default;
        EntityUid miningModule = default;
        var entered = false;
        var selectedType = false;

        await server.WaitPost(() =>
        {
            body = SpawnPreparedBody(entMan, testMap.MapCoords);
            aiBrain = SpawnStationAiWithMind(entMan, "Astra", out _, testMap.MapCoords);

            Assert.That(entMan.System<SharedBorgGenderSystem>().TrySetGender(
                (body, entMan.GetComponent<BorgGenderComponent>(body)),
                BorgGender.Female), Is.True);

            entered = entMan.System<StationAiBodySystem>().TryEnterBody(aiBrain, body);
            selectedType = entMan.System<StationAiBodySystem>().TrySelectBodyType(aiBrain, engineeringType);
            genericModule = entMan.SpawnEntity("BorgModuleInflatable", testMap.MapCoords);
            miningModule = entMan.SpawnEntity("BorgModuleMining", testMap.MapCoords);
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            var prototype = server.ProtoMan.Index(engineeringType);
            var chassis = entMan.GetComponent<BorgChassisComponent>(body);
            var switchable = entMan.GetComponent<BorgSwitchableTypeComponent>(body);
            var borgSystem = entMan.System<BorgSystem>();
            var installedModulePrototypes = chassis.ModuleContainer.ContainedEntities
                .Select(module => entMan.GetComponent<MetaDataComponent>(module).EntityPrototype?.ID)
                .ToArray();
            var expectedDefaultModules = prototype.DefaultModules
                .Select(module => module.ToString())
                .ToArray();
            var gender = entMan.GetComponent<BorgGenderComponent>(body);

            Assert.Multiple(() =>
            {
                Assert.That(entered, Is.True);
                Assert.That(selectedType, Is.True);
                Assert.That(switchable.SelectedBorgType, Is.EqualTo(engineeringType));
                Assert.That(gender.SelectedGender, Is.EqualTo(BorgGender.Female));
                Assert.That(TryFindAction(entMan, body, "ActionChangeBorgGender", out _), Is.True);
                Assert.That(chassis.MaxModules, Is.EqualTo(prototype.ExtraModuleCount + prototype.DefaultModules.Length));
                Assert.That(chassis.ModuleCount, Is.EqualTo(prototype.DefaultModules.Length));
                Assert.That(installedModulePrototypes, Is.EquivalentTo(expectedDefaultModules));
                Assert.That(
                    chassis.ModuleContainer.ContainedEntities.Select(module => entMan.GetComponent<BorgModuleComponent>(module).DefaultModule),
                    Is.All.True);
                Assert.That(
                    borgSystem.CanInsertModule(
                        (body, chassis),
                        (genericModule, entMan.GetComponent<BorgModuleComponent>(genericModule))),
                    Is.True);
                Assert.That(
                    borgSystem.CanInsertModule(
                        (body, chassis),
                        (miningModule, entMan.GetComponent<BorgModuleComponent>(miningModule))),
                    Is.False);
                Assert.That(entMan.GetComponent<IntrinsicRadioTransmitterComponent>(body).Channels, Is.EquivalentTo(StationAiRadioChannels));
                Assert.That(entMan.GetComponent<ActiveRadioComponent>(body).Channels, Is.EquivalentTo(StationAiRadioChannels));
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task ExitingAiBodyDoesNotResetBodyGender()
    {
        await using var pair = await PoolManager.GetServerClient();
        var testMap = await pair.CreateTestMap();
        var server = pair.Server;
        var entMan = server.EntMan;

        EntityUid body = default;
        EntityUid aiBrain = default;
        var entered = false;
        var exited = false;

        await server.WaitPost(() =>
        {
            body = SpawnPreparedBody(entMan, testMap.MapCoords);
            aiBrain = SpawnStationAiWithMind(entMan, "Astra", out _, testMap.MapCoords);

            Assert.That(entMan.System<SharedBorgGenderSystem>().TrySetGender(
                (body, entMan.GetComponent<BorgGenderComponent>(body)),
                BorgGender.Female), Is.True);

            entered = entMan.System<StationAiBodySystem>().TryEnterBody(aiBrain, body);
            exited = entMan.System<StationAiBodySystem>().TryExitBody(aiBrain);
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            var gender = entMan.GetComponent<BorgGenderComponent>(body);

            Assert.Multiple(() =>
            {
                Assert.That(entered, Is.True);
                Assert.That(exited, Is.True);
                Assert.That(gender.SelectedGender, Is.EqualTo(BorgGender.Female));
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task ControlledBodyGetsBodyActionsAndExitActionReturnsMind()
    {
        await using var pair = await PoolManager.GetServerClient();
        var testMap = await pair.CreateTestMap();
        var server = pair.Server;
        var entMan = server.EntMan;

        EntityUid body = default;
        EntityUid aiBrain = default;
        EntityUid mindId = default;
        var entered = false;
        var hasMenuAction = false;
        var hasExitAction = false;
        var performedExitAction = false;

        await server.WaitPost(() =>
        {
            body = SpawnPreparedBody(entMan, testMap.MapCoords);
            aiBrain = SpawnStationAiWithMind(entMan, "Astra", out mindId, testMap.MapCoords);

            entered = entMan.System<StationAiBodySystem>().TryEnterBody(aiBrain, body);
            hasMenuAction = TryFindAction(entMan, body, "ActionStationAiBodyMenu", out _);
            hasExitAction = TryFindAction(entMan, body, "ActionStationAiBodyExit", out var exitAction);

            if (hasExitAction)
            {
                entMan.System<SharedActionsSystem>().PerformAction(
                    (body, entMan.GetComponent<ActionsComponent>(body)),
                    (exitAction, entMan.GetComponent<ActionComponent>(exitAction)),
                    predicted: false);
                performedExitAction = true;
            }
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(entered, Is.True);
                Assert.That(hasMenuAction, Is.True);
                Assert.That(hasExitAction, Is.True);
                Assert.That(performedExitAction, Is.True);
                Assert.That(entMan.System<SharedMindSystem>().TryGetMind(aiBrain, out var aiMindId, out _), Is.True);
                Assert.That(aiMindId, Is.EqualTo(mindId));
                Assert.That(entMan.System<SharedMindSystem>().TryGetMind(body, out _, out _), Is.False);
                Assert.That(entMan.GetComponent<StationAiBodyComponent>(body).LinkedAi, Is.Null);
                Assert.That(entMan.GetComponent<StationAiBodyControllerComponent>(aiBrain).CurrentBody, Is.Null);
                Assert.That(TryFindAction(entMan, body, "ActionStationAiBodyMenu", out _), Is.False);
                Assert.That(TryFindAction(entMan, body, "ActionStationAiBodyExit", out _), Is.False);
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task ReturningFromSelectedDistantBodyRestoresStationAiEyeRelay()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = true,
        });
        var testMap = await pair.CreateTestMap();
        var server = pair.Server;
        var client = pair.Client;
        var entMan = server.EntMan;
        var coreCoords = new MapCoordinates(Vector2.Zero, testMap.MapId);
        var bodyCoords = new MapCoordinates(new Vector2(128f, 0f), testMap.MapId);
        var clownType = new ProtoId<BorgTypePrototype>("clown");

        EntityUid body = default;
        EntityUid aiBrain = default;
        EntityUid mindId = default;
        EntityUid remoteEye = default;
        NetEntity aiBrainNet = default;
        NetEntity remoteEyeNet = default;
        var entered = false;
        var selectedType = false;
        var performedExitAction = false;

        await server.WaitPost(() =>
        {
            body = SpawnPreparedBody(entMan, bodyCoords);
            entMan.RemoveComponent<InventoryComponent>(body);
            aiBrain = SpawnStationAiCoreWithMind(entMan, "Astra", out mindId, coreCoords);
            aiBrainNet = entMan.GetNetEntity(aiBrain);

            var session = server.ResolveDependency<IPlayerManager>().Sessions.Single();
            entMan.System<SharedMindSystem>().SetUserId(mindId, session.UserId);

            Assert.That(entMan.System<StationAiServerSystem>().TryGetCore(aiBrain, out var core), Is.True);
            Assert.That(core.Comp!.RemoteEntity, Is.Not.Null);
            remoteEye = core.Comp.RemoteEntity!.Value;
            remoteEyeNet = entMan.GetNetEntity(remoteEye);

            entered = entMan.System<StationAiBodySystem>().TryEnterBody(aiBrain, body);
            selectedType = entMan.System<StationAiBodySystem>().TrySelectBodyType(aiBrain, clownType);

            if (TryFindAction(entMan, body, "ActionStationAiBodyExit", out var exitAction))
            {
                entMan.System<SharedActionsSystem>().PerformAction(
                    (body, entMan.GetComponent<ActionsComponent>(body)),
                    (exitAction, entMan.GetComponent<ActionComponent>(exitAction)),
                    predicted: false);
                performedExitAction = true;
            }
        });

        await pair.RunTicksSync(20);
        await pair.SyncTicks(targetDelta: 1);

        await server.WaitAssertion(() =>
        {
            var session = server.ResolveDependency<IPlayerManager>().Sessions.Single();
            var eye = entMan.GetComponent<EyeComponent>(aiBrain);
            var relay = entMan.GetComponent<RelayInputMoverComponent>(aiBrain);

            Assert.Multiple(() =>
            {
                Assert.That(entered, Is.True);
                Assert.That(selectedType, Is.True);
                Assert.That(performedExitAction, Is.True);
                Assert.That(entMan.System<SharedMindSystem>().TryGetMind(aiBrain, out var aiMindId, out _), Is.True);
                Assert.That(aiMindId, Is.EqualTo(mindId));
                Assert.That(session.AttachedEntity, Is.EqualTo(aiBrain));
                Assert.That(eye.Target, Is.EqualTo(remoteEye));
                Assert.That(eye.DrawFov, Is.False);
                Assert.That(relay.RelayEntity, Is.EqualTo(remoteEye));
            });
        });

        await client.WaitAssertion(() =>
        {
            var clientAiBrain = client.EntMan.GetEntity(aiBrainNet);
            var clientRemoteEye = client.EntMan.GetEntity(remoteEyeNet);
            var eye = client.EntMan.GetComponent<EyeComponent>(clientAiBrain);
            var relay = client.EntMan.GetComponent<RelayInputMoverComponent>(clientAiBrain);

            Assert.Multiple(() =>
            {
                Assert.That(client.ResolveDependency<Robust.Client.Player.IPlayerManager>().LocalEntity, Is.EqualTo(clientAiBrain));
                Assert.That(client.EntMan.EntityExists(clientRemoteEye), Is.True);
                Assert.That(eye.Target, Is.EqualTo(clientRemoteEye));
                Assert.That(eye.DrawFov, Is.False);
                Assert.That(relay.RelayEntity, Is.EqualTo(clientRemoteEye));
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task HolopadAiRequestOpensOnControlledBody()
    {
        await using var pair = await PoolManager.GetServerClient();
        var testMap = await pair.CreateTestMap();
        var server = pair.Server;
        var entMan = server.EntMan;

        EntityUid body = default;
        EntityUid aiBrain = default;
        EntityUid core = default;
        EntityUid holopad = default;
        EntityUid requester = default;
        var entered = false;

        await server.WaitPost(() =>
        {
            body = SpawnPreparedBody(entMan, testMap.MapCoords);
            aiBrain = SpawnStationAiCoreWithMind(entMan, "Astra", out _, testMap.MapCoords);

            Assert.That(entMan.System<StationAiServerSystem>().TryGetCore(aiBrain, out var stationAiCore), Is.True);
            core = stationAiCore.Owner;
            entMan.RemoveComponent<ApcPowerReceiverComponent>(core);
            entMan.EnsureComponent<TestListenerComponent>(core);

            holopad = entMan.SpawnEntity("SunriseStationAiBodyTestHolopad", testMap.MapCoords);
            requester = entMan.SpawnEntity("SunriseStationAiBodyTestUser", testMap.MapCoords);

            entered = entMan.System<StationAiBodySystem>().TryEnterBody(aiBrain, body);

            var requestMessage = new HolopadStationAiRequestMessage
            {
                Actor = requester,
                Entity = entMan.GetNetEntity(holopad),
                UiKey = HolopadUiKey.InteractionWindow,
            };

            entMan.EventBus.RaiseLocalEvent(holopad, requestMessage);
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            var listener = entMan.System<StationAiBodyUiOpenedTestSystem>();

            Assert.Multiple(() =>
            {
                Assert.That(entered, Is.True);
                Assert.That(entMan.GetComponent<TelephoneComponent>(core).CurrentState, Is.EqualTo(TelephoneState.Ringing));
                Assert.That(
                    listener.Count(core, ev => ev.UiKey.Equals(HolopadUiKey.AiRequestWindow) && ev.Actor == body),
                    Is.EqualTo(1));
                Assert.That(
                    listener.Count(core, ev => ev.UiKey.Equals(HolopadUiKey.AiRequestWindow) && ev.Actor == aiBrain),
                    Is.EqualTo(0));
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task AnsweringHolopadRequestFromControlledBodyExitsBody()
    {
        await using var pair = await PoolManager.GetServerClient();
        var testMap = await pair.CreateTestMap();
        var server = pair.Server;
        var entMan = server.EntMan;

        EntityUid body = default;
        EntityUid aiBrain = default;
        EntityUid mindId = default;
        EntityUid core = default;
        EntityUid holopad = default;
        EntityUid requester = default;
        var entered = false;

        await server.WaitPost(() =>
        {
            body = SpawnPreparedBody(entMan, testMap.MapCoords);
            aiBrain = SpawnStationAiCoreWithMind(entMan, "Astra", out mindId, testMap.MapCoords);

            Assert.That(entMan.System<StationAiServerSystem>().TryGetCore(aiBrain, out var stationAiCore), Is.True);
            core = stationAiCore.Owner;
            entMan.RemoveComponent<ApcPowerReceiverComponent>(core);

            holopad = entMan.SpawnEntity("SunriseStationAiBodyTestHolopad", testMap.MapCoords);
            requester = entMan.SpawnEntity("SunriseStationAiBodyTestUser", testMap.MapCoords);

            entered = entMan.System<StationAiBodySystem>().TryEnterBody(aiBrain, body);

            var requestMessage = new HolopadStationAiRequestMessage
            {
                Actor = requester,
                Entity = entMan.GetNetEntity(holopad),
                UiKey = HolopadUiKey.InteractionWindow,
            };

            entMan.EventBus.RaiseLocalEvent(holopad, requestMessage);

            var answerMessage = new HolopadAnswerCallMessage
            {
                Actor = body,
                Entity = entMan.GetNetEntity(core),
                UiKey = HolopadUiKey.AiRequestWindow,
            };

            entMan.EventBus.RaiseLocalEvent(core, answerMessage);
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            var bodyComponent = entMan.GetComponent<StationAiBodyComponent>(body);
            var controller = entMan.GetComponent<StationAiBodyControllerComponent>(aiBrain);
            var coreHolopad = entMan.GetComponent<HolopadComponent>(core);

            Assert.Multiple(() =>
            {
                Assert.That(entered, Is.True);
                Assert.That(entMan.System<SharedMindSystem>().TryGetMind(aiBrain, out var aiMindId, out _), Is.True);
                Assert.That(aiMindId, Is.EqualTo(mindId));
                Assert.That(entMan.System<SharedMindSystem>().TryGetMind(body, out _, out _), Is.False);
                Assert.That(bodyComponent.LinkedAi, Is.Null);
                Assert.That(controller.CurrentBody, Is.Null);
                Assert.That(coreHolopad.User?.Owner, Is.EqualTo(aiBrain));
                Assert.That(entMan.GetComponent<TelephoneComponent>(core).CurrentState, Is.EqualTo(TelephoneState.InCall));
                Assert.That(entMan.GetComponent<TelephoneComponent>(holopad).CurrentState, Is.EqualTo(TelephoneState.InCall));
            });
        });

        await pair.CleanReturnAsync();
    }

    private static StationAiBodyBuiEntry FindBodyEntry(StationAiBodyBuiState state, NetEntity body)
    {
        foreach (var entry in state.Bodies)
        {
            if (entry.Body == body)
                return entry;
        }

        Assert.Fail($"No AI body BUI entry for {body}.");
        return default!;
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

    private static readonly ProtoId<ChatNotificationPrototype> IntellicardDownloadNotification = "IntellicardDownload";

    private static readonly ProtoId<AlertPrototype> AiBatteryAlert = "AiBattery";

    private static readonly ProtoId<AlertPrototype> BorgHealthAlert = "BorgHealth";

    private static readonly HashSet<ProtoId<RadioChannelPrototype>> StationAiRadioChannels =
    [
        "Binary",
        "Common",
        "Command",
        "Engineering",
        "Law",
        "Medical",
        "Science",
        "Security",
        "Service",
        "Supply",
    ];

    private static EntityUid SpawnPreparedBody(IEntityManager entMan, MapCoordinates? coordinates = null)
    {
        var spawnCoordinates = coordinates ?? MapCoordinates.Nullspace;
        var chassis = entMan.SpawnEntity("BorgChassisSelectable", spawnCoordinates);
        var board = entMan.SpawnEntity("SunriseStationAiCommunicationBoard", spawnCoordinates);
        var borg = entMan.GetComponent<BorgChassisComponent>(chassis);
        entMan.System<SharedContainerSystem>().Insert(board, borg.BrainContainer);
        return chassis;
    }

    private static EntityUid SpawnStationAiWithMind(
        IEntityManager entMan,
        string name,
        out EntityUid mindId,
        MapCoordinates? coordinates = null)
    {
        var aiBrain = entMan.SpawnEntity("StationAiBrain", coordinates ?? MapCoordinates.Nullspace);
        entMan.System<MetaDataSystem>().SetEntityName(aiBrain, name);

        var mind = entMan.System<SharedMindSystem>().CreateMind(null, name);
        mindId = mind.Owner;
        entMan.System<SharedMindSystem>().TransferTo(mindId, aiBrain, mind: mind.Comp);

        return aiBrain;
    }

    private static EntityUid SpawnStationAiCoreWithMind(
        IEntityManager entMan,
        string name,
        out EntityUid mindId,
        MapCoordinates coordinates)
    {
        var aiBrain = SpawnStationAiWithMind(entMan, name, out mindId, coordinates);
        var core = entMan.SpawnEntity("PlayerStationAiEmpty", coordinates);
        var holder = entMan.GetComponent<StationAiHolderComponent>(core);

        Assert.That(entMan.System<ItemSlotsSystem>().TryInsert(core, holder.Slot, aiBrain, null), Is.True);
        return aiBrain;
    }

    private static StationAiRadialMessage CreateDoorRadialMessage(
        IEntityManager entMan,
        EntityUid actor,
        EntityUid target,
        BaseStationAiAction action)
    {
        return new StationAiRadialMessage
        {
            Actor = actor,
            Entity = entMan.GetNetEntity(target),
            UiKey = AiUi.Key,
            Event = action,
        };
    }
}

public sealed class StationAiBodyChatNotificationTestSystem : TestListenerSystem<ChatNotificationEvent>;

public sealed class StationAiBodyUiOpenedTestSystem : TestListenerSystem<BoundUIOpenedEvent>;

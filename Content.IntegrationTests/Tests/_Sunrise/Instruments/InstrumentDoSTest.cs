using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Content.IntegrationTests.Pair;
using Content.Server.Instruments;
using Content.Shared.CCVar;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Instruments;
using Content.Shared.UserInterface;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Audio.Midi;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Sunrise.Instruments;

[TestFixture]
public sealed class InstrumentDoSTest
{
    private const string InstrumentPrototype = "SynthesizerInstrument";
    private const string NoLimitInstrumentPrototype = "SuperSynthesizerNoLimitInstrument";
    private const string PlayerPrototype = "MobHuman";

    [Test]
    public async Task ValidBurstCountsRealEventsAndDoesNotEchoToSender()
    {
        await using var pair = await GetInstrumentPair();
        var instrument = await SpawnOwnedInstrument(pair);
        var probe = pair.Client.System<InstrumentNetworkProbeSystem>();
        var serverInstrument = pair.Server.EntMan.GetComponent<InstrumentComponent>(instrument);
        var netInstrument = pair.Server.EntMan.GetNetEntity(instrument);

        await ConfigureMidiGuardrails(pair, eventsPerSecond: 3, eventsPerBatch: 8);
        await pair.Client.WaitPost(() => probe.Reset());

        // Old exploit: send multiple small packets in one second.
        // Vanilla counted packets, so 2x two-note bursts looked like "2" instead of "4".
        await pair.Client.WaitPost(() =>
        {
            probe.SendMidi(netInstrument, MakeValidBatch(2, tickStart: 1));
            probe.SendMidi(netInstrument, MakeValidBatch(2, tickStart: 3));
        });

        await pair.RunTicksSync(5);

        await pair.Server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(serverInstrument.MidiEventCount, Is.EqualTo(4));
                Assert.That(serverInstrument.BatchesDropped, Is.EqualTo(1));
                Assert.That(serverInstrument.LastSequencerTick, Is.EqualTo(4));
            });
        });

        await pair.Client.WaitAssertion(() =>
        {
            Assert.That(probe.MidiEventsReceived, Is.Empty);
            Assert.That(probe.StopEventsReceived, Is.Empty);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task CrossInstrumentSpamUsesAggregateSessionBudget()
    {
        await using var pair = await GetInstrumentPair();
        var firstInstrument = await SpawnOwnedInstrument(pair);
        var secondInstrument = await SpawnOwnedInstrument(pair);
        var probe = pair.Client.System<InstrumentNetworkProbeSystem>();
        var first = pair.Server.EntMan.GetComponent<InstrumentComponent>(firstInstrument);
        var second = pair.Server.EntMan.GetComponent<InstrumentComponent>(secondInstrument);
        var firstNet = pair.Server.EntMan.GetNetEntity(firstInstrument);
        var secondNet = pair.Server.EntMan.GetNetEntity(secondInstrument);

        await ConfigureMidiGuardrails(pair, eventsPerSecond: 3, eventsPerBatch: 8);
        await pair.Client.WaitPost(() => probe.Reset());

        // Old exploit: split spam across several instruments owned by the same session.
        await pair.Client.WaitPost(() =>
        {
            probe.SendMidi(firstNet, MakeValidBatch(2, tickStart: 1));
            probe.SendMidi(secondNet, MakeValidBatch(2, tickStart: 3));
        });

        await pair.RunTicksSync(5);

        await pair.Server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(first.MidiEventCount, Is.EqualTo(2));
                Assert.That(first.BatchesDropped, Is.EqualTo(0));
                Assert.That(second.MidiEventCount, Is.EqualTo(2));
                Assert.That(second.BatchesDropped, Is.EqualTo(1));
            });
        });

        await pair.Client.WaitAssertion(() =>
        {
            Assert.That(probe.MidiEventsReceived, Is.Empty);
            Assert.That(probe.StopEventsReceived, Is.Empty);
        });

        await pair.CleanReturnAsync();
    }

    [TestCase(RejectedBatchScenario.Empty)]
    [TestCase(RejectedBatchScenario.Oversized)]
    [TestCase(RejectedBatchScenario.InvalidPayload)]
    public async Task EarlyRejectedBatchesDoNotAdvanceStateOrForward(RejectedBatchScenario scenario)
    {
        await using var pair = await GetInstrumentPair();
        var instrument = await SpawnOwnedInstrument(pair);
        var probe = pair.Client.System<InstrumentNetworkProbeSystem>();
        var serverInstrument = pair.Server.EntMan.GetComponent<InstrumentComponent>(instrument);
        var netInstrument = pair.Server.EntMan.GetNetEntity(instrument);

        await ConfigureMidiGuardrails(pair, eventsPerSecond: 8, eventsPerBatch: 3);
        await pair.Client.WaitPost(() => probe.Reset());

        await pair.Client.WaitPost(() => probe.SendMidi(netInstrument, CreateRejectedBatch(scenario)));
        await pair.RunTicksSync(5);

        await pair.Server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(serverInstrument.BatchesDropped, Is.EqualTo(1));
                Assert.That(serverInstrument.MidiEventCount, Is.EqualTo(0));
                Assert.That(serverInstrument.LastSequencerTick, Is.EqualTo(0));
            });
        });

        await pair.Client.WaitAssertion(() =>
        {
            Assert.That(probe.MidiEventsReceived, Is.Empty);
            Assert.That(probe.StopEventsReceived, Is.Empty);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task InvalidFilteredChannelIsIgnored()
    {
        await using var pair = await GetInstrumentPair();
        var instrument = await SpawnOwnedInstrument(pair);
        var probe = pair.Client.System<InstrumentNetworkProbeSystem>();
        var serverInstrument = pair.Server.EntMan.GetComponent<InstrumentComponent>(instrument);
        var netInstrument = pair.Server.EntMan.GetNetEntity(instrument);

        await ConfigureMidiGuardrails(pair, eventsPerSecond: 8, eventsPerBatch: 8);
        await pair.Client.WaitPost(() => probe.Reset());

        await pair.Client.WaitPost(() => probe.SendFilteredChannel(netInstrument, -1, false));
        await pair.RunTicksSync(5);

        await pair.Server.WaitAssertion(() =>
        {
            Assert.That(Enumerable.Range(0, RobustMidiEvent.MaxChannels).All(i => serverInstrument.FilteredChannels[i]), Is.True);
        });

        await pair.Client.WaitAssertion(() =>
        {
            Assert.That(probe.MidiEventsReceived, Is.Empty);
            Assert.That(probe.StopEventsReceived, Is.Empty);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task StopStillResetsAndStopsClientPlayback()
    {
        await using var pair = await GetInstrumentPair();
        var instrument = await SpawnOwnedInstrument(pair);
        var probe = pair.Client.System<InstrumentNetworkProbeSystem>();
        var serverInstrument = pair.Server.EntMan.GetComponent<InstrumentComponent>(instrument);
        var netInstrument = pair.Server.EntMan.GetNetEntity(instrument);

        await ConfigureMidiGuardrails(pair, eventsPerSecond: 8, eventsPerBatch: 8);
        await pair.Client.WaitPost(() => probe.Reset());

        await pair.Client.WaitPost(() => probe.SendStop(netInstrument));
        await pair.RunTicksSync(10);

        await pair.Server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(serverInstrument.Playing, Is.False);
                Assert.That(serverInstrument.Master, Is.Null);
                Assert.That(Enumerable.Range(0, RobustMidiEvent.MaxChannels).All(i => serverInstrument.FilteredChannels[i] == false), Is.True);
            });
        });

        await pair.Client.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(probe.StopEventsReceived, Has.Count.EqualTo(1));
                Assert.That(probe.MidiEventsReceived, Has.Count.EqualTo(1));
                Assert.That(probe.MidiEventsReceived[0].MidiEvent, Is.EqualTo(new[] { RobustMidiEvent.SystemReset(0) }));
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task NoLimitInstrumentStillUsesHardNetworkGuard()
    {
        await using var pair = await GetInstrumentPair();
        var instrument = await SpawnOwnedInstrument(pair, prototype: NoLimitInstrumentPrototype);
        var probe = pair.Client.System<InstrumentNetworkProbeSystem>();
        var serverInstrument = pair.Server.EntMan.GetComponent<InstrumentComponent>(instrument);
        var netInstrument = pair.Server.EntMan.GetNetEntity(instrument);

        await ConfigureMidiGuardrails(pair, eventsPerSecond: 3, eventsPerBatch: 8, maxDropped: 1);
        await pair.Client.WaitPost(() => probe.Reset());

        await pair.Client.WaitPost(() =>
        {
            probe.SendMidi(netInstrument, MakeValidBatch(2, tickStart: 1));
            probe.SendMidi(netInstrument, MakeValidBatch(2, tickStart: 3));
        });

        await pair.RunTicksSync(5);

        await pair.Server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(serverInstrument.RespectMidiLimits, Is.False);
                Assert.That(serverInstrument.Playing, Is.True);
                Assert.That(serverInstrument.BatchesDropped, Is.EqualTo(1));
                Assert.That(serverInstrument.MidiEventCount, Is.EqualTo(4));
            });
        });

        await pair.Client.WaitAssertion(() =>
        {
            Assert.That(probe.MidiEventsReceived, Is.Empty);
            Assert.That(probe.StopEventsReceived, Is.Empty);
        });

        await pair.CleanReturnAsync();
    }

    private static RobustMidiEvent[] MakeValidBatch(int count, byte channel = 0, uint tickStart = 1)
    {
        return Enumerable.Range(0, count)
            .Select(i => RobustMidiEvent.NoteOn(channel, (byte) (60 + i), 100, tickStart + (uint) i))
            .ToArray();
    }

    private static RobustMidiEvent[] CreateRejectedBatch(RejectedBatchScenario scenario)
    {
        return scenario switch
        {
            RejectedBatchScenario.Empty => [],
            RejectedBatchScenario.Oversized => MakeValidBatch(4),
            RejectedBatchScenario.InvalidPayload => [new RobustMidiEvent(0x70, 0x00, 0x00, 1)],
            _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null)
        };
    }

    private static async Task ConfigureMidiGuardrails(
        TestPair pair,
        int eventsPerSecond,
        int eventsPerBatch,
        int maxDropped = 100,
        int maxLagged = 100)
    {
        await pair.Server.WaitPost(() =>
        {
            pair.Server.CfgMan.SetCVar(CVars.NetPVS, true);
            pair.Server.CfgMan.SetCVar(CCVars.MaxMidiEventsPerSecond, eventsPerSecond);
            pair.Server.CfgMan.SetCVar(CCVars.MaxMidiEventsPerBatch, eventsPerBatch);
            pair.Server.CfgMan.SetCVar(CCVars.MaxMidiBatchesDropped, maxDropped);
            pair.Server.CfgMan.SetCVar(CCVars.MaxMidiLaggedBatches, maxLagged);
        });
    }

    private static Task<TestPair> GetInstrumentPair()
    {
        return PoolManager.GetServerClient(new PoolSettings
        {
            Connected = true,
            Dirty = true,
            Fresh = true,
        });
    }

    private static async Task<EntityUid> SpawnOwnedInstrument(
        TestPair pair,
        string prototype = InstrumentPrototype,
        bool playing = true)
    {
        EntityUid instrument = default;
        var player = await EnsurePlayerAttached(pair);

        await pair.Server.WaitAssertion(() =>
        {
            var entityManager = pair.Server.EntMan;
            var coordinates = entityManager.GetComponent<TransformComponent>(player).Coordinates;
            instrument = entityManager.SpawnEntity(prototype, coordinates);

            var hands = entityManager.GetComponent<HandsComponent>(player);
            var handsSystem = pair.Server.System<SharedHandsSystem>();
            Assert.That(handsSystem.TryPickupAnyHand(player, instrument, checkActionBlocker: false, handsComp: hands), Is.True);

            entityManager.EnsureComponent<ActiveInstrumentComponent>(instrument);
            pair.Server.System<ActivatableUISystem>().SetCurrentSingleUser(instrument, player);
        });

        await pair.RunTicksSync(5);

        await pair.Client.WaitAssertion(() =>
        {
            Assert.That(pair.Client.EntMan.EntityExists(pair.ToClientUid(instrument)), Is.True);
        });

        if (playing)
        {
            await pair.Server.WaitAssertion(() =>
            {
                var instrumentComp = pair.Server.EntMan.GetComponent<InstrumentComponent>(instrument);
                SharedInstrumentPlaying.SetValue(instrumentComp, true);
            });
        }

        return instrument;
    }

    private static async Task<EntityUid> EnsurePlayerAttached(TestPair pair)
    {
        var session = pair.Server.ResolveDependency<IPlayerManager>().Sessions.Single();
        if (session.AttachedEntity is { } attached)
            return attached;

        var map = await pair.CreateTestMap();
        EntityUid player = default;

        await pair.Server.WaitAssertion(() =>
        {
            var serverSession = pair.Server.ResolveDependency<IPlayerManager>().Sessions.Single();
            player = pair.Server.EntMan.SpawnEntity(PlayerPrototype, map.GridCoords.Offset(new Vector2(0.5f, 0.5f)));
            pair.Server.PlayerMan.SetAttachedEntity(serverSession, player);
        });

        await pair.RunTicksSync(1);
        return player;
    }

    public enum RejectedBatchScenario
    {
        Empty,
        Oversized,
        InvalidPayload
    }

    private static readonly PropertyInfo SharedInstrumentPlaying =
        typeof(SharedInstrumentComponent).GetProperty(nameof(SharedInstrumentComponent.Playing))!;
}

public sealed class InstrumentNetworkProbeSystem : EntitySystem
{
    public List<InstrumentMidiEventEvent> MidiEventsReceived { get; } = new();
    public List<InstrumentStopMidiEvent> StopEventsReceived { get; } = new();

    public override void Initialize()
    {
        SubscribeNetworkEvent<InstrumentMidiEventEvent>(OnMidiEvent);
        SubscribeNetworkEvent<InstrumentStopMidiEvent>(OnStopEvent);
    }

    public void Reset()
    {
        MidiEventsReceived.Clear();
        StopEventsReceived.Clear();
    }

    public void SendMidi(NetEntity uid, RobustMidiEvent[] midiEvents)
    {
        RaiseNetworkEvent(new InstrumentMidiEventEvent(uid, midiEvents));
    }

    public void SendStop(NetEntity uid)
    {
        RaiseNetworkEvent(new InstrumentStopMidiEvent(uid));
    }

    public void SendFilteredChannel(NetEntity uid, int channel, bool value)
    {
        RaiseNetworkEvent(new InstrumentSetFilteredChannelEvent(uid, channel, value));
    }

    private void OnMidiEvent(InstrumentMidiEventEvent ev)
    {
        MidiEventsReceived.Add(ev);
    }

    private void OnStopEvent(InstrumentStopMidiEvent ev)
    {
        StopEventsReceived.Add(ev);
    }
}

using System.Linq;
using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server.Interaction;
using Content.Server.Popups;
using Content.Server.Stunnable;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Instruments;
using Content.Shared.Instruments.UI;
using Content.Shared.Physics;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Midi;
using Robust.Shared.Collections;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.GameStates;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Instruments;

// TODO: Sunrise - возможно полностью скопировать систему в папки санрайза, чтобы сделать ее нормальной и безопасной.
[UsedImplicitly]
public sealed partial class InstrumentSystem : SharedInstrumentSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IConsoleHost _conHost = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly StunSystem _stuns = default!;
    [Dependency] private readonly UserInterfaceSystem _bui = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly ExamineSystemShared _examineSystem = default!;
    [Dependency] private readonly IAdminLogManager _admingLogSystem = default!;

    private const float MaxInstrumentBandRange = 10f;

    // Band Requests are queued and delayed both to avoid metagaming and to prevent spamming it, since it's expensive.
    private const float BandRequestDelay = 1.0f;
    private TimeSpan _bandRequestTimer = TimeSpan.Zero;
    private readonly List<InstrumentBandRequestBuiMessage> _bandRequestQueue = new();

    public override void Initialize()
    {
        base.Initialize();

        InitializeCVars();

        SubscribeNetworkEvent<InstrumentMidiEventEvent>(OnMidiEventRx);
        SubscribeNetworkEvent<InstrumentStartMidiEvent>(OnMidiStart);
        SubscribeNetworkEvent<InstrumentStopMidiEvent>(OnMidiStop);
        SubscribeNetworkEvent<InstrumentSetMasterEvent>(OnMidiSetMaster);
        SubscribeNetworkEvent<InstrumentSetFilteredChannelEvent>(OnMidiSetFilteredChannel);
        SubscribeNetworkEvent<InstrumentSetChannelsEvent>(OnMidiSetChannels);

        Subs.BuiEvents<InstrumentComponent>(InstrumentUiKey.Key, subs =>
        {
            subs.Event<BoundUIClosedEvent>(OnBoundUIClosed);
            subs.Event<BoundUIOpenedEvent>(OnBoundUIOpened);
            subs.Event<InstrumentBandRequestBuiMessage>(OnBoundUIRequestBands);
        });

        SubscribeLocalEvent<InstrumentComponent, ComponentGetState>(OnStrumentGetState);

        // Sunrise added start
        InitializeAbuse();
        // Sunrise added end

        _conHost.RegisterCommand("addtoband", AddToBandCommand);
    }

    private void OnStrumentGetState(EntityUid uid, InstrumentComponent component, ref ComponentGetState args)
    {
        args.State = new InstrumentComponentState()
        {
            Playing = component.Playing,
            InstrumentProgram = component.InstrumentProgram,
            InstrumentBank = component.InstrumentBank,
            AllowPercussion = component.AllowPercussion,
            AllowProgramChange = component.AllowProgramChange,
            RespectMidiLimits = component.RespectMidiLimits,
            Master = GetNetEntity(component.Master),
            FilteredChannels = component.FilteredChannels
        };
    }

    [AdminCommand(AdminFlags.Fun)]
    private void AddToBandCommand(IConsoleShell shell, string _, string[] args)
    {
        if (!NetEntity.TryParse(args[0], out var firstUidNet) || !TryGetEntity(firstUidNet, out var firstUid))
        {
            shell.WriteError($"Cannot parse first Uid");
            return;
        }

        if (!NetEntity.TryParse(args[1], out var secondUidNet) || !TryGetEntity(secondUidNet, out var secondUid))
        {
            shell.WriteError($"Cannot parse second Uid");
            return;
        }

        if (!HasComp<ActiveInstrumentComponent>(secondUid))
        {
            shell.WriteError($"Puppet instrument is not active!");
            return;
        }

        var otherInstrument = Comp<InstrumentComponent>(secondUid.Value);
        otherInstrument.Playing = true;
        otherInstrument.Master = firstUid;
        Dirty(secondUid.Value, otherInstrument);
    }

    private void OnMidiStart(InstrumentStartMidiEvent msg, EntitySessionEventArgs args)
    {
        // Sunrise edit start - validate instrument usage server-side
        if (!TryValidateInstrumentRequest(msg.Uid, args, out var uid, out _, out var instrument, requireActiveInstrument: true))
            return;
        // Sunrise edit end

        instrument.Playing = true;
        Dirty(uid, instrument);
    }

    private void OnMidiStop(InstrumentStopMidiEvent msg, EntitySessionEventArgs args)
    {
        // Sunrise edit start - validate instrument usage server-side
        if (!TryValidateInstrumentRequest(msg.Uid, args, out var uid, out _, out var instrument, requireActiveInstrument: true))
            return;
        // Sunrise edit end

        Clean(uid, instrument);
    }


    private void OnMidiSetChannels(InstrumentSetChannelsEvent msg, EntitySessionEventArgs args)
    {
        // Sunrise edit start - validate instrument usage server-side
        if (!TryValidateInstrumentRequest(msg.Uid, args, out var uid, out _, out _, requireActiveInstrument: true)
            || !TryComp(uid, out ActiveInstrumentComponent? activeInstrument))
            return;
        // Sunrise edit end

        if (msg.Tracks.Length > RobustMidiEvent.MaxChannels)
        {
            Log.Warning($"{args.SenderSession.UserId.ToString()} - Tried to send tracks over the limit! Received: {msg.Tracks.Length}; Limit: {RobustMidiEvent.MaxChannels}");
            return;
        }


        foreach (var t in msg.Tracks)
        {
            // Remove any control characters that may be part of the midi file so they don't end up in the admin logs.
            t?.SanitizeFields();
            // Truncate any track names too long.
            t?.TruncateFields(_cfg.GetCVar(CCVars.MidiMaxChannelNameLength));
        }

        var tracksString = string.Join("\n",
            msg.Tracks
            .Where(t => t != null)
            .Select(t => t!.ToString()));

        _admingLogSystem.Add(
            LogType.Instrument,
            LogImpact.Low,
            $"{ToPrettyString(args.SenderSession.AttachedEntity)} set the midi channels for {ToPrettyString(uid)} to {tracksString}");

        activeInstrument.Tracks = msg.Tracks;

        Dirty(uid, activeInstrument);
    }

    private void OnMidiSetMaster(InstrumentSetMasterEvent msg, EntitySessionEventArgs args)
    {
        var master = GetEntity(msg.Master);

        // Sunrise edit start - validate instrument usage server-side
        if (!TryValidateInstrumentRequest(msg.Uid, args, out var uid, out var user, out var instrument, requireActiveInstrument: true))
            return;
        // Sunrise edit end

        if (master != null)
        {
            // Sunrise edit start
            if (!TryComp<InstrumentComponent>(master, out var masterInstrument)
                || !CanJoinInstrumentBand(uid, user, master.Value, masterInstrument))
                return;
            // Sunrise edit end

            instrument.Master = master;
            instrument.FilteredChannels.SetAll(false);
            instrument.Playing = true;
            Dirty(uid, instrument);
            return;
        }

        // Cleanup when disabling master...
        if (master == null && instrument.Master != null)
        {
            Clean(uid, instrument);
        }
    }

    private void OnMidiSetFilteredChannel(InstrumentSetFilteredChannelEvent msg, EntitySessionEventArgs args)
    {
        // Sunrise edit start - validate instrument usage server-side
        if (!TryValidateInstrumentRequest(msg.Uid, args, out var uid, out var user, out var instrument, requireActiveInstrument: true))
            return;

        if (!InstrumentMidiValidation.IsValidChannel(msg.Channel))
            return;
        // Sunrise edit end

        if (msg.Channel == RobustMidiEvent.PercussionChannel && !instrument.AllowPercussion)
            return;

        instrument.FilteredChannels[msg.Channel] = msg.Value;

        if (msg.Value)
        {
            // Prevent stuck notes when turning off a channel... Shrimple.
            // Sunrise edit start - scope all-notes-off to nearby listeners
            RaiseInstrumentMidiEvent(uid,
                new InstrumentMidiEventEvent(msg.Uid, new[] { RobustMidiEvent.AllNotesOff((byte) msg.Channel, 0) }),
                user);
            // Sunrise edit end
        }

        Dirty(uid, instrument);
    }

    private void OnBoundUIClosed(EntityUid uid, InstrumentComponent component, BoundUIClosedEvent args)
    {
        if (HasComp<ActiveInstrumentComponent>(uid)
            && !_bui.IsUiOpen(uid, args.UiKey))
        {
            RemComp<ActiveInstrumentComponent>(uid);
        }

        Clean(uid, component);
    }

    private void OnBoundUIOpened(EntityUid uid, InstrumentComponent component, BoundUIOpenedEvent args)
    {
        EnsureComp<ActiveInstrumentComponent>(uid);
        Clean(uid, component);
    }

    private void OnBoundUIRequestBands(EntityUid uid, InstrumentComponent component, InstrumentBandRequestBuiMessage args)
    {
        foreach (var request in _bandRequestQueue)
        {
            // Prevent spamming requests for the same entity.
            if (request.Entity == args.Entity)
                return;
        }

        _bandRequestQueue.Add(args);
    }

    public (NetEntity, string)[] GetBands(EntityUid uid)
    {
        var metadataQuery = GetEntityQuery<MetaDataComponent>();

        if (Deleted(uid))
            return Array.Empty<(NetEntity, string)>();

        var list = new ValueList<(NetEntity, string)>();
        var instrumentQuery = GetEntityQuery<InstrumentComponent>();

        if (!TryComp(uid, out InstrumentComponent? originInstrument)
            || originInstrument.InstrumentPlayer is not {} originPlayer)
            return Array.Empty<(NetEntity, string)>();

        // It's probably faster to get all possible active instruments than all entities in range
        var activeEnumerator = EntityQueryEnumerator<ActiveInstrumentComponent>();
        while (activeEnumerator.MoveNext(out var entity, out _))
        {
            if (entity == uid)
                continue;

            // Don't grab puppet instruments.
            if (!instrumentQuery.TryGetComponent(entity, out var instrument) || instrument.Master != null)
                continue;

            // We want to use the instrument player's name.
            if (instrument.InstrumentPlayer is not {} playerUid)
                continue;

            // Maybe a bit expensive but oh well GetBands is queued and has a timer anyway.
            // Make sure the instrument is visible
            if (!_examineSystem.InRangeUnOccluded(uid, entity, MaxInstrumentBandRange, e => e == playerUid || e == originPlayer))
                continue;

            if (!metadataQuery.TryGetComponent(playerUid, out var playerMetadata)
                || !metadataQuery.TryGetComponent(entity, out var metadata))
                continue;

            list.Add((GetNetEntity(entity), $"{playerMetadata.EntityName} - {metadata.EntityName}"));
        }

        return list.ToArray();
    }

    public void Clean(EntityUid uid, InstrumentComponent? instrument = null)
    {
        if (!Resolve(uid, ref instrument))
            return;

        if (instrument.Playing)
        {
            var netUid = GetNetEntity(uid);

            // Reset puppet instruments too.
            // Sunrise edit start - scope shutdown MIDI events to nearby listeners
            RaiseInstrumentMidiEvent(uid, new InstrumentMidiEventEvent(netUid, new[] { RobustMidiEvent.SystemReset(0) }));
            RaiseInstrumentStopEvent(uid, new InstrumentStopMidiEvent(netUid));
            // Sunrise edit end
        }

        instrument.Playing = false;
        instrument.Master = null;
        instrument.FilteredChannels.SetAll(false);
        instrument.LastSequencerTick = 0;
        instrument.BatchesDropped = 0;
        instrument.LaggedBatches = 0;
        Dirty(uid, instrument);
    }

    private void OnMidiEventRx(InstrumentMidiEventEvent msg, EntitySessionEventArgs args)
    {
        // Sunrise edit start - validate instrument usage server-side
        if (!TryValidateInstrumentRequest(msg.Uid,
                args,
                out var uid,
                out var attached,
                out var instrument,
                requireActiveInstrument: true,
                requirePlaying: true))
        {
            return;
        }

        var eventCount = msg.MidiEvent.Length;
        if (eventCount == 0
            || eventCount > MaxMidiEventsPerBatch
            || !InstrumentMidiValidation.IsValidBatch(msg.MidiEvent))
        {
            instrument.BatchesDropped++; // Sunrise added
            return;
        }
        // Sunrise edit end

        var send = true;
        var droppedBatch = false; // Sunrise added

        var minTick = uint.MaxValue;
        var maxTick = uint.MinValue;

        for (var i = 0; i < eventCount; i++)  // Sunrise edit
        {
            var tick = msg.MidiEvent[i].Tick;

            if (tick < minTick)
                minTick = tick;

            if (tick > maxTick)
                maxTick = tick;
        }

        if (instrument.LastSequencerTick > minTick)
        {
            instrument.LaggedBatches++;

            if (instrument.RespectMidiLimits)
            {
                if (instrument.LaggedBatches == (int) (MaxMidiLaggedBatches * (1 / 3d) + 1))
                {
                    _popup.PopupEntity(Loc.GetString("instrument-component-finger-cramps-light-message"),
                        uid, attached, PopupType.SmallCaution);
                }
                else if (instrument.LaggedBatches == (int) (MaxMidiLaggedBatches * (2 / 3d) + 1))
                {
                    _popup.PopupEntity(Loc.GetString("instrument-component-finger-cramps-serious-message"),
                        uid, attached, PopupType.MediumCaution);
                }
            }

            if (instrument.LaggedBatches > MaxMidiLaggedBatches)
            {
                send = false;
            }
        }

        // Sunrise added start
        if (!TryConsumeSessionMidiBudget(args.SenderSession.UserId, eventCount))
        {
            droppedBatch = true;
            send = false;
        }

        instrument.MidiEventCount += eventCount;
        if (instrument.MidiEventCount > MaxMidiEventsPerSecond)
        {
            droppedBatch = true;
            send = false;
        }

        if (droppedBatch)
            instrument.BatchesDropped++;
        // Sunrise added end

        instrument.LastSequencerTick = Math.Max(maxTick, minTick);

        // Sunrise edit start - scope forwarded MIDI traffic to nearby listeners
        if (!send)
            return;

        RaiseInstrumentMidiEvent(uid, msg, attached);
        // Sunrise edit end
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_bandRequestQueue.Count > 0 && _bandRequestTimer < _timing.RealTime)
        {
            _bandRequestTimer = _timing.RealTime.Add(TimeSpan.FromSeconds(BandRequestDelay));

            foreach (var request in _bandRequestQueue)
            {
                var entity = GetEntity(request.Entity);

                var nearby = GetBands(entity);
                _bui.ServerSendUiMessage(entity, request.UiKey, new InstrumentBandResponseBuiMessage(nearby), request.Actor);
            }

            _bandRequestQueue.Clear();
        }

        var activeQuery = GetEntityQuery<ActiveInstrumentComponent>();
        var transformQuery = GetEntityQuery<TransformComponent>();

        var query = AllEntityQuery<ActiveInstrumentComponent, InstrumentComponent>();
        while (query.MoveNext(out var uid, out _, out var instrument))
        {
            if (instrument.Master is {} master)
            {
                if (Deleted(master))
                {
                    Clean(uid, instrument);
                }

                var masterActive = activeQuery.CompOrNull(master);
                if (masterActive == null)
                {
                    Clean(uid, instrument);
                }

                var trans = transformQuery.GetComponent(uid);
                var masterTrans = transformQuery.GetComponent(master);
                if (!_transform.InRange(masterTrans.Coordinates, trans.Coordinates, 10f)
)
                {
                    Clean(uid, instrument);
                }
            }

            if (instrument.RespectMidiLimits &&
                (instrument.BatchesDropped >= MaxMidiBatchesDropped
                 || instrument.LaggedBatches >= MaxMidiLaggedBatches))
            {
                if (instrument.InstrumentPlayer is {Valid: true} mob)
                {
                    _stuns.TryUpdateParalyzeDuration(mob, TimeSpan.FromSeconds(1));

                    _popup.PopupEntity(Loc.GetString("instrument-component-finger-cramps-max-message"),
                        uid, mob, PopupType.LargeCaution);
                }

                // Just in case
                Clean(uid);
                _bui.CloseUi(uid, InstrumentUiKey.Key);
            }

            instrument.Timer += frameTime;
            if (instrument.Timer < 1)
                continue;

            instrument.Timer = 0f;
            instrument.MidiEventCount = 0;
            instrument.LaggedBatches = 0;
            instrument.BatchesDropped = 0;
        }
    }

    public void ToggleInstrumentUi(EntityUid uid, EntityUid actor, InstrumentComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        _bui.TryToggleUi(uid, InstrumentUiKey.Key, actor);
    }

    public override bool ResolveInstrument(EntityUid uid, ref SharedInstrumentComponent? component)
    {
        if (component is not null)
            return true;

        TryComp<InstrumentComponent>(uid, out var localComp);
        component = localComp;
        return component != null;
    }
}

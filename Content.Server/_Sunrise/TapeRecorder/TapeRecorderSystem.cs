using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Server._Sunrise.TTS;
using Content.Server.Chat.Systems;
using Content.Server.Popups;
using Content.Shared._Sunrise.TapeRecorder;
using Content.Shared._Sunrise.TTS;
using Content.Shared.Chat;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Mobs.Systems;
using Content.Shared.Paper;
using Content.Shared.Speech;
using Content.Shared.Speech.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Sunrise.TapeRecorder;

public sealed class TapeRecorderSystem : SharedTapeRecorderSystem
{
    [Dependency] private readonly ILogManager _log = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly PaperSystem _paper = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly TTSSystem _tts = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = _log.GetSawmill("tape_recorder");

        SubscribeLocalEvent<TapeRecorderComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<TapeRecorderComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<TapeRecorderComponent, EntInsertedIntoContainerMessage>(OnCassetteInserted);
        SubscribeLocalEvent<TapeRecorderComponent, EntRemovedFromContainerMessage>(OnCassetteRemoved);
        SubscribeLocalEvent<TapeRecorderComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<TapeRecorderComponent, ListenEvent>(OnListen);

        Subs.BuiEvents<TapeRecorderComponent>(TapeRecorderUiKey.Key, subs =>
        {
            subs.Event<TapeRecorderSetModeMessage>(OnSetMode);
            subs.Event<TapeRecorderPrintMessage>(OnPrint);
        });
    }

    private void OnMapInit(Entity<TapeRecorderComponent> ent, ref MapInitEvent args) =>
        _itemSlots.AddItemSlot(ent, TapeRecorderComponent.CassetteSlotId, ent.Comp.CassetteSlot);

    private void OnShutdown(Entity<TapeRecorderComponent> ent, ref ComponentShutdown args)
    {
        RemComp<ActiveListenerComponent>(ent);
        RemComp<ActiveTapeRecorderComponent>(ent);
    }

    private void OnCassetteInserted(Entity<TapeRecorderComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        Stop(ent, false);
        UpdateUserInterface(ent);
    }

    private void OnCassetteRemoved(Entity<TapeRecorderComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        Stop(ent, false);
        UpdateUserInterface(ent);
    }

    private void OnUiOpened(Entity<TapeRecorderComponent> ent, ref BoundUIOpenedEvent args) =>
        UpdateUserInterface(ent);

    private void OnSetMode(Entity<TapeRecorderComponent> ent, ref TapeRecorderSetModeMessage args) =>
        TrySetMode(ent, args.Mode, args.Actor);

    private void OnPrint(Entity<TapeRecorderComponent> ent, ref TapeRecorderPrintMessage args) =>
        TryPrintTranscript(ent, args.Actor);

    private void OnListen(Entity<TapeRecorderComponent> ent, ref ListenEvent args)
    {
        if (ent.Comp.Mode != TapeRecorderMode.Recording)
            return;

        if (!IsLivePlayerSpeechSource(args.Source))
            return;

        if (!TryGetCassette(ent, out var cassette))
            return;

        UpdateRecorder(ent);

        if (cassette.Comp.Position >= cassette.Comp.Capacity)
        {
            Stop(ent);
            return;
        }

        // Determine effective max records based on cassette capacity: 60s->120, 120s->240, 180s->360
        var effectiveMax = (int)(cassette.Comp.Capacity.TotalSeconds * 2);
        if (effectiveMax <= 0)
            effectiveMax = ent.Comp.MaxRecords > 0 ? ent.Comp.MaxRecords : 120;

        if (cassette.Comp.Records.Count >= effectiveMax)
            cassette.Comp.Records.RemoveAt(0);

        cassette.Comp.Records.Add(new TapeCassetteRecord(
            cassette.Comp.Position,
            GetSpeakerName(ent, args.Source),
            args.Message,
            GetSpeakerVoice(args.Source),
            _timing.CurTime));

        Dirty(cassette);
        UpdateUserInterface(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<TapeRecorderComponent, ActiveTapeRecorderComponent>();
        while (query.MoveNext(out var uid, out var recorder, out _))
        {
            UpdateRecorder((uid, recorder));
        }
    }

    public bool TrySetMode(Entity<TapeRecorderComponent> ent, TapeRecorderMode mode, EntityUid user)
    {
        if (!IsValidMode(mode))
            return false;

        if (mode == TapeRecorderMode.Stopped)
        {
            Stop(ent);
            _audio.PlayPvs(ent.Comp.ButtonSound, ent.Owner);
            return true;
        }

        if (!CanSetMode(ent, mode, user))
            return false;

        UpdateRecorder(ent);

        ent.Comp.Mode = mode;
        ent.Comp.LastUpdateTime = _timing.CurTime;
        ent.Comp.NextPlaybackLineTime = TimeSpan.Zero;

        if (mode == TapeRecorderMode.Recording)
        {
            var listener = EnsureComp<ActiveListenerComponent>(ent);
            listener.Range = ent.Comp.RecordingRange;
        }
        else
        {
            RemComp<ActiveListenerComponent>(ent);
        }

        EnsureComp<ActiveTapeRecorderComponent>(ent);
        _audio.PlayPvs(ent.Comp.ButtonSound, ent.Owner);
        Dirty(ent);
        UpdateUserInterface(ent);
        return true;
    }

    public bool CanSetMode(Entity<TapeRecorderComponent> ent, TapeRecorderMode mode, EntityUid user, bool quiet = false)
    {
        if (!TryGetCassette(ent, out var cassette))
        {
            if (!quiet)
                _popup.PopupEntity(Loc.GetString("tape-recorder-popup-no-cassette"), ent, user);
            return false;
        }

        if (mode == TapeRecorderMode.Recording && cassette.Comp.Position >= cassette.Comp.Capacity)
        {
            if (!quiet)
                _popup.PopupEntity(Loc.GetString("tape-recorder-popup-tape-full"), ent, user);
            return false;
        }

        if (mode == TapeRecorderMode.Recording && IsTapePositionUsed(cassette, cassette.Comp.Position))
        {
            if (!quiet)
                _popup.PopupEntity(Loc.GetString("tape-recorder-popup-tape-used"), ent, user);
            return false;
        }

        if (mode == TapeRecorderMode.Playing && !HasUsedTape(cassette))
        {
            if (!quiet)
                _popup.PopupEntity(Loc.GetString("tape-recorder-popup-tape-empty"), ent, user);
            return false;
        }

        return true;
    }

    private void UpdateRecorder(Entity<TapeRecorderComponent> ent)
    {
        if (!TryGetCassette(ent, out var cassette))
        {
            Stop(ent, false);
            return;
        }

        var now = _timing.CurTime;
        var elapsed = TimeSpan.FromSeconds(Math.Max(0, (now - ent.Comp.LastUpdateTime).TotalSeconds));
        ent.Comp.LastUpdateTime = now;

        switch (ent.Comp.Mode)
        {
            case TapeRecorderMode.Recording:
                var oldPosition = cassette.Comp.Position;
                var requestedPosition = Min(cassette.Comp.Capacity, cassette.Comp.Position + elapsed);
                var nextUsedPosition = GetNextUsedPosition(cassette, oldPosition);
                cassette.Comp.Position = Min(requestedPosition, nextUsedPosition);
                AddRecordedRange(cassette, oldPosition, cassette.Comp.Position);

                if (cassette.Comp.Position >= cassette.Comp.Capacity)
                {
                    Stop(ent);
                }
                else if (cassette.Comp.Position < requestedPosition)
                {
                    Stop(ent);
                }
                break;

            case TapeRecorderMode.Playing:
                oldPosition = cassette.Comp.Position;
                cassette.Comp.Position = Min(cassette.Comp.Capacity, cassette.Comp.Position + elapsed);
                PlayDueRecords(ent, cassette, oldPosition, cassette.Comp.Position);
                if (cassette.Comp.Position >= cassette.Comp.Capacity)
                    Stop(ent);
                break;

            case TapeRecorderMode.Rewinding:
                cassette.Comp.Position = Max(TimeSpan.Zero, cassette.Comp.Position - elapsed * ent.Comp.RewindSpeed);
                if (cassette.Comp.Position <= TimeSpan.Zero)
                    Stop(ent);
                break;
        }

        Dirty(ent);
        Dirty(cassette);
        UpdateUserInterface(ent);
    }

    private void PlayDueRecords(
        Entity<TapeRecorderComponent> recorder,
        Entity<TapeCassetteComponent> cassette,
        TimeSpan oldPosition,
        TimeSpan newPosition)
    {
        if (_timing.CurTime < recorder.Comp.NextPlaybackLineTime)
            return;

        foreach (var record in cassette.Comp.Records)
        {
            if (record.Time < oldPosition || record.Time > newPosition)
                continue;

            recorder.Comp.NextPlaybackLineTime = _timing.CurTime + recorder.Comp.PlaybackLineCooldown;
            _ = PlayRecord(recorder, record);
            return;
        }
    }

    private async Task PlayRecord(Entity<TapeRecorderComponent> recorder, TapeCassetteRecord record)
    {
        try
        {
            _chat.TrySendInGameICMessage(
                recorder,
                record.Message,
                InGameICChatType.Speak,
                ChatTransmitRange.Normal,
                nameOverride: GetPlaybackSpeakerName(recorder, record),
                checkRadioPrefix: false,
                ignoreActionBlocker: true);

            var voiceId = record.VoiceId ?? recorder.Comp.PlaybackVoice;
            if (!_prototype.TryIndex<TTSVoicePrototype>(voiceId, out var voice))
                return;

            var recipients = Filter.Pvs(recorder.Owner);
            if (!recipients.Recipients.Any())
                return;

            var soundData = await _tts.GenerateTTS(record.Message, voice);
            if (soundData == null)
                return;

            RaiseNetworkEvent(new PlayTTSEvent(soundData, GetNetEntity(recorder.Owner)), recipients);
        }
        catch (Exception ex)
        {
            _sawmill.Error($"Failed to play tape recorder TTS for {ToPrettyString(recorder)}: {ex}");
        }
    }

    private bool TryPrintTranscript(Entity<TapeRecorderComponent> ent, EntityUid user)
    {
        if (_timing.CurTime < ent.Comp.NextPrintTime)
            return false;

        if (!TryGetCassette(ent, out var cassette))
        {
            _popup.PopupEntity(Loc.GetString("tape-recorder-popup-no-cassette"), ent, user);
            return false;
        }

        if (cassette.Comp.Records.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("tape-recorder-popup-tape-empty"), ent, user);
            return false;
        }

        var paper = Spawn(ent.Comp.PaperPrototype, Transform(user).Coordinates);
        if (!TryComp<PaperComponent>(paper, out var paperComp))
            return false;

        _paper.SetContent((paper, paperComp), BuildTranscript(cassette));
        ent.Comp.NextPrintTime = _timing.CurTime + ent.Comp.PrintCooldown;
        _audio.PlayPvs(ent.Comp.PrintSound, ent.Owner);
        Dirty(ent);
        return true;
    }

    private string BuildTranscript(Entity<(TapeCassetteComponent)> cassette)
    {
        var transcript = new StringBuilder();
        var speakerMap = new Dictionary<string, int>();
        var nextSpeaker = 1;

        foreach (var record in cassette.Comp.Records)
        {
            if (!speakerMap.TryGetValue(record.Speaker, out var idx))
            {
                idx = nextSpeaker++;
                speakerMap[record.Speaker] = idx;
            }

            var speakerLabel = Loc.GetString("tape-recorder-transcript-speaker", ("number", idx));
            var serverTime = FormatTime(record.RecordedAt); // hh:mm:ss
            var pos = FormatPosition(record.Time); // mm:ss

            transcript.AppendLine(Loc.GetString(
                "tape-recorder-transcript-line",
                ("time", serverTime),
                ("position", pos),
                ("speaker", speakerLabel),
                ("message", record.Message)));
        }

        return transcript.ToString().TrimEnd();
    }

    private bool TryGetCassette(Entity<TapeRecorderComponent> recorder, out Entity<TapeCassetteComponent> cassette)
    {
        cassette = default;
        var cassetteUid = recorder.Comp.CassetteSlot.Item;
        if (cassetteUid == null)
            return false;

        if (!TryComp<TapeCassetteComponent>(cassetteUid, out var cassetteComp))
            return false;

        cassette = (cassetteUid.Value, cassetteComp);
        return true;
    }

    private void Stop(Entity<TapeRecorderComponent> ent, bool updateUi = true)
    {
        ent.Comp.Mode = TapeRecorderMode.Stopped;
        ent.Comp.LastUpdateTime = _timing.CurTime;
        RemComp<ActiveListenerComponent>(ent);
        RemComp<ActiveTapeRecorderComponent>(ent);
        Dirty(ent);

        if (updateUi)
            UpdateUserInterface(ent);
    }

    private void UpdateUserInterface(Entity<TapeRecorderComponent> ent)
    {
        if (!_ui.IsUiOpen(ent.Owner, TapeRecorderUiKey.Key))
            return;

        var cassetteName = Loc.GetString("tape-recorder-ui-no-cassette");
        var position = TimeSpan.Zero;
        var capacity = TimeSpan.Zero;
        var records = 0;
        var recordedRanges = new List<TapeCassetteRecordedRange>();
        var hasCassette = false;
        var canRecord = false;
        var canPlay = false;

        if (TryGetCassette(ent, out var cassette))
        {
            cassetteName = Name(cassette);
            position = cassette.Comp.Position;
            capacity = cassette.Comp.Capacity;
            records = cassette.Comp.Records.Count;
            recordedRanges = cassette.Comp.RecordedRanges.ToList();
            hasCassette = true;
            canRecord = position < capacity && !IsTapePositionUsed(cassette, position);
            canPlay = HasUsedTape(cassette);
        }

        _ui.SetUiState(ent.Owner, TapeRecorderUiKey.Key, new TapeRecorderBoundUserInterfaceState(
            cassetteName,
            ent.Comp.Mode,
            position,
            capacity,
            recordedRanges,
            records,
            hasCassette,
            canRecord,
            canPlay));
    }

    private string GetSpeakerName(Entity<TapeRecorderComponent> recorder, EntityUid source) =>
        Exists(source) ? Name(source) : Loc.GetString(recorder.Comp.UnknownSpeaker);

    private string GetPlaybackSpeakerName(Entity<TapeRecorderComponent> recorder, TapeCassetteRecord record) =>
        $"{Name(recorder)} ({record.Speaker})";

    private ProtoId<TTSVoicePrototype>? GetSpeakerVoice(EntityUid source)
    {
        if (!TryComp<TTSComponent>(source, out var ttsComponent))
            return null;

        var voiceId = ttsComponent.VoicePrototypeId;
        if (voiceId == null || string.IsNullOrWhiteSpace(voiceId.Value))
            return null;

        var voiceEv = new TransformSpeakerVoiceEvent(source, voiceId.Value);
        RaiseLocalEvent(source, voiceEv);
        return voiceEv.VoiceId;
    }

    private bool IsLivePlayerSpeechSource(EntityUid source) =>
        HasComp<ActorComponent>(source) && _mobState.IsAlive(source);

    private static bool IsTapePositionUsed(Entity<TapeCassetteComponent> cassette, TimeSpan position)
    {
        foreach (var range in cassette.Comp.RecordedRanges)
        {
            if (position >= range.Start && position < range.End)
                return true;
        }

        return false;
    }

    private static bool HasUsedTape(Entity<TapeCassetteComponent> cassette) =>
        cassette.Comp.Records.Count > 0 || cassette.Comp.RecordedRanges.Count > 0;

    private static TimeSpan GetNextUsedPosition(Entity<TapeCassetteComponent> cassette, TimeSpan position)
    {
        var next = cassette.Comp.Capacity;
        foreach (var range in cassette.Comp.RecordedRanges)
        {
            if (range.Start > position && range.Start < next)
                next = range.Start;
        }

        return next;
    }

    private static void AddRecordedRange(Entity<TapeCassetteComponent> cassette, TimeSpan start, TimeSpan end)
    {
        if (end <= start)
            return;

        cassette.Comp.RecordedRanges.Add(new TapeCassetteRecordedRange(start, end));
        cassette.Comp.RecordedRanges.Sort(static (a, b) => a.Start.CompareTo(b.Start));

        for (var i = 0; i < cassette.Comp.RecordedRanges.Count - 1; i++)
        {
            var current = cassette.Comp.RecordedRanges[i];
            var next = cassette.Comp.RecordedRanges[i + 1];
            if (current.End < next.Start)
                continue;

            cassette.Comp.RecordedRanges[i] = new TapeCassetteRecordedRange(current.Start, Max(current.End, next.End));
            cassette.Comp.RecordedRanges.RemoveAt(i + 1);
            i--;
        }
    }

    private static bool IsValidMode(TapeRecorderMode mode) =>
        Enum.IsDefined(typeof(TapeRecorderMode), mode);

    private static string FormatTime(TimeSpan time) =>
        time.ToString(@"hh\:mm\:ss");

    private static string FormatPosition(TimeSpan time) =>
        time.ToString(@"mm\:ss");

    private static TimeSpan Min(TimeSpan a, TimeSpan b) =>
        a <= b ? a : b;

    private static TimeSpan Max(TimeSpan a, TimeSpan b) =>
        a >= b ? a : b;
}
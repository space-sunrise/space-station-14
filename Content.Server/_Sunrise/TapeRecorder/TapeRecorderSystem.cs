using System.Linq;
using System.Text;
using Content.Server._Sunrise.TTS;
using Content.Server.Chat.Systems;
using Content.Server.Popups;
using Content.Shared._Sunrise.TapeRecorder;
using Content.Shared._Sunrise.TTS;
using Content.Shared.Chat;
using Content.Shared.Containers.ItemSlots;
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
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly PaperSystem _paper = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly TTSSystem _tts = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

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

        if (!TryGetCassette(ent, out var cassette))
            return;

        UpdateRecorder(ent);

        if (cassette.Comp.Position >= cassette.Comp.Capacity)
        {
            Stop(ent);
            return;
        }

        if (ent.Comp.MaxRecords <= 0)
            return;

        if (cassette.Comp.Records.Count >= ent.Comp.MaxRecords)
            cassette.Comp.Records.RemoveAt(0);

        cassette.Comp.Records.Add(new TapeCassetteRecord(
            cassette.Comp.Position,
            GetSpeakerName(ent, args.Source),
            args.Message));

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

        if (mode == TapeRecorderMode.Playing && cassette.Comp.Records.Count == 0)
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
                cassette.Comp.Position = Min(cassette.Comp.Capacity, cassette.Comp.Position + elapsed);
                if (cassette.Comp.Position >= cassette.Comp.Capacity)
                    Stop(ent);
                break;

            case TapeRecorderMode.Playing:
                var oldPosition = cassette.Comp.Position;
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
            PlayRecord(recorder, record);
            return;
        }
    }

    private async void PlayRecord(Entity<TapeRecorderComponent> recorder, TapeCassetteRecord record)
    {
        _chat.TrySendInGameICMessage(
            recorder,
            record.Message,
            InGameICChatType.Speak,
            ChatTransmitRange.Normal,
            nameOverride: GetPlaybackSpeakerName(recorder, record),
            checkRadioPrefix: false,
            ignoreActionBlocker: true);

        if (!_prototype.TryIndex<TTSVoicePrototype>(recorder.Comp.PlaybackVoice, out var voice))
            return;

        var recipients = Filter.Pvs(recorder.Owner);
        if (!recipients.Recipients.Any())
            return;

        var soundData = await _tts.GenerateTTS(record.Message, voice);
        if (soundData == null)
            return;

        RaiseNetworkEvent(new PlayTTSEvent(soundData, GetNetEntity(recorder.Owner)), recipients);
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

    private string BuildTranscript(Entity<TapeCassetteComponent> cassette)
    {
        var transcript = new StringBuilder();
        transcript.AppendLine(Loc.GetString("tape-recorder-transcript-start"));
        foreach (var record in cassette.Comp.Records)
        {
            transcript.AppendLine(Loc.GetString(
                "tape-recorder-transcript-line",
                ("time", FormatTime(record.Time)),
                ("speaker", record.Speaker),
                ("message", record.Message)));
        }

        transcript.Append(Loc.GetString("tape-recorder-transcript-end"));
        return transcript.ToString();
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
        var hasCassette = false;

        if (TryGetCassette(ent, out var cassette))
        {
            cassetteName = Name(cassette);
            position = cassette.Comp.Position;
            capacity = cassette.Comp.Capacity;
            records = cassette.Comp.Records.Count;
            hasCassette = true;
        }

        _ui.SetUiState(ent.Owner, TapeRecorderUiKey.Key, new TapeRecorderBoundUserInterfaceState(
            cassetteName,
            ent.Comp.Mode,
            position,
            capacity,
            records,
            hasCassette));
    }

    private string GetSpeakerName(Entity<TapeRecorderComponent> recorder, EntityUid source) =>
        Exists(source) ? Name(source) : Loc.GetString(recorder.Comp.UnknownSpeaker);

    private string GetPlaybackSpeakerName(Entity<TapeRecorderComponent> recorder, TapeCassetteRecord record) =>
        $"{Name(recorder)} ({record.Speaker})";

    private static bool IsValidMode(TapeRecorderMode mode) =>
        Enum.IsDefined(typeof(TapeRecorderMode), mode);

    private static string FormatTime(TimeSpan time) =>
        time.ToString(@"hh\:mm\:ss");

    private static TimeSpan Min(TimeSpan a, TimeSpan b) =>
        a <= b ? a : b;

    private static TimeSpan Max(TimeSpan a, TimeSpan b) =>
        a >= b ? a : b;
}

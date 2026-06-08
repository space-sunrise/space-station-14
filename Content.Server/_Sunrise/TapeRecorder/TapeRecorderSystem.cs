using System.Linq;
using Content.Server._Sunrise.TTS;
using Content.Server.Popups;
using Content.Shared._Sunrise.TapeRecorder;
using Content.Shared._Sunrise.TTS;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Paper;
using Content.Shared.Speech;
using Content.Shared.Speech.Components;
using Content.Shared.UserInterface;
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
        SubscribeLocalEvent<TapeRecorderComponent, TapeRecorderSetModeMessage>(OnSetMode);
        SubscribeLocalEvent<TapeRecorderComponent, TapeRecorderPrintMessage>(OnPrint);
    }

    private void OnMapInit(Entity<TapeRecorderComponent> ent, ref MapInitEvent args)
    {
        _itemSlots.AddItemSlot(ent, TapeRecorderComponent.CassetteSlotId, ent.Comp.CassetteSlot);
    }

    private void OnShutdown(Entity<TapeRecorderComponent> ent, ref ComponentShutdown args)
    {
        RemComp<ActiveListenerComponent>(ent);
        RemComp<ActiveTapeRecorderComponent>(ent);
    }

    private void OnCassetteInserted(Entity<TapeRecorderComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        ent.Comp.InsertedCassette = args.Entity;
        Stop(ent, false);
        Dirty(ent);
        UpdateUserInterface(ent);
    }

    private void OnCassetteRemoved(Entity<TapeRecorderComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        ent.Comp.InsertedCassette = null;
        Stop(ent, false);
        Dirty(ent);
        UpdateUserInterface(ent);
    }

    private void OnUiOpened(Entity<TapeRecorderComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUserInterface(ent);
    }

    private void OnSetMode(Entity<TapeRecorderComponent> ent, ref TapeRecorderSetModeMessage args)
    {
        TrySetMode(ent, args.Mode, args.Actor);
    }

    private void OnPrint(Entity<TapeRecorderComponent> ent, ref TapeRecorderPrintMessage args)
    {
        TryPrintTranscript(ent, args.Actor);
    }

    private void OnListen(Entity<TapeRecorderComponent> ent, ref ListenEvent args)
    {
        if (ent.Comp.Mode != TapeRecorderMode.Recording)
            return;

        if (!TryGetCassette(ent, out var cassette))
            return;

        UpdateRecorder(ent);

        if (cassette.Comp.PositionSeconds >= cassette.Comp.CapacitySeconds)
        {
            Stop(ent);
            return;
        }

        cassette.Comp.Records.Add(new TapeCassetteRecord
        {
            Time = cassette.Comp.PositionSeconds,
            Speaker = GetSpeakerName(ent, args.Source),
            Message = args.Message
        });

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

        if (mode == TapeRecorderMode.Recording && cassette.Comp.PositionSeconds >= cassette.Comp.CapacitySeconds)
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
        var elapsed = Math.Max(0f, (float) (now - ent.Comp.LastUpdateTime).TotalSeconds);
        ent.Comp.LastUpdateTime = now;

        switch (ent.Comp.Mode)
        {
            case TapeRecorderMode.Recording:
                cassette.Comp.PositionSeconds = Math.Min(cassette.Comp.CapacitySeconds, cassette.Comp.PositionSeconds + elapsed);
                if (cassette.Comp.PositionSeconds >= cassette.Comp.CapacitySeconds)
                    Stop(ent);
                break;

            case TapeRecorderMode.Playing:
                var oldPosition = cassette.Comp.PositionSeconds;
                cassette.Comp.PositionSeconds = Math.Min(cassette.Comp.CapacitySeconds, cassette.Comp.PositionSeconds + elapsed);
                PlayDueRecords(ent, cassette, oldPosition, cassette.Comp.PositionSeconds);
                if (cassette.Comp.PositionSeconds >= cassette.Comp.CapacitySeconds)
                    Stop(ent);
                break;

            case TapeRecorderMode.Rewinding:
                cassette.Comp.PositionSeconds = Math.Max(0f, cassette.Comp.PositionSeconds - elapsed * ent.Comp.RewindSpeed);
                if (cassette.Comp.PositionSeconds <= 0f)
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
        float oldPosition,
        float newPosition)
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

        var paper = Spawn(ent.Comp.PaperPrototype, Transform(ent.Owner).Coordinates);
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
        var transcript = Loc.GetString("tape-recorder-transcript-start");
        foreach (var record in cassette.Comp.Records.OrderBy(record => record.Time))
        {
            transcript += "\n" + Loc.GetString(
                "tape-recorder-transcript-line",
                ("time", FormatTime(record.Time)),
                ("speaker", record.Speaker),
                ("message", record.Message));
        }

        return transcript + "\n" + Loc.GetString("tape-recorder-transcript-end");
    }

    private bool TryGetCassette(Entity<TapeRecorderComponent> recorder, out Entity<TapeCassetteComponent> cassette)
    {
        cassette = default;
        if (recorder.Comp.InsertedCassette == null)
            return false;

        if (!TryComp<TapeCassetteComponent>(recorder.Comp.InsertedCassette, out var cassetteComp))
            return false;

        cassette = (recorder.Comp.InsertedCassette.Value, cassetteComp);
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
        var position = 0f;
        var capacity = 0f;
        var records = 0;
        var hasCassette = false;

        if (TryGetCassette(ent, out var cassette))
        {
            cassetteName = Name(cassette);
            position = cassette.Comp.PositionSeconds;
            capacity = cassette.Comp.CapacitySeconds;
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

    private string GetSpeakerName(Entity<TapeRecorderComponent> recorder, EntityUid source)
    {
        return Exists(source) ? Name(source) : Loc.GetString(recorder.Comp.UnknownSpeaker);
    }

    private static string FormatTime(float seconds)
    {
        return TimeSpan.FromSeconds(seconds).ToString(@"hh\:mm\:ss");
    }
}

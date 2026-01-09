using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Content.Server._Sunrise.AnnouncementSpeaker;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.Power.Components;
using Content.Shared._Sunrise.CollectiveMind;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared._Sunrise.TTS;
using Content.Shared._Sunrise.AnnouncementSpeaker.Components;
using Content.Shared._Sunrise.AnnouncementSpeaker.Events;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared.Chat;

namespace Content.Server._Sunrise.TTS;

// ReSharper disable once InconsistentNaming
public sealed partial class TTSSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly TTSManager _ttsManager = default!;
    [Dependency] private readonly SharedTransformSystem _xforms = default!;
    [Dependency] private readonly IRobustRandom _rng = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly AnnouncementSpeakerSystem _announcementSpeakerSystem = default!;

    private readonly List<string> _sampleText =
        new()
        {
            "Съешь же ещё этих мягких французских булок, да выпей чаю.",
            "Клоун, прекрати разбрасывать банановые кожурки офицерам под ноги!",
            "Капитан, вы уверены что хотите назначить клоуна на должность главы персонала?",
            "Эс Бэ! Тут человек в сером костюме, с тулбоксом и в маске! Помогите!!",
            "Учёные, тут странная аномалия в баре! Она уже съела мима!",
            "Я надеюсь что инженеры внимательно следят за сингулярностью...",
            "Вы слышали эти странные крики в техах? Мне кажется туда ходить небезопасно.",
            "Вы не видели Гамлета? Мне кажется он забегал к вам на кухню.",
            "Здесь есть доктор? Человек умирает от отравленного пончика! Нужна помощь!",
            "Вам нужно согласие и печать квартирмейстера, если вы хотите сделать заказ на партию дробовиков.",
            "Возле эвакуационного шаттла разгерметизация! Инженеры, нам срочно нужна ваша помощь!",
            "Бармен, налей мне самого крепкого вина, которое есть в твоих запасах!"
        };


    private const int MaxMessageChars = 100 * 2; // same as SingleBubbleCharLimit * 2
    private bool _isEnabled;
    private string _defaultAnnounceVoice = "Hanson";
    private List<ICommonSession> _ignoredRecipients = new();
    private const float WhisperVoiceVolumeModifier = 0.6f; // how far whisper goes in world units
    private const int WhisperVoiceRange = 3; // how far whisper goes in world units
    private string _radioEffect = string.Empty;

    public override void Initialize()
    {
        _cfg.OnValueChanged(SunriseCCVars.TTSEnabled, v => _isEnabled = v, true);
        _cfg.OnValueChanged(SunriseCCVars.TTSRadioEffect, OnRadioEffectChanged, true);

        SubscribeLocalEvent<TransformSpeechEvent>(OnTransformSpeech);
        SubscribeLocalEvent<TTSComponent, EntitySpokeEvent>(OnEntitySpoke);
        SubscribeLocalEvent<RadioSpokeEvent>(OnRadioReceiveEvent);
        SubscribeLocalEvent<CollectiveMindSpokeEvent>(OnCollectiveMindSpokeEvent);
        SubscribeLocalEvent<AnnouncementSpeakerEvent>(OnAnnouncementSpeaker);

        SubscribeNetworkEvent<RequestPreviewTTSEvent>(OnRequestPreviewTTS);
        SubscribeNetworkEvent<ClientOptionTTSEvent>(OnClientOptionTTS);
    }

    private void OnRadioEffectChanged(string value)
    {
        _radioEffect = value;
    }

    private async void OnRequestPreviewTTS(RequestPreviewTTSEvent ev, EntitySessionEventArgs args)
    {
        if (!_isEnabled ||
            !_prototypeManager.TryIndex<TTSVoicePrototype>(ev.VoiceId, out var protoVoice))
            return;

        var previewText = _rng.Pick(_sampleText);
        var soundData = await GenerateTTS(previewText, protoVoice);
        if (soundData is null)
            return;

        RaiseNetworkEvent(new PlayTTSEvent(soundData), Filter.SinglePlayer(args.SenderSession));
    }

    private async void OnClientOptionTTS(ClientOptionTTSEvent ev, EntitySessionEventArgs args)
    {
        if (ev.Enabled)
            _ignoredRecipients.Remove(args.SenderSession);
        else
            _ignoredRecipients.Add(args.SenderSession);
    }

    private void OnRadioReceiveEvent(RadioSpokeEvent args)
    {
        if (!_isEnabled || args.Message.Length > MaxMessageChars)
            return;

        if (!TryComp(args.Source, out TTSComponent? senderComponent))
            return;

        var voiceId = senderComponent.VoicePrototypeId;
        if (voiceId == null || string.IsNullOrWhiteSpace(voiceId.Value))
            return;

        var voiceEv = new TransformSpeakerVoiceEvent(args.Source, voiceId.Value);
        RaiseLocalEvent(args.Source, voiceEv);
        voiceId = voiceEv.VoiceId;

        if (!GetVoicePrototype(voiceId, out var protoVoice))
        {
            return;
        }

        var accentEvent = new TTSSanitizeEvent(args.Message);
        RaiseLocalEvent(args.Source, accentEvent);
        var message = accentEvent.Text;

        HandleRadio(args.Receivers, message, protoVoice, voiceEv.Effect);
    }

    private async void OnCollectiveMindSpokeEvent(CollectiveMindSpokeEvent args)
    {
        if (!_isEnabled || args.Message.Length > MaxMessageChars)
            return;

        // Get the collective mind prototype to use its voice
        if (!_prototypeManager.TryIndex<CollectiveMindPrototype>(args.CollectiveMindId, out var collectiveMindProto))
            return;

        var voiceId = collectiveMindProto.VoiceId;
        if (voiceId == null || string.IsNullOrWhiteSpace(voiceId.Value))
            return;

        if (!GetVoicePrototype(voiceId, out var protoVoice))
        {
            return;
        }

        var accentEvent = new TTSSanitizeEvent(args.Message);
        RaiseLocalEvent(args.Source, accentEvent);
        var message = accentEvent.Text;

        var soundData = await GenerateTTS(message, protoVoice);
        if (soundData is null)
            return;

        var recipients = Filter.Entities(args.Receivers.ToArray()).RemovePlayers(_ignoredRecipients);
        RaiseNetworkEvent(new PlayTTSEvent(soundData, null, false), recipients);
    }

    private bool GetVoicePrototype(ProtoId<TTSVoicePrototype>? voiceId, [NotNullWhen(true)] out TTSVoicePrototype? voicePrototype)
    {
        if (!_prototypeManager.TryIndex(voiceId, out voicePrototype))
        {
            return _prototypeManager.TryIndex(_defaultAnnounceVoice, out voicePrototype);
        }

        return true;
    }

    /// <summary>
    /// Handles station-wide announcements by finding all speakers on the station and playing the announcement through them.
    /// </summary>
    private void OnAnnouncementSpeaker(ref AnnouncementSpeakerEvent ev)
    {
        // Find all speakers on the station
        var speakers = _announcementSpeakerSystem.GetStationSpeakers(ev.Station);

        if (speakers.Count == 0)
        {
            // Fallback: If no speakers are found, log a warning
            // In the future, this could send to a single communications console or similar
            Logger.Warning($"No announcement speakers found on station {ToPrettyString(ev.Station)}. Announcement not played: {ev.Message}");
            return;
        }

        // Play announcement sound via PVS for each speaker on server side
        if (ev.AnnouncementSound != null)
        {
            foreach (var speaker in speakers)
            {
                if (!TryComp<AnnouncementSpeakerComponent>(speaker, out var speakerComp))
                    continue;

                // Check if speaker is enabled and has power
                if (!speakerComp.Enabled)
                    continue;

                if (speakerComp.RequiresPower)
                {
                    if (!TryComp<ApcPowerReceiverComponent>(speaker, out var powerReceiver) || !powerReceiver.Powered)
                        continue;
                }

                // Play announcement sound via PVS from this speaker
                var audioParams = AudioParams.Default.WithVolume(-2f * speakerComp.VolumeModifier).WithMaxDistance(speakerComp.Range);
                _audioSystem.PlayPvs(ev.AnnouncementSound, speaker, audioParams);
            }
        }

        if (ev.TtsData == null || ev.TtsData.Length <= 0)
            return;

        var speakerData = new List<(EntityUid Uid, AnnouncementSpeakerComponent Comp)>();
        foreach (var speaker in speakers)
        {
            if (!TryComp<AnnouncementSpeakerComponent>(speaker, out var speakerComp))
                continue;
            if (!speakerComp.Enabled)
                continue;
            if (speakerComp.RequiresPower)
            {
                if (!TryComp<ApcPowerReceiverComponent>(speaker, out var powerReceiver) || !powerReceiver.Powered)
                    continue;
            }
            speakerData.Add((speaker, speakerComp));
        }

        // Для каждого игрока на станции определяем, какие динамики он слышит
        var playerQuery = EntityQueryEnumerator<ActorComponent, TransformComponent>();
        while (playerQuery.MoveNext(out var playerUid, out var actor, out var playerXform))
        {
            if (_ignoredRecipients.Contains(actor.PlayerSession))
                continue;

            var heardSpeakers = new List<NetEntity>();
            foreach (var (speakerUid, speakerComp) in speakerData)
            {
                if (Transform(speakerUid).Coordinates.TryDistance(EntityManager, playerXform.Coordinates, out var dist) &&
                    dist <= speakerComp.Range)
                {
                    heardSpeakers.Add(GetNetEntity(speakerUid));
                }
            }
            if (heardSpeakers.Count > 0)
            {
                var evMulti = new PlayMultiSpeakerTTSEvent(heardSpeakers, ev.TtsData);
                RaiseNetworkEvent(evMulti, actor.PlayerSession);
            }
        }
    }

    private async void OnEntitySpoke(EntityUid uid, TTSComponent component, EntitySpokeEvent args)
    {
        var voiceId = component.VoicePrototypeId;
        if (!_isEnabled ||
            args.Message.Length > MaxMessageChars ||
            voiceId == null || string.IsNullOrWhiteSpace(voiceId.Value))
            return;

        var voiceEv = new TransformSpeakerVoiceEvent(uid, voiceId.Value);
        RaiseLocalEvent(uid, voiceEv);
        voiceId = voiceEv.VoiceId;

        if (!GetVoicePrototype(voiceId, out var protoVoice))
        {
            return;
        }

        var accentEvent = new TTSSanitizeEvent(args.Message);
        RaiseLocalEvent(uid, accentEvent);
        var message = accentEvent.Text;

        if (args.ObfuscatedMessage != null)
        {
            HandleWhisper(uid, message, protoVoice);
            return;
        }

        HandleSay(uid, message, protoVoice, voiceEv.Effect);
    }

    private async void HandleSay(EntityUid uid, string message, TTSVoicePrototype voicePrototype, string? effect)
    {
        var recipients = Filter.Pvs(uid, 1F).RemovePlayers(_ignoredRecipients);

        // Если нету получаетей ттса то зачем вообще генерировать его?
        if (!recipients.Recipients.Any())
            return;

        var soundData = await GenerateTTS(message, voicePrototype, effect);

        if (soundData is null)
            return;

        var netEntity = GetNetEntity(uid);

        RaiseNetworkEvent(new PlayTTSEvent(soundData, netEntity), recipients);
    }

    private async void HandleWhisper(EntityUid uid, string message, TTSVoicePrototype voicePrototype)
    {
        // If it's a whisper into a radio, generate speech without whisper
        // attributes to prevent an additional speech synthesis event
        var soundData = await GenerateTTS(message, voicePrototype);
        if (soundData is null)
            return;

        // TODO: Check obstacles
        var xformQuery = GetEntityQuery<TransformComponent>();
        var sourcePos = _xforms.GetWorldPosition(xformQuery.GetComponent(uid), xformQuery);
        var receptions = Filter.Pvs(uid).Recipients;
        foreach (var session in receptions)
        {
            if (!session.AttachedEntity.HasValue)
                continue;

            if (_ignoredRecipients.Contains(session))
                return;

            var xform = xformQuery.GetComponent(session.AttachedEntity.Value);
            var distance = (sourcePos - _xforms.GetWorldPosition(xform, xformQuery)).LengthSquared();

            if (distance > WhisperVoiceRange)
                continue;

            var ttsEvent = new PlayTTSEvent(
                soundData,
                GetNetEntity(uid),
                false,
                WhisperVoiceVolumeModifier * (1f - distance / WhisperVoiceRange));
            RaiseNetworkEvent(ttsEvent, session);
        }
    }

    private async void HandleRadio(EntityUid[] uids, string message, TTSVoicePrototype voicePrototype, string? effect = null)
    {
        var soundData = await GenerateTTS(message, voicePrototype, _radioEffect);
        if (soundData is null)
            return;

        RaiseNetworkEvent(new PlayTTSEvent(soundData, null, true), Filter.Entities(uids).RemovePlayers(_ignoredRecipients));
    }

    // ReSharper disable once InconsistentNaming
    public async Task<byte[]?> GenerateTTS(string text, TTSVoicePrototype voicePrototype, string? effect = null)
    {
        try
        {
            var textSanitized = Sanitize(text);
            if (textSanitized == "") return null;
            if (char.IsLetter(textSanitized[^1]))
                textSanitized += ".";

            return await _ttsManager.ConvertTextToSpeech(voicePrototype, textSanitized, effect);
        }
        catch (Exception e)
        {
            // Catch TTS exceptions to prevent a server crash.
            Logger.Error($"TTS System error: {e.Message}");
        }

        return null;
    }
}

public sealed class TransformSpeakerVoiceEvent : EntityEventArgs
{
    public EntityUid Sender;
    public ProtoId<TTSVoicePrototype> VoiceId;
    public string? Effect;

    public TransformSpeakerVoiceEvent(EntityUid sender, ProtoId<TTSVoicePrototype> voiceId, string? effect = null)
    {
        Sender = sender;
        VoiceId = voiceId;
        Effect = effect;
    }
}

public sealed class TTSSanitizeEvent : EntityEventArgs
{
    public string Text;

    public TTSSanitizeEvent(string text)
    {
        Text = text;
    }
}

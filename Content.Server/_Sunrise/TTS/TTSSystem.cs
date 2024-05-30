using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Content.Server.Chat.Systems;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared._Sunrise.TTS;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Sunrise.TTS;

// ReSharper disable once InconsistentNaming
public sealed partial class TTSSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly TTSManager _ttsManager = default!;
    [Dependency] private readonly SharedTransformSystem _xforms = default!;
    [Dependency] private readonly IRobustRandom _rng = default!;

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
    private bool _isEnabled = false;
    private string _voiceId = "Hanson";
    private string _nukieVoiceId = "Sentrybot";
    public const float WhisperVoiceVolumeModifier = 0.6f; // how far whisper goes in world units
    public const int WhisperVoiceRange = 4; // how far whisper goes in world units

    public override void Initialize()
    {
        _cfg.OnValueChanged(SunriseCCVars.TTSEnabled, v => _isEnabled = v, true);
        _cfg.OnValueChanged(SunriseCCVars.TTSAnnounceVoiceId, v => _voiceId = v, true);
        _cfg.OnValueChanged(SunriseCCVars.TTSNukieAnnounceVoiceId, v => _nukieVoiceId = v, true);

        SubscribeLocalEvent<TransformSpeechEvent>(OnTransformSpeech);
        SubscribeLocalEvent<TTSComponent, EntitySpokeEvent>(OnEntitySpoke);
        SubscribeLocalEvent<RadioSpokeEvent>(OnRadioReceiveEvent);
        SubscribeLocalEvent<AnnouncementSpokeEvent>(OnAnnouncementSpoke);

        SubscribeNetworkEvent<RequestPreviewTTSEvent>(OnRequestPreviewTTS);
    }

    private async void OnRequestPreviewTTS(RequestPreviewTTSEvent ev, EntitySessionEventArgs args)
    {
        if (!_isEnabled ||
            !_prototypeManager.TryIndex<TTSVoicePrototype>(ev.VoiceId, out var protoVoice))
            return;

        var previewText = _rng.Pick(_sampleText);
        var soundData = await GenerateTTS(previewText, protoVoice.Speaker);
        if (soundData is null)
            return;

        RaiseNetworkEvent(new PlayTTSEvent(soundData), Filter.SinglePlayer(args.SenderSession));
    }

    private void OnRadioReceiveEvent(RadioSpokeEvent args)
    {
        if (!_isEnabled || args.Message.Length > MaxMessageChars)
            return;

        if (!TryComp(args.Source, out TTSComponent? senderComponent))
            return;

        var voiceId = senderComponent.VoicePrototypeId;
        if (voiceId == null)
            return;

        var voiceEv = new TransformSpeakerVoiceEvent(args.Source, voiceId);
        RaiseLocalEvent(args.Source, voiceEv);
        voiceId = voiceEv.VoiceId;

        if (!GetVoicePrototype(voiceId, out var protoVoice))
        {
            return;
        }

        HandleRadio(args.Receivers, args.Message, protoVoice.Speaker);
    }

    private bool GetVoicePrototype(string voiceId, [NotNullWhen(true)] out TTSVoicePrototype? voicePrototype)
    {
        if (!_prototypeManager.TryIndex(voiceId, out voicePrototype))
        {
            return _prototypeManager.TryIndex("father_grigori", out voicePrototype);
        }

        return true;
    }

    private async void OnAnnouncementSpoke(AnnouncementSpokeEvent args)
    {
        if (!_isEnabled ||
            args.Message.Length > MaxMessageChars * 2 ||
            !GetVoicePrototype(args.Nukie ? _nukieVoiceId : _voiceId, out var protoVoice))
        {
            RaiseNetworkEvent(new AnnounceTtsEvent(new byte[] { }), args.Source);
            return;
        }

        var soundData = await GenerateTTS(args.Message, protoVoice.Speaker, isAnnounce: true);
        soundData ??= new byte[] { };
        RaiseNetworkEvent(new AnnounceTtsEvent(soundData), args.Source);
    }

    private async void OnEntitySpoke(EntityUid uid, TTSComponent component, EntitySpokeEvent args)
    {
        var voiceId = component.VoicePrototypeId;
        if (!_isEnabled ||
            args.Message.Length > MaxMessageChars ||
            voiceId == null)
            return;

        var voiceEv = new TransformSpeakerVoiceEvent(uid, voiceId);
        RaiseLocalEvent(uid, voiceEv);
        voiceId = voiceEv.VoiceId;

        if (!GetVoicePrototype(voiceId, out var protoVoice))
        {
            return;
        }

        if (args.ObfuscatedMessage != null)
        {
            HandleWhisper(uid, args.Message, protoVoice.Speaker, args.IsRadio);
            return;
        }

        HandleSay(uid, args.Message, protoVoice.Speaker);
    }

    private async void HandleSay(EntityUid uid, string message, string speaker)
    {
        var soundData = await GenerateTTS(message, speaker);
        if (soundData is null) return;
        RaiseNetworkEvent(new PlayTTSEvent(soundData, GetNetEntity(uid)), Filter.Pvs(uid));
    }

    private async void HandleWhisper(EntityUid uid, string message, string speaker, bool isRadio)
    {
        // If it's a whisper into a radio, generate speech without whisper
        // attributes to prevent an additional speech synthesis event
        var soundData = await GenerateTTS(message, speaker);
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

    private async void HandleRadio(EntityUid[] uids, string message, string speaker)
    {
        var soundData = await GenerateTTS(message, speaker, isRadio: true);
        if (soundData is null)
            return;

        foreach (var uid in uids)
        {
            RaiseNetworkEvent(new PlayTTSEvent(soundData, GetNetEntity(uid), true), Filter.Entities(uid));
        }
    }

    // ReSharper disable once InconsistentNaming
    private async Task<byte[]?> GenerateTTS(string text, string speaker, bool isRadio = false, bool isAnnounce = false)
    {
        try
        {
            var textSanitized = Sanitize(text);
            if (textSanitized == "") return null;
            if (char.IsLetter(textSanitized[^1]))
                textSanitized += ".";

            if (isRadio)
            {
                return await _ttsManager.ConvertTextToSpeechRadio(speaker, textSanitized);
            }

            if (isAnnounce)
            {
                return await _ttsManager.ConvertTextToSpeechAnnounce(speaker, textSanitized);
            }

            return await _ttsManager.ConvertTextToSpeech(speaker, textSanitized);
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
    public string VoiceId;

    public TransformSpeakerVoiceEvent(EntityUid sender, string voiceId)
    {
        Sender = sender;
        VoiceId = voiceId;
    }
}

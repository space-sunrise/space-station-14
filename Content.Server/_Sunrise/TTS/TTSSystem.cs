using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared._Sunrise.TTS;
using Robust.Shared.Audio.Systems;
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
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;


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

    public override void Initialize()
    {
        _cfg.OnValueChanged(SunriseCCVars.TTSEnabled, v => _isEnabled = v, true);

        SubscribeLocalEvent<TransformSpeechEvent>(OnTransformSpeech);
        SubscribeLocalEvent<TTSComponent, EntitySpokeEvent>(OnEntitySpoke);
        SubscribeLocalEvent<RadioSpokeEvent>(OnRadioReceiveEvent);
        SubscribeLocalEvent<AnnouncementSpokeEvent>(OnAnnouncementSpoke);

        SubscribeNetworkEvent<RequestPreviewTTSEvent>(OnRequestPreviewTTS);
        SubscribeNetworkEvent<ClientOptionTTSEvent>(OnClientOptionTTS);
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
        if (voiceId == null)
            return;

        var voiceEv = new TransformSpeakerVoiceEvent(args.Source, voiceId);
        RaiseLocalEvent(args.Source, voiceEv);
        voiceId = voiceEv.VoiceId;

        if (!GetVoicePrototype(voiceId, out var protoVoice))
        {
            return;
        }

        var accentEvent = new TTSSanitizeEvent(args.Message);
        RaiseLocalEvent(args.Source, accentEvent);
        var message = accentEvent.Text;

        HandleRadio(args.Receivers, message, protoVoice);
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
        if (!_isEnabled && args.AnnouncementSound != null)
        {
            var allPlayersInGame = Filter.Empty().AddWhere(_gameTicker.UserHasJoinedGame);
            _audioSystem.PlayGlobal(args.AnnouncementSound, allPlayersInGame, true);
            return;
        }

        if (!_isEnabled ||
            args.Message.Length > MaxMessageChars * 2 ||
            !GetVoicePrototype(args.AnnounceVoice ?? _defaultAnnounceVoice, out var protoVoice))
            return;

        var soundData = await GenerateTTS(args.Message, protoVoice, isAnnounce: true);
        soundData ??= [];
        RaiseNetworkEvent(new AnnounceTtsEvent(soundData, args.AnnouncementSound), args.Source.RemovePlayers(_ignoredRecipients));
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

        var accentEvent = new TTSSanitizeEvent(args.Message);
        RaiseLocalEvent(uid, accentEvent);
        var message = accentEvent.Text;

        if (args.ObfuscatedMessage != null)
        {
            HandleWhisper(uid, message, protoVoice);
            return;
        }

        HandleSay(uid, message, protoVoice);
    }

    private async void HandleSay(EntityUid uid, string message, TTSVoicePrototype voicePrototype)
    {
        var recipients = Filter.Pvs(uid, 1F).RemovePlayers(_ignoredRecipients);

        // Если нету получаетей ттса то зачем вообще генерировать его?
        if (!recipients.Recipients.Any())
            return;

        var soundData = await GenerateTTS(message, voicePrototype);

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

    private async void HandleRadio(EntityUid[] uids, string message, TTSVoicePrototype voicePrototype)
    {
        var soundData = await GenerateTTS(message, voicePrototype, isRadio: true);
        if (soundData is null)
            return;

        RaiseNetworkEvent(new PlayTTSEvent(soundData, null, true), Filter.Entities(uids).RemovePlayers(_ignoredRecipients));
    }

    // ReSharper disable once InconsistentNaming
    private async Task<byte[]?> GenerateTTS(string text, TTSVoicePrototype voicePrototype, bool isRadio = false, bool isAnnounce = false)
    {
        try
        {
            var textSanitized = Sanitize(text);
            if (textSanitized == "") return null;
            if (char.IsLetter(textSanitized[^1]))
                textSanitized += ".";

            if (isRadio)
            {
                return await _ttsManager.ConvertTextToSpeechRadio(voicePrototype, textSanitized);
            }

            if (isAnnounce)
            {
                return await _ttsManager.ConvertTextToSpeechAnnounce(voicePrototype, textSanitized);
            }

            return await _ttsManager.ConvertTextToSpeech(voicePrototype, textSanitized);
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

public sealed class TTSSanitizeEvent : EntityEventArgs
{
    public string Text;

    public TTSSanitizeEvent(string text)
    {
        Text = text;
    }
}

using Content.Server._Sunrise.TTS;
using Content.Shared._Sunrise.TTS;
using Content.Shared.VoiceMask;
using Content.Shared.Silicons.StationAi;

namespace Content.Server.VoiceMask;

public partial class VoiceMaskSystem
{
    private void InitializeTTS()
    {
        SubscribeLocalEvent<TTSComponent, TransformSpeakerVoiceEvent>(OnSpeakerVoiceTransform);
        SubscribeLocalEvent<VoiceMaskComponent, VoiceMaskChangeVoiceMessage>(OnChangeVoice);
    }

    private void OnSpeakerVoiceTransform(EntityUid uid, TTSComponent component, TransformSpeakerVoiceEvent args)
    {
        if (TryComp<VoiceMaskerComponent>(uid, out var maskerComponent))
            args.VoiceId = maskerComponent.VoiceId;
    }

    private void OnChangeVoice(EntityUid uid, VoiceMaskComponent component, VoiceMaskChangeVoiceMessage message)
    {
        // Используем существующее поле _proto (или _prototypeManager — как в основном файле)
        if (!_proto.TryIndex<TTSVoicePrototype>(message.Voice, out var voiceProto))
            return;

        // Если ИИ пытается установить неподходящий голос — запрещаем
        if (HasComp<StationAiHeldComponent>(message.Actor) && !voiceProto.CanAiUse)
        {
            _popupSystem.PopupEntity(Loc.GetString("voice-mask-ai-cannot-use-this-voice"), uid);
            return;
        }

        component.VoiceId = message.Voice;

        if (TryComp<VoiceMaskerComponent>(message.Actor, out var maskerComponent))
            maskerComponent.VoiceId = message.Voice;

        _popupSystem.PopupEntity(Loc.GetString("voice-mask-voice-popup-success"), uid);

        TrySetLastKnownVoice(uid, message.Voice);

        UpdateUI((uid, component));
    }

    private void TrySetLastKnownVoice(EntityUid maskWearer, string voiceId)
    {
        if (!TryComp<VoiceMaskComponent>(maskWearer, out var maskComp))
            return;

        maskComp.VoiceId = voiceId;
    }
}
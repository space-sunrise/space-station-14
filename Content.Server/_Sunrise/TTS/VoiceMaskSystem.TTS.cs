using Content.Server._Sunrise.TTS;
using Content.Shared.VoiceMask;

namespace Content.Server.VoiceMask;

public partial class VoiceMaskSystem
{
    private void InitializeTTS()
    {
        SubscribeLocalEvent<VoiceMaskComponent, TransformSpeakerVoiceEvent>(OnSpeakerVoiceTransform);
        SubscribeLocalEvent<VoiceMaskComponent, VoiceMaskChangeVoiceMessage>(OnChangeVoice);
    }

    private void OnSpeakerVoiceTransform(EntityUid uid, VoiceMaskComponent component, TransformSpeakerVoiceEvent args)
    {
        args.VoiceId = component.VoiceId;
    }

    private void OnChangeVoice(EntityUid uid, VoiceMaskComponent component, VoiceMaskChangeVoiceMessage message)
    {
        component.VoiceId = message.Voice;

        _popupSystem.PopupEntity(Loc.GetString("voice-mask-voice-popup-success"), uid);

        TrySetLastKnownVoice(uid, message.Voice);

        UpdateUI((uid, component));
    }

    private void TrySetLastKnownVoice(EntityUid maskWearer, string voiceId)
    {
        if (!TryComp<VoiceMaskComponent>(maskWearer, out var maskComp))
        {
            return;
        }

        maskComp.VoiceId = voiceId;
    }
}

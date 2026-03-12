using Content.Shared._Sunrise.TTS;
using Content.Shared.Humanoid;
using Robust.Shared.Prototypes;

namespace Content.Server.VoiceMask;

[RegisterComponent]
public sealed partial class VoiceMaskerComponent : Component
{
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<TTSVoicePrototype> VoiceId = SharedHumanoidAppearanceSystem.DefaultVoice;
}

using Content.Shared.Humanoid;

namespace Content.Server.VoiceMask;

[RegisterComponent]
public sealed partial class VoiceMaskerComponent : Component
{
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public string VoiceId = SharedHumanoidAppearanceSystem.DefaultVoice;
}

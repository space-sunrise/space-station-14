using Content.Shared.Humanoid;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.TTS;

[Prototype("ttsVoice")]
public sealed class TTSVoicePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [DataField]
    public string Name { get; } = string.Empty;

    [DataField(required: true)]
    public Sex Sex { get; }

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField(required: true)]
    public string Speaker { get; } = string.Empty;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField(required: true)]
    public string Provider { get; } = string.Empty;

    [DataField]
    public bool RoundStart { get; } = true;

    [DataField]
    public bool SponsorOnly { get; }
}

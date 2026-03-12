using Content.Shared.Humanoid;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.TTS;

[Prototype("ttsVoice")]
public sealed partial class TTSVoicePrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public string Name = string.Empty;

    [DataField(required: true)]
    public Sex Sex;

    [DataField(required: true)]
    public string Speaker = string.Empty;

    [DataField(required: true)]
    public string Provider = string.Empty;

    [DataField]
    public bool RoundStart = true;

    [DataField]
    public bool SponsorOnly;
}

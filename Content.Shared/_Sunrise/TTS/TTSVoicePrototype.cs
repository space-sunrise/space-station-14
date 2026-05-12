using Content.Shared.Humanoid;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared._Sunrise.TTS;

[Prototype("ttsVoice")]
public sealed partial class TTSVoicePrototype : IPrototype, IInheritingPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<TTSVoicePrototype>))]
    public string[]? Parents { get; private set; }

    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; private set; }

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

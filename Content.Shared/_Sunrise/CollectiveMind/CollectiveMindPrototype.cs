using Content.Shared._Sunrise.TTS;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.CollectiveMind;

[Prototype]
public sealed partial class CollectiveMindPrototype : IPrototype
{
    [DataField("name")]
    public string Name { get; private set; } = string.Empty;

    [ViewVariables(VVAccess.ReadOnly)]
    public string LocalizedName => Loc.GetString(Name);

    [DataField("keycode")]
    public char KeyCode { get; private set; } = '\0';

    [DataField("color")]
    public Color Color { get; private set; } = Color.Lime;

    [DataField("voiceId")]
    public ProtoId<TTSVoicePrototype>? VoiceId;

    [DataField("showAuthor")]
    public bool ShowAuthor { get; private set; } = false;

    [IdDataField, ViewVariables]
    public string ID { get; private set; } = default!;
}

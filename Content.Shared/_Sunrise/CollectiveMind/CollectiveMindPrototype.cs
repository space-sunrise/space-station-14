using Content.Shared._Sunrise.TTS;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.CollectiveMind;

[Prototype]
public sealed partial class CollectiveMindPrototype : IPrototype
{
    [DataField]
    public string Name { get; private set; } = string.Empty;

    [ViewVariables(VVAccess.ReadOnly)]
    public string LocalizedName => Loc.GetString(Name);

    [DataField]
    public char KeyCode { get; private set; } = '\0';

    [DataField]
    public Color Color { get; private set; } = Color.Lime;

    [DataField]
    public ProtoId<TTSVoicePrototype>? VoiceId;

    [DataField]
    public bool ShowAuthor { get; private set; } = false;

    [IdDataField, ViewVariables]
    public string ID { get; private set; } = default!;

    [DataField]
    public string? RequiredComponent { get; set; } = null;

    [DataField]
    public string? RequiredTag { get; set; } = null;
}

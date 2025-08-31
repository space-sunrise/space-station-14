using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.CollectiveMind;

[Prototype("collectiveMind")]
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
    public string? VoiceId { get; private set; } = null;

    [DataField("showAuthor")]
    public bool ShowAuthor { get; private set; } = false;

    [IdDataField, ViewVariables]
    public string ID { get; } = default!;
}

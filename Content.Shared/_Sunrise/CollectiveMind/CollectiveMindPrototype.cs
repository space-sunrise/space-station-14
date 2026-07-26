using Content.Shared._Sunrise.TTS;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.CollectiveMind;

[Prototype]
public sealed partial class CollectiveMindPrototype : IPrototype
{
    [DataField(required: true)]
    public LocId Name { get; private set; } = string.Empty;

    [ViewVariables]
    public string LocalizedName => Loc.GetString(Name);

    [DataField(required: true)]
    public char KeyCode { get; private set; } = '\0';

    [DataField]
    public Color Color { get; private set; } = Color.Lime;

    [DataField]
    public ProtoId<TTSVoicePrototype>? VoiceId;

    [DataField]
    public bool ShowAuthor { get; private set; }

    [DataField]
    public CollectiveMindMode Mode { get; private set; } = CollectiveMindMode.Global;

    [IdDataField]
    public string ID { get; private set; } = default!;
}

/// <summary>
/// Определяет способ объединения участников одного типа коллективного разума.
/// </summary>
public enum CollectiveMindMode : byte
{
    /// <summary>
    /// Все участники этого типа слышат друг друга.
    /// </summary>
    Global,

    /// <summary>
    /// Участники слышат только членов своей группы; один тип может иметь несколько независимых групп.
    /// Для работы группа должна существовать и иметь активный <see cref="CollectiveMindGroupComponent"/> соответствующего типа.
    /// </summary>
    Group
}

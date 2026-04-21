using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Laws;

/// <summary>
///     Groups laws or guidelines into sections (e.g., "Minor Crimes", "Sentencing Guidelines").
/// </summary>
[Prototype]
public sealed partial class CorporateLawSectionPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    ///     The title of this section.
    /// </summary>
    [DataField(required: true)]
    public string Title = string.Empty;

    /// <summary>
    ///     The list of laws or entries in this section.
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<CorporateLawPrototype>> Entries = new();

    /// <summary>
    ///     Visual color for this section in the PDA UI.
    /// </summary>
    [DataField]
    public Color? Color;
}

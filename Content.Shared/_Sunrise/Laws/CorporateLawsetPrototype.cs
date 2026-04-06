using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Laws;

/// <summary>
///     A complete set of corporate laws and guidelines.
/// </summary>
[Prototype]
public sealed partial class CorporateLawsetPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    ///     The sections in this lawset.
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<CorporateLawSectionPrototype>> Sections = new();
}

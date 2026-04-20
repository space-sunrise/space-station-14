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
    ///     General provisions and rights.
    /// </summary>
    [DataField]
    public List<ProtoId<CorporateLawPrototype>> Provisions = new();

    /// <summary>
    ///     Sentence-modifying factors.
    /// </summary>
    [DataField]
    public List<ProtoId<CorporateLawPrototype>> Circumstances = new();

    /// <summary>
    ///     Categorized legal articles (1xx-6xx).
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<CorporateLawSectionPrototype>> Articles = new();

    /// <summary>
    ///     Threshold at which the sentence becomes permanent/life.
    /// </summary>
    [DataField]
    public int PermanentSentenceThreshold = 50;
}

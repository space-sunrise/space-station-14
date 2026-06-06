using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Laws;

public enum LawCategory : byte
{
    Article,
    Mitigating,
    Aggravating,
    Provision
}

/// <summary>
///     Defines a single corporate law or legal provision entry.
/// </summary>
[Prototype]
public sealed partial class CorporateLawPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    ///     Visual category for UI organization and sentencing logic.
    /// </summary>
    [DataField]
    public LawCategory Category = LawCategory.Article;

    /// <summary>
    ///     Official article number (e.g. "101", "603").
    ///     Nullable for general provisions/guidelines.
    /// </summary>
    [DataField]
    public string? LawIdentifier;

    /// <summary>
    ///     Title of the law.
    /// </summary>
    [DataField(required: true)]
    public string Title = string.Empty;

    /// <summary>
    ///     Full text/description of the law.
    /// </summary>
    [DataField(required: true)]
    public string Description = string.Empty;

    /// <summary>
    ///     Base sentence duration in minutes.
    /// </summary>
    [DataField("baseSentence")]
    public int BaseSentence = 0;

    /// <summary>
    ///     Multiplier for total sentence time (used for circumstances).
    ///     1.0 is no change. 1.2 is +20%. 0.8 is -20%.
    /// </summary>
    [DataField("sentenceMultiplier")]
    public float SentenceMultiplier = 1.0f;
}

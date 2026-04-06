using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Laws;

/// <summary>
///     Defines a single corporate law or legal provision entry.
/// </summary>
[Prototype]
public sealed partial class CorporateLawPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

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
}

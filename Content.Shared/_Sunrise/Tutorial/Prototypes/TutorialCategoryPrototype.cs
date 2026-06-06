using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Sunrise.Tutorial.Prototypes;

/// <summary>
///     Prototype describing a tutorial category.
///     Contains a list of tutorial sequence prototypes that belong to this category.
/// </summary>
[Prototype]
public sealed partial class TutorialCategoryPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public string Name = string.Empty;

    /// <summary>
    ///     List of tutorial sequence prototypes included in this category.
    /// </summary>
    [DataField]
    public List<ProtoId<TutorialSequencePrototype>> Tutorials =[];
}

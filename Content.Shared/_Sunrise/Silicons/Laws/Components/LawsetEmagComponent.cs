using Content.Shared.Silicons.Laws;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Silicons.Laws.Components;

/// <summary>
/// Extra data for emags that can directly assign a silicon lawset.
/// </summary>
[RegisterComponent]
public sealed partial class LawsetEmagComponent : Component
{
    /// <summary>
    /// Lawset applied when this emag is used on a silicon.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<SiliconLawsetPrototype> Lawset = default!;

    /// <summary>
    /// Whether this emag can also rewrite law boards.
    /// </summary>
    [DataField]
    public bool AffectsLawboards = true;
}

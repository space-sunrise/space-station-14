using Content.Shared._Sunrise.QuickConstruction.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.QuickConstruction.Components;

/// <summary>
/// Allows opening a quick construction radial menu while interacting with this item in hand.
/// </summary>
[RegisterComponent]
public sealed partial class QuickConstructableComponent : Component
{
    [DataField(required: true)]
    public ProtoId<QuickConstructionCategoryPrototype> Category = default!;
}

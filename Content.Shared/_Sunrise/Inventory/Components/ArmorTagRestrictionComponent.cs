using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Inventory.Components;

/// <summary>
/// Ограничивает экипировку на цель, если у цели отсутствует нужный тег.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ArmorTagRestrictionComponent : Component
{
    /// <summary>
    /// Тег, который должен быть у цели, чтобы предмет можно было надеть.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<TagPrototype>? RequiredTag;
}

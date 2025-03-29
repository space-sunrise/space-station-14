using Content.Shared._Sunrise.BloodCult.UI;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.BloodCult.Items;

[RegisterComponent]
public sealed partial class RuneDrawerProviderComponent : Component
{
    [DataField]
    public List<EntProtoId> RunePrototypes = [];

    [ViewVariables]
    public Enum UserInterfaceKey = ListViewSelectorUiKey.Key;
}

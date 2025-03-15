using Content.Shared._Sunrise.BloodCult.UI;

namespace Content.Server._Sunrise.BloodCult.Runes.Comps;

[RegisterComponent]
public sealed partial class RuneDrawerProviderComponent : Component
{
    [DataField("runePrototypes")]
    public List<string> RunePrototypes = new();

    [ViewVariables]
    public Enum UserInterfaceKey = ListViewSelectorUiKey.Key;
}

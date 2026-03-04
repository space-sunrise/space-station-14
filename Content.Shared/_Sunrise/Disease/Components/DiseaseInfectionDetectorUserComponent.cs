using Content.Shared._Sunrise.TimeWindow;

namespace Content.Shared._Sunrise.Disease.Components;

[RegisterComponent]
public sealed partial class DiseaseInfectionDetectorUserComponent : Component
{
    [ViewVariables]
    public int Count;
}
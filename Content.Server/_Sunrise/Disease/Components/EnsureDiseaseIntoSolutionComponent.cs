// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using Content.Shared._Sunrise.Disease.Components;

namespace Content.Server._Sunrise.Disease.Components;

[RegisterComponent]
public sealed partial class EnsureDiseaseIntoSolutionComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    [DataField]
    public DiseaseData? Data = null;
}

// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

namespace Content.Server._Nox.Disease.Components;

/// <summary>
///     Логика резистов зомби инфекции.
/// </summary>
[RegisterComponent]
public sealed partial class DiseaseResistanceComponent : Component
{
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public float DiseaseResistanceCoefficient = 0.1f;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public LocId Examine = "disease-resistance-coefficient-value";
}

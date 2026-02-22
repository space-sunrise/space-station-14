// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

namespace Content.Server._Nox.Disease.Components;

[RegisterComponent]
public sealed partial class DiseaseImmunComponent : Component
{
    /// <summary>
    ///     Штаммы к которым у сущности есть иммунитет.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public List<string> StrainsId = new();

    [DataField]
    public bool ImmunAll = false;
}


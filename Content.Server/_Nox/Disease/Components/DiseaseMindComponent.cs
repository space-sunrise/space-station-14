// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using Content.Shared._Nox.Disease.Prototypes;

using Robust.Shared.Prototypes;

namespace Content.Server._Nox.Disease.Components;

[RegisterComponent]
public sealed partial class DiseaseMindComponent : Component
{
    /// <summary>
    ///     ID штамма.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public string StrainId = string.Empty;

    /// <summary>
    ///     Очки мутации, которые игрок может тратить на приобретение симптомов.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public int MutationPoints = 0;

    /// <summary>
    ///     Список активных симптомов для вируса.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public HashSet<ProtoId<DiseaseSymptomPrototype>> ActiveSymptoms = new();
}

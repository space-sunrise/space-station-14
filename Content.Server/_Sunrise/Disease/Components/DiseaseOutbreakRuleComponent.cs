// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using Content.Server._Sunrise.Disease.Systems;
using Content.Shared._Sunrise.Disease.Components;
using Content.Shared._Sunrise.Disease.Prototypes;

namespace Content.Server._Sunrise.Disease.Components;

[RegisterComponent, Access(typeof(DiseaseOutbreakRule))]
public sealed partial class DiseaseOutbreakRuleComponent : Component
{
    /// <summary>
    ///     Симптомы при случайном вирусе
    /// </summary>
    [DataField(required: true)]
    [ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<DangerIndicatorSymptom, int> SymptomsByDanger;

    /// <summary>
    ///     Количество тел при случайном вирусе
    /// </summary>
    [DataField(required: true)]
    [ViewVariables(VVAccess.ReadOnly)]
    public int BodyCount;

    /// <summary>
    ///     Количество первичных заражённых
    /// </summary>
    [DataField(required: true)]
    [ViewVariables(VVAccess.ReadOnly)]
    public int NumberPrimaryPacienst;

    /// <summary>
    ///     Если вирус не случайный
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public DiseaseData? Data;
}

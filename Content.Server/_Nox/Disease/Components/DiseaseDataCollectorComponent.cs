// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using Content.Shared._Nox.Disease.Components;

namespace Content.Server._Nox.Disease.Components;

[RegisterComponent]
public sealed partial class DiseaseDataCollectorComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public string DNA = String.Empty;

    /// <summary>
    ///     Длительность сбора материала.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public float Duration = 3f;

    /// <summary>
    ///     Требуемое расстояние до цели.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public float Distance = 1f;

    /// <summary>
    ///     Использован ли?
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsUsed = false;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public DiseaseData? Data = null;
}

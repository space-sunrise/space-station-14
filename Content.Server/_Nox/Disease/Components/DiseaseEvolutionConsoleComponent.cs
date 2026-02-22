// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using Content.Shared.DeviceLinking;
using Robust.Shared.Prototypes;

namespace Content.Server._Nox.Disease.Components;

[RegisterComponent]
public sealed partial class DiseaseEvolutionConsoleComponent : Component
{
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public ProtoId<SourcePortPrototype> DiseaseSolutionAnalyzerPort = "DiseaseSolutionAnalyzerSender";

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public ProtoId<SourcePortPrototype> DiseaseDiagnoserDataServerPort = "DiseaseDiagnoserDataServerSender";

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? DiseaseDiagnoserDataServer = null;

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? DiseaseSolutionAnalyzer = null;

    [DataField]
    public float MaxDistanceForDataServer = 50f;

    [DataField]
    public float MaxDistanceForOther = 4f;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool DataServerInRange = true;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool SolutionAnalyzerInRange = true;
}


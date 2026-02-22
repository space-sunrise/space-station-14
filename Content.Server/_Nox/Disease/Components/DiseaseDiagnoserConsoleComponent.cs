// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using Content.Shared.DeviceLinking;
using Robust.Shared.Prototypes;

namespace Content.Server._Nox.Disease.Components;

[RegisterComponent]
public sealed partial class DiseaseDiagnoserConsoleComponent : Component
{
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public ProtoId<SourcePortPrototype> DiseaseDiagnoserPort = "DiseaseDiagnoserSender";

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public ProtoId<SourcePortPrototype> DiseaseDiagnoserDataServerPort = "DiseaseDiagnoserDataServerSender";

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public ProtoId<SourcePortPrototype> DiseaseSolutionAnalyzerPort = "DiseaseSolutionAnalyzerSender";

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? DiseaseDiagnoser = null;

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
    public bool DiagnoserInRange = true;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool SolutionAnalyzerInRange = true;
}


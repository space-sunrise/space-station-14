// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using Content.Shared.DeviceLinking;
using Content.Shared._Sunrise.Disease;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.Disease.Components;

[RegisterComponent]
public sealed partial class DiseaseSolutionAnalyzerComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? ConnectedConsole = null;

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? ConnectedEvolutionConsole = null;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public ProtoId<SinkPortPrototype> DiseaseSolutionAnalyzerPort = "DiseaseSolutionAnalyzerReceiver";

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public SoundSpecifier? PrintingSound = new SoundPathSpecifier("/Audio/Machines/diagnoser_printing.ogg");

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public DiseaseSolutionAnalyzerStatus Status = DiseaseSolutionAnalyzerStatus.Off;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public SoundSpecifier? ScanningSound = new SoundPathSpecifier("/Audio/_Sunrise/Disease/Diagnoser/scanning.ogg");

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public SoundSpecifier? DenialSound = new SoundPathSpecifier("/Audio/_Sunrise/Disease/Diagnoser/denial.ogg");

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public SoundSpecifier? SuccessfullySound = new SoundPathSpecifier("/Audio/_Sunrise/Disease/Diagnoser/success.ogg");

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? CurrentSoundEntity = default!;
}

// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using Robust.Shared.Prototypes;
using Content.Shared.DeviceLinking;
using Robust.Shared.Audio;
using Content.Shared._Nox.Disease.Components;
using Content.Shared._Nox.TimeWindow;
using Content.Shared._Nox.Disease;

namespace Content.Server._Nox.Disease.Components;

[RegisterComponent]
public sealed partial class DiseaseDiagnoserComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? ConnectedConsole = null;

    /// <summary>
    ///     Длительность анимации печати отчёта. Костыль, но упрощает систему.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public TimedWindow AnimationWindow = new TimedWindow(TimeSpan.FromSeconds(4f), TimeSpan.FromSeconds(4f));

    /// <summary>
    ///     Данные которые печатаются в отчёт или генерируются в реагент.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public DiseaseData? DiseaseDataCPU = default!;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public ProtoId<SinkPortPrototype> DiseaseDiagnoserPort = "DiseaseDiagnoserReceiver";

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public EntProtoId Paper = "DiagnosisReportPaper";

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public DiseaseDiagnoserStatus Status = DiseaseDiagnoserStatus.Off;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public SoundSpecifier? PrintingSound = new SoundPathSpecifier("/Audio/Machines/diagnoser_printing.ogg");

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public SoundSpecifier? ScanningSound = new SoundPathSpecifier("/Audio/_Nox/Disease/Diagnoser/scanning.ogg");

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public SoundSpecifier? DenialSound = new SoundPathSpecifier("/Audio/_Nox/Disease/Diagnoser/denial.ogg");

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public SoundSpecifier? SuccessfullySound = new SoundPathSpecifier("/Audio/_Nox/Disease/Diagnoser/success.ogg");

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public SoundSpecifier? GenerateDiseaseSound = default!;

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? CurrentSoundEntity = default!;
}

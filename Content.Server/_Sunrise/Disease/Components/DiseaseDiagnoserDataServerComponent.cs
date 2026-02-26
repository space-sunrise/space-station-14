// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using Content.Shared._Sunrise.TimeWindow;
using Content.Shared._Sunrise.Disease.Components;
using Content.Shared.DeviceLinking;
using Content.Shared._Sunrise.Disease;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.Disease.Components;

[RegisterComponent]
public sealed partial class DiseaseDiagnoserDataServerComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? ConnectedConsole = null;

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? ConnectedEvolutionConsole = null;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public ProtoId<SinkPortPrototype> DiseaseDiagnoserDataServerPort = "DiseaseDiagnoserDataServerReceiver";

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<DiseaseStrainRecord, DiseaseData> StrainData = new();

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public EntProtoId Disk = "ResearchDisk";

    /// <summary>
    ///     Длительность обновления данных.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public TimedWindow UpdateWindow = new TimedWindow(TimeSpan.FromSeconds(1f), TimeSpan.FromSeconds(1f));

    /// <summary>
    ///     Множитель получаемых очков за каждый хранимый симптом.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public int SymptomsPointsMultiply = 2;

    /// <summary>
    ///     Множитель получаемых очков за каждое тело.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public int BodyPointsMultiply = 1;

    /// <summary>
    ///     Исследовательские очки.
    /// </summary>
    [DataField]
    public int Points = 0;
}

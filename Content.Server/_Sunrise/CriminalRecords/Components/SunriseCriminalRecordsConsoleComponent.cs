using Content.Shared._Sunrise.CriminalRecords;
using Content.Shared.StationRecords;

namespace Content.Server._Sunrise.CriminalRecords.Components;

[RegisterComponent]
public sealed partial class SunriseCriminalRecordsConsoleComponent : Component
{
    /// <summary>
    ///     Selected station record key for this console.
    ///     Shared by all users of this console.
    /// </summary>
    [ViewVariables]
    public StationRecordKey? SelectedKey;

    [ViewVariables]
    public SunriseCriminalRecordsUIState CurrentUIState = SunriseCriminalRecordsUIState.List;

    [ViewVariables]
    public uint? SelectedCaseId;
}

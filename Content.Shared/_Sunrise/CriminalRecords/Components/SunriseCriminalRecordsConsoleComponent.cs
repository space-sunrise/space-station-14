using Content.Shared._Sunrise.CriminalRecords;
using Content.Shared.StationRecords;
using Content.Shared._Sunrise.CriminalRecords.Systems;

namespace Content.Shared._Sunrise.CriminalRecords.Components;

[RegisterComponent]
[Access(typeof(SharedSunriseCriminalRecordsSystem))]
public sealed partial class SunriseCriminalRecordsConsoleComponent : Component
{
    /// <summary>
    ///     Selected station record key. Shared by all users.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public StationRecordKey? SelectedKey;

    /// <summary>
    ///     The current UI state. Shared by all users.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public SunriseCriminalRecordsUIState CurrentUIState = SunriseCriminalRecordsUIState.List;

    /// <summary>
    ///     The ID of the currently selected criminal case. Shared by all users.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public uint? SelectedCaseId;
}

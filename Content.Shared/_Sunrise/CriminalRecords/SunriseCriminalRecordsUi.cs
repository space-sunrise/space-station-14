using Robust.Shared.Serialization;
using Robust.Shared.Prototypes;
using Content.Shared.StationRecords;
using Content.Shared._Sunrise.Laws;

namespace Content.Shared._Sunrise.CriminalRecords;

/// <summary>
///     Key for the criminal records console UI.
/// </summary>
[Serializable, NetSerializable]
public enum SunriseCriminalRecordsConsoleKey : byte
{
    Key
}

/// <summary>
///     The current view/tab of the criminal records console.
/// </summary>
[Serializable, NetSerializable]
public enum SunriseCriminalRecordsUIState : byte
{
    List,
    Editor
}

/// <summary>
///     The state of a criminal records console, sent to the client.
/// </summary>
[Serializable, NetSerializable]
public sealed class SunriseCriminalRecordsConsoleState : BoundUserInterfaceState
{
    /// <summary>
    ///     List of all station records (Id -> Name).
    /// </summary>
    public readonly Dictionary<uint, string> Records;

    /// <summary>
    ///     List of criminal cases for the currently selected record.
    /// </summary>
    public readonly List<CriminalCase> Cases;

    /// <summary>
    ///     The currently selected station record ID.
    /// </summary>
    public readonly uint? SelectedStationRecord;

    /// <summary>
    ///     The currently selected case ID within the record.
    /// </summary>
    public readonly uint? SelectedCaseId;

    /// <summary>
    ///     The current UI sub-state (List or Editor).
    /// </summary>
    public readonly SunriseCriminalRecordsUIState CurrentUIState;

    // Person details
    /// <summary>The name of the currently selected person.</summary>
    public readonly string? SelectedName;
    /// <summary>The job title of the selected person.</summary>
    public readonly string? JobTitle;
    /// <summary>The job icon of the selected person.</summary>
    public readonly string? JobIcon;
    /// <summary>The age of the selected person.</summary>
    public readonly int? Age;
    /// <summary>The gender of the selected person.</summary>
    public readonly string? Gender;
    /// <summary>The species of the selected person.</summary>
    public readonly string? Species;
    /// <summary>The fingerprints of the selected person.</summary>
    public readonly string? Fingerprints;
    /// <summary>The DNA of the selected person.</summary>
    public readonly string? DNA;

    public SunriseCriminalRecordsConsoleState(
        Dictionary<uint, string> records,
        string? selectedName,
        List<CriminalCase> cases,
        uint? selectedStationRecord,
        uint? selectedCaseId,
        SunriseCriminalRecordsUIState currentState,
        string? jobTitle = null,
        string? jobIcon = null,
        int? age = null,
        string? gender = null,
        string? species = null,
        string? fingerprints = null,
        string? dna = null)
    {
        Records = new Dictionary<uint, string>(records);
        SelectedName = selectedName;
        Cases = new List<CriminalCase>(cases);
        SelectedStationRecord = selectedStationRecord;
        SelectedCaseId = selectedCaseId;
        CurrentUIState = currentState;

        JobTitle = jobTitle;
        JobIcon = jobIcon;
        Age = age;
        Gender = gender;
        Species = species;
        Fingerprints = fingerprints;
        DNA = dna;
    }
}

/// <summary>
///     BUI message to request creating a new open criminal case.
/// </summary>
[Serializable, NetSerializable]
public sealed class SunriseCriminalRecordsCreateCaseMessage : BoundUserInterfaceMessage
{
}

/// <summary>
///     BUI message to update an existing open criminal case.
/// </summary>
[Serializable, NetSerializable]
public sealed class SunriseCriminalRecordsUpdateCaseMessage : BoundUserInterfaceMessage
{
    /// <summary>The ID of the case to update.</summary>
    public readonly uint CaseId;
    /// <summary>List of law article IDs to assign to the case.</summary>
    public readonly List<ProtoId<CorporateLawPrototype>> Laws;
    /// <summary>List of circumstance IDs to assign to the case.</summary>
    public readonly List<ProtoId<CorporateLawPrototype>> Circumstances;
    /// <summary>Optional notes/description for the case.</summary>
    public readonly string? Notes;

    public SunriseCriminalRecordsUpdateCaseMessage(uint caseId, List<ProtoId<CorporateLawPrototype>> laws, List<ProtoId<CorporateLawPrototype>> circumstances, string? notes)
    {
        CaseId = caseId;
        Laws = new List<ProtoId<CorporateLawPrototype>>(laws);
        Circumstances = new List<ProtoId<CorporateLawPrototype>>(circumstances);
        Notes = notes;
    }
}

/// <summary>
///     BUI message to close an open criminal case.
/// </summary>
[Serializable, NetSerializable]
public sealed class SunriseCriminalRecordsCloseCaseMessage : BoundUserInterfaceMessage
{
    /// <summary>The ID of the case to close.</summary>
    public readonly uint CaseId;

    public SunriseCriminalRecordsCloseCaseMessage(uint caseId)
    {
        CaseId = caseId;
    }
}

/// <summary>
///     BUI message to change the current UI sub-state.
/// </summary>
[Serializable, NetSerializable]
public sealed class SunriseCriminalRecordsSetUIStateMessage : BoundUserInterfaceMessage
{
    /// <summary>The new UI state to switch to.</summary>
    public readonly SunriseCriminalRecordsUIState State;

    public SunriseCriminalRecordsSetUIStateMessage(SunriseCriminalRecordsUIState state)
    {
        State = state;
    }
}

/// <summary>
///     BUI message to select a specific case for editing/viewing.
/// </summary>
[Serializable, NetSerializable]
public sealed class SunriseCriminalRecordsSelectCaseMessage : BoundUserInterfaceMessage
{
    /// <summary>The ID of the case to select.</summary>
    public readonly uint CaseId;

    public SunriseCriminalRecordsSelectCaseMessage(uint caseId)
    {
        CaseId = caseId;
    }
}

/// <summary>
///     BUI message to send a full state refresh to a specific client.
///     Used for per-user UI projection.
/// </summary>
[Serializable, NetSerializable]
public sealed class SunriseCriminalRecordsRefreshMessage : BoundUserInterfaceMessage
{
    public readonly SunriseCriminalRecordsConsoleState State;

    public SunriseCriminalRecordsRefreshMessage(SunriseCriminalRecordsConsoleState state)
    {
        State = state;
    }
}

/// <summary>
///     BUI message to select a person's record to view their cases.
/// </summary>
[Serializable, NetSerializable]
public sealed class SunriseCriminalRecordsSelectRecordMessage : BoundUserInterfaceMessage
{
    /// <summary>The station record ID of the person. If null, deselects the current record.</summary>
    public readonly uint? RecordId;

    public SunriseCriminalRecordsSelectRecordMessage(uint? recordId)
    {
        RecordId = recordId;
    }
}

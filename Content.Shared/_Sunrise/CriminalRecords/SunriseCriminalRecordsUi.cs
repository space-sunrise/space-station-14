using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.CriminalRecords;

[Serializable, NetSerializable]
public enum SunriseCriminalRecordsConsoleKey : byte
{
    Key
}

[Serializable, NetSerializable]
public enum SunriseCriminalRecordsUIState : byte
{
    List,
    Editor
}

[Serializable, NetSerializable]
public sealed class SunriseCriminalRecordsConsoleState : BoundUserInterfaceState
{
    public readonly Dictionary<uint, string> Records;
    public readonly List<CriminalCase> Cases;
    public readonly uint? SelectedStationRecord;
    public readonly uint? SelectedCaseId;
    public readonly SunriseCriminalRecordsUIState CurrentUIState;

    // Person details
    public readonly string? SelectedName;
    public readonly string? JobTitle;
    public readonly string? JobIcon;
    public readonly int? Age;
    public readonly string? Gender;
    public readonly string? Species;
    public readonly string? Fingerprints;
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
        Records = records;
        SelectedName = selectedName;
        Cases = cases;
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

[Serializable, NetSerializable]
public sealed class SunriseCriminalRecordsCreateCaseMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class SunriseCriminalRecordsUpdateCaseMessage : BoundUserInterfaceMessage
{
    public readonly uint CaseId;
    public readonly List<string> Laws;
    public readonly List<string> Circumstances;
    public readonly string? Notes;

    public SunriseCriminalRecordsUpdateCaseMessage(uint caseId, List<string> laws, List<string> circumstances, string? notes)
    {
        CaseId = caseId;
        Laws = laws;
        Circumstances = circumstances;
        Notes = notes;
    }
}

[Serializable, NetSerializable]
public sealed class SunriseCriminalRecordsCloseCaseMessage : BoundUserInterfaceMessage
{
    public readonly uint CaseId;

    public SunriseCriminalRecordsCloseCaseMessage(uint caseId)
    {
        CaseId = caseId;
    }
}

[Serializable, NetSerializable]
public sealed class SunriseCriminalRecordsSetUIStateMessage : BoundUserInterfaceMessage
{
    public readonly SunriseCriminalRecordsUIState State;

    public SunriseCriminalRecordsSetUIStateMessage(SunriseCriminalRecordsUIState state)
    {
        State = state;
    }
}

[Serializable, NetSerializable]
public sealed class SunriseCriminalRecordsSelectCaseMessage : BoundUserInterfaceMessage
{
    public readonly uint CaseId;

    public SunriseCriminalRecordsSelectCaseMessage(uint caseId)
    {
        CaseId = caseId;
    }
}

[Serializable, NetSerializable]
public sealed class SunriseCriminalRecordsSelectRecordMessage : BoundUserInterfaceMessage
{
    public readonly uint? RecordId;

    public SunriseCriminalRecordsSelectRecordMessage(uint? recordId)
    {
        RecordId = recordId;
    }
}

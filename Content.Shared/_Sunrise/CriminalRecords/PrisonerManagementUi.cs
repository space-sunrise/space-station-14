using Robust.Shared.Serialization;
using Content.Shared.StationRecords;

namespace Content.Shared._Sunrise.CriminalRecords;

[Serializable, NetSerializable]
public enum PrisonerManagementConsoleKey : byte
{
    Key
}

/// <summary>
///     The state of a prisoner management console, sent to the client.
/// </summary>
[Serializable, NetSerializable]
public sealed class PrisonerManagementConsoleState : BoundUserInterfaceState
{
    /// <summary>Cases waiting for incarceration to begin.</summary>
    public readonly List<PrisonerRecordInfo> Waiting;
    /// <summary>Active incarcerations currently in progress.</summary>
    public readonly List<IncarcerationInfo> InProgress;
    /// <summary>Completed or paroled cases.</summary>
    public readonly List<PrisonerRecordInfo> Finished;
    /// <summary>Mapping of cell indices to their occupancy status.</summary>
    public readonly Dictionary<int, bool> CellOccupied;
    /// <summary>Mapping of cell indices to whether they are fully equipped.</summary>
    public readonly Dictionary<int, bool> CellEquipped;
    /// <summary>The threshold in minutes at which a sentence becomes permanent.</summary>
    public readonly int PermanentThreshold;

    public PrisonerManagementConsoleState(
        List<PrisonerRecordInfo> waiting,
        List<IncarcerationInfo> inProgress,
        List<PrisonerRecordInfo> finished,
        Dictionary<int, bool> cellOccupied,
        Dictionary<int, bool> cellEquipped,
        int permanentThreshold)
    {
        Waiting = waiting;
        InProgress = inProgress;
        Finished = finished;
        CellOccupied = cellOccupied;
        CellEquipped = cellEquipped;
        PermanentThreshold = permanentThreshold;
    }
}

/// <summary>
///     Summary information for a criminal record in the prisoner management system.
/// </summary>
[Serializable, NetSerializable]
public record struct PrisonerRecordInfo(
    uint RecordId, 
    string Name, 
    string Job, 
    uint CaseId, 
    int Sentence, 
    bool IsParoled = false, 
    bool IsWarning = false,
    List<SentenceBreakdownEntry>? SentenceBreakdown = null);

[Serializable, NetSerializable]
public record struct IncarcerationInfo(uint RecordId, string Name, uint CaseId, int CellIndex, TimeSpan StartTime, int Sentence);

[Serializable, NetSerializable]
public sealed class PrisonerManagementStartIncarcerationMessage : BoundUserInterfaceMessage
{
    public readonly uint RecordId;
    public readonly uint CaseId;
    public readonly int CellIndex;

    public PrisonerManagementStartIncarcerationMessage(uint recordId, uint caseId, int cellIndex)
    {
        RecordId = recordId;
        CaseId = caseId;
        CellIndex = cellIndex;
    }
}

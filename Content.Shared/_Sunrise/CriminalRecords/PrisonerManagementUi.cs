using Robust.Shared.Serialization;
using Content.Shared.StationRecords;

namespace Content.Shared._Sunrise.CriminalRecords;

[Serializable, NetSerializable]
public enum PrisonerManagementConsoleKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class PrisonerManagementConsoleState : BoundUserInterfaceState
{
    public readonly List<PrisonerRecordInfo> Waiting;
    public readonly List<IncarcerationInfo> InProgress;
    public readonly List<PrisonerRecordInfo> Finished;
    public readonly Dictionary<int, bool> CellOccupied;
    public readonly Dictionary<int, bool> CellEquipped;
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

[Serializable, NetSerializable]
public record struct PrisonerRecordInfo(uint RecordId, string Name, string Job, uint CaseId, int Sentence);

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

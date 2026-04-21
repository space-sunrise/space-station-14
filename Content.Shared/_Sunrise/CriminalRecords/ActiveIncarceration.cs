using Content.Shared.StationRecords;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.CriminalRecords;

[DataDefinition]
public sealed partial class ActiveIncarceration
{
    [DataField]
    public StationRecordKey RecordKey;

    [DataField]
    public uint CaseId;

    [DataField]
    public string PrisonerAccessId = string.Empty;

    [DataField]
    public TimeSpan StartTime;

    [DataField]
    public int SentenceMinutes;
}

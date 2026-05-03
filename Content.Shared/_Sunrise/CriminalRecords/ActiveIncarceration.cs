using Content.Shared.StationRecords;
using Robust.Shared.Serialization;
using Robust.Shared.GameObjects;

namespace Content.Shared._Sunrise.CriminalRecords;

[DataDefinition]
public sealed partial class ActiveIncarceration
{
    [DataField]
    public StationRecordKey RecordKey = default;

    [DataField]
    public uint CaseId = 0;

    [DataField]
    public string PrisonerAccessId = string.Empty;

    [DataField]
    public TimeSpan StartTime = TimeSpan.Zero;

    [DataField]
    public int SentenceMinutes = 0;
}

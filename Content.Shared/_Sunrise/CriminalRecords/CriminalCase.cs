using Robust.Shared.Serialization;
using Robust.Shared.Prototypes;
using Content.Shared._Sunrise.Laws;
using System.Linq;
using System.Collections.Generic;

namespace Content.Shared._Sunrise.CriminalRecords;

[Serializable, NetSerializable]
public enum CriminalCaseStatus : byte
{
    Open,
    Closed,
    Incarcerated,
    Finished
}

[Serializable, NetSerializable]
public sealed class SentenceBreakdownEntry
{
    public string LocId = string.Empty;
    public Dictionary<string, object>? Args;

    public SentenceBreakdownEntry() { }

    public SentenceBreakdownEntry(string locId, params (string, object)[] args)
    {
        LocId = locId;
        Args = args.ToDictionary(x => x.Item1, x => x.Item2);
    }
}

[Serializable, NetSerializable]
public sealed class CriminalCase
{
    public uint Id;

    [ViewVariables]
    public NetEntity? OriginStation;

    [ViewVariables]
    public List<ProtoId<CorporateLawPrototype>> Laws = new();

    [ViewVariables]
    public List<ProtoId<CorporateLawPrototype>> Circumstances = new();

    [ViewVariables]
    public CriminalCaseStatus Status = CriminalCaseStatus.Open;

    [ViewVariables]
    public string? Notes;

    [ViewVariables]
    public bool IsParoled;

    [ViewVariables]
    public bool IsWarning;

    [ViewVariables]
    public List<SentenceBreakdownEntry> SentenceBreakdown = new();

    [ViewVariables]
    public TimeSpan CreationTime;

    /// <summary>
    /// Calculated sentence in minutes.
    /// </summary>
    [ViewVariables]
    public int CalculatedSentence;

    /// <summary>
    /// When the incarceration started.
    /// </summary>
    [ViewVariables]
    public TimeSpan? IncarcerationStartTime;

    public CriminalCase(uint id, TimeSpan creationTime)
    {
        Id = id;
        CreationTime = creationTime;
    }
}

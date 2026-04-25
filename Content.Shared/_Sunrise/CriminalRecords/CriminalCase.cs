using Robust.Shared.Serialization;
using Robust.Shared.Prototypes;
using Content.Shared._Sunrise.Laws;

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

using Robust.Shared.Serialization;

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
    [ViewVariables]
    public uint Id;

    [ViewVariables]
    public List<string> Laws = new();

    [ViewVariables]
    public List<string> Circumstances = new();

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

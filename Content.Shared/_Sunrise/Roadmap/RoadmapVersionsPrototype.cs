using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Roadmap;

[Prototype("roadmapVersions")]
public sealed class RoadmapVersionsPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = default!;
    [DataField]
    public int Year { get; set; } = 1984;

    [DataField]
    public Dictionary<string, RoadmapGroup> Versions = new();
}

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class RoadmapGroup
{
    [DataField]
    public string Name { get; set; } = string.Empty;

    [DataField]
    public Dictionary<string, RoadmapGoal> Goals = new();
}

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class RoadmapGoal
{
    [DataField]
    public string Name { get; set; } = string.Empty;

    [DataField]
    public string Desc { get; set; } = string.Empty;

    [DataField]
    public RoadmapItemState State { get; set; } = RoadmapItemState.Planned;
}

[Serializable, NetSerializable]
public enum RoadmapItemState
{
    Planned,
    InProgress,
    Partial,
    Complete
}

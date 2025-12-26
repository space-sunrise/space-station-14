using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Roadmap;

[Prototype]
public sealed class RoadmapVersionsPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = default!;
    [DataField]
    public string Fork { get; set; } = "SUNRISE";

    [DataField]
    public List<RoadmapGroup> Versions = [];
}

[DataDefinition]
public partial record struct RoadmapGroup
{
    [DataField] public string Name;

    [DataField] public List<RoadmapGoal> Goals = [];
}

[DataDefinition]
public partial record struct RoadmapGoal
{
    [DataField] public string Name;

    [DataField] public string Desc;

    [DataField] public RoadmapItemState State = RoadmapItemState.Planned;
}

[Serializable, NetSerializable]
public enum RoadmapItemState
{
    Planned,
    InProgress,
    Partial,
    Complete,
}

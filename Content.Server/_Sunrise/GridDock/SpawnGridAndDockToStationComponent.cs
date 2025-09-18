using Robust.Shared.Utility;

namespace Content.Server._Sunrise.GridDock;

[RegisterComponent]
public sealed partial class SpawnGridAndDockToStationComponent : Component
{
    [DataField(required: true)]
    public List<GridDockEntry> Grids { get; set; } = new();
}

[DataDefinition]
public sealed partial class GridDockEntry
{
    [DataField(required: true)]
    public ResPath GridPath;

    [DataField(required: true)]
    public string PriorityTag;
}

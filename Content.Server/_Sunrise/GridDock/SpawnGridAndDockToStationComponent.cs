using Robust.Shared.Utility;

namespace Content.Server._Sunrise.RoundStartFtl;

[RegisterComponent]
public sealed partial class SpawnGridAndDockToStationComponent : Component
{
    [DataField]
    public ResPath? GridPath { get; set; }

    [DataField(required: true)]
    public string PriorityTag;
}

using Content.Shared.Actions;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.Boss.Components;

[DataDefinition]
public sealed partial class UseActionWhenTargetInRange
{
    [DataField]
    public EntityUid? ActionEnt;

    [DataField("actionId")]
    public string ActionId;

    [DataField]
    public float? MaxRange = 10f;

    [DataField]
    public float? MinRange = 5f;
}

[RegisterComponent]
public sealed partial class NPCUseActionWhenTargetInRangeComponent : Component
{
    [DataField]
    public List<UseActionWhenTargetInRange> Actions = new();

    [DataField]
    public TimeSpan? Delay = TimeSpan.FromSeconds(1);

    [DataField]
    public TimeSpan? Prev = TimeSpan.Zero;

    /// <summary>
    ///     HTN blackboard key for the target entity
    /// </summary>
    [DataField]
    public string TargetKey = "Target";
}

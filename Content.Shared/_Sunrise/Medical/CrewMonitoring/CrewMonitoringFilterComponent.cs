using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared.Medical.CrewMonitoring;

[RegisterComponent]
public sealed partial class CrewMonitoringFilterComponent : Component
{
    /// <summary>
    /// Health states allowed by this monitor. Empty means all states are allowed.
    /// </summary>
    [DataField]
    public List<CrewMonitoringHealthState> AllowedHealthStates = [];

    /// <summary>
    /// Departments allowed by this monitor. Empty means all departments are allowed.
    /// </summary>
    [DataField]
    public List<ProtoId<DepartmentPrototype>> AllowedDepartmentIds = [];

    /// <summary>
    /// Whether implant trackers are shown by this monitor.
    /// </summary>
    [DataField]
    public bool IncludeTrackers;
}

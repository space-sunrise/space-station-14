using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared.Medical.CrewMonitoring;

[RegisterComponent]
public sealed partial class CrewMonitoringFilterComponent : Component
{
    /// <summary>
    /// Разрешённые состояния здоровья. Если пусто – отображаются все.
    /// </summary>
    [DataField]
    public List<CrewMonitoringHealthState> AllowedHealthStates = [];

    /// <summary>
    /// Разрешенные отделы. Если пусто – все доступны.
    /// </summary>
    [DataField]
    public List<ProtoId<DepartmentPrototype>> AllowedDepartmentIds = [];

    /// <summary>
    /// Будут ли отображаться трекеры (импланты).
    /// </summary>
    [DataField]
    public bool IncludeTrackers;
}

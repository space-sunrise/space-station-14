namespace Content.Shared.Medical.CrewMonitoring;

[RegisterComponent]
public sealed partial class CrewMonitoringFilterComponent : Component
{
    /// <summary>
    /// Разрешенные отделы. Если пустое все доступны
    /// </summary>
    [DataField]
    public List<string> AllowedDepartmentIds = new();
    /// <summary>
    /// Будут ли отображаться по трекерам
    /// </summary>
    [DataField]
    public bool IncludeTrackers;
    /// <summary>
    /// Показывать ли мертвых, в крите, в ужасном
    /// </summary>
    [DataField]
    public bool OnlyShowWoundedOrDead;
}


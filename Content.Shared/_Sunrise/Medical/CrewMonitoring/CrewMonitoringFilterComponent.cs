using Robust.Shared.GameStates;

namespace Content.Shared.Medical.CrewMonitoring;

/// <summary>
/// Режимы фильтрации мониторинга экипажа.
/// </summary>
public enum CrewMonitoringFilterMode
{
    /// <summary>
    /// Показывать всех (без фильтрации).
    /// </summary>
    All,

    /// <summary>
    /// Только здоровые (урон 0% – 13.2%).
    /// </summary>
    HealthyOnly,

    /// <summary>
    /// Только состояния "хорошо" и "не очень" (урон 13.2% – 60%).
    /// </summary>
    GoodAndNotGreat,

    /// <summary>
    /// Только состояния "плохо" и "ужасно" (урон 60% – 100%).
    /// </summary>
    BadAndTerrible,

    /// <summary>
    /// Только мёртвые и критические (урон >= 100% + мёртвые).
    /// </summary>
    DeadAndCritical,

    /// <summary>
    /// Все раненые (Good, NotGreat, Bad, Terrible, Critical) – без здоровых и мёртвых.
    /// </summary>
    Wounded,

    /// <summary>
    /// Все пострадавшие (Bad, Terrible, Critical, Dead) – для медиков.
    /// </summary>
    Casualties
}

[RegisterComponent]
public sealed partial class CrewMonitoringFilterComponent : Component
{
    /// <summary>
    /// Режим фильтрации отображаемых существ.
    /// </summary>
    [DataField("mode")]
    public CrewMonitoringFilterMode Mode = CrewMonitoringFilterMode.All;

    /// <summary>
    /// Разрешенные отделы. Если пусто – все доступны.
    /// </summary>
    [DataField("allowedDepartmentIds")]
    public List<string> AllowedDepartmentIds = new();

    /// <summary>
    /// Будут ли отображаться трекеры (импланты).
    /// </summary>
    [DataField("includeTrackers")]
    public bool IncludeTrackers;
}

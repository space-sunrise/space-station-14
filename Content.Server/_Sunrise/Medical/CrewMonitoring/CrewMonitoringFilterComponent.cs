using Content.Shared.Medical.SuitSensor;

namespace Content.Server._Sunrise.Medical.CrewMonitoring;

/// <summary>
/// Серверный фильтр для ручного мониторинга экипажа
/// Применяется перед отправкой <see cref="SuitSensorStatus"/> на клиент
/// Сделано против читеров
/// </summary>
[RegisterComponent]
public sealed partial class CrewMonitoringFilterComponent : Component
{
    /// <summary>
    /// Id отделов, которые разрешено показывать. Пустой список = без фильтра по отделам
    /// </summary>
    [DataField]
    public List<string> AllowedDepartmentIds = new();

    /// <summary>
    /// Добавлять в выдачу трекеры
    /// </summary>
    [DataField]
    public bool IncludeTrackers;

    /// <summary>
    /// Показывать только тех, кто в крите или мертв
    /// </summary>
    [DataField]
    public bool OnlyShowWoundedOrDead;
}

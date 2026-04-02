using Content.Shared.Implants.Components;
using Content.Shared.Medical.CrewMonitoring;
using Content.Shared.Medical.SuitSensor;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.Medical.CrewMonitoring;

public sealed partial class CrewMonitoringConsoleSystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private void ApplyFilter(EntityUid uid, ref List<SuitSensorStatus> sensors)
    {
        if (!TryComp(uid, out CrewMonitoringFilterComponent? filter))
            return;

        // Если ни один фильтр не включён – показываем всех (или можно ничего не показывать – решайте)
        bool hasFilter = filter.ShowHealthy || filter.ShowGood || filter.ShowNotGreat ||
                         filter.ShowBad || filter.ShowTerrible || filter.ShowCritical || filter.ShowDead;
        bool filterByDepartment = filter.AllowedDepartmentIds.Count > 0;

        if (!hasFilter && !filterByDepartment)
            return;

        HashSet<string>? allowedDepartmentNames = null;
        if (filterByDepartment)
            allowedDepartmentNames = BuildAllowedDepartmentNameSet(filter.AllowedDepartmentIds);

        var includeTrackers = filter.IncludeTrackers;
        var filteredSensors = new List<SuitSensorStatus>(sensors.Count);

        foreach (var sensor in sensors)
        {
            // Проверка соответствия состояния здоровья включённым фильтрам
            bool healthMatches = false;

            if (sensor.IsAlive == false && filter.ShowDead)
                healthMatches = true;
            else if (sensor.IsAlive)
            {
                float damage = sensor.DamagePercentage ?? 0f;
                HealthState state = GetHealthState(damage);

                healthMatches = state switch
                {
                    HealthState.Healthy when filter.ShowHealthy => true,
                    HealthState.Good when filter.ShowGood => true,
                    HealthState.NotGreat when filter.ShowNotGreat => true,
                    HealthState.Bad when filter.ShowBad => true,
                    HealthState.Terrible when filter.ShowTerrible => true,
                    HealthState.Critical when filter.ShowCritical => true,
                    _ => false
                };
            }

            if (!healthMatches)
                continue;

            // Фильтр по отделам
            if (allowedDepartmentNames != null && !IsInAllowedDepartments(sensor, allowedDepartmentNames, includeTrackers))
                continue;

            filteredSensors.Add(sensor);
        }

        sensors = filteredSensors;
    }

    private enum HealthState
    {
        Healthy,    // 0% – 13.2%
        Good,       // 13.2% – 36%
        NotGreat,   // 36% – 60%
        Bad,        // 60% – 83%
        Terrible,   // 83% – 100%
        Critical    // >= 100%
    }

    private HealthState GetHealthState(float damagePercentage)
    {
        if (damagePercentage < 0.132f)
            return HealthState.Healthy;
        if (damagePercentage < 0.36f)
            return HealthState.Good;
        if (damagePercentage < 0.6f)
            return HealthState.NotGreat;
        if (damagePercentage < 0.83f)
            return HealthState.Bad;
        if (damagePercentage < 1.0f)
            return HealthState.Terrible;
        return HealthState.Critical;
    }

    private HashSet<string> BuildAllowedDepartmentNameSet(List<string> departmentIds)
    {
        var allowedDepartments = new HashSet<string>();
        foreach (var departmentId in departmentIds)
        {
            if (_prototypeManager.TryIndex<DepartmentPrototype>(departmentId, out var department))
                allowedDepartments.Add(Loc.GetString(department.Name));
            else
                allowedDepartments.Add(departmentId);
        }
        return allowedDepartments;
    }

    private bool IsInAllowedDepartments(SuitSensorStatus sensor, HashSet<string> allowedDepartmentNames, bool includeTrackers)
    {
        foreach (var department in sensor.JobDepartments)
        {
            if (allowedDepartmentNames.Contains(department))
                return true;
        }

        if (!includeTrackers)
            return false;

        var sensorEntity = GetEntity(sensor.SuitSensorUid);
        return HasComp<SubdermalImplantComponent>(sensorEntity);
    }
}

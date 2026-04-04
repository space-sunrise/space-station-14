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

        var filterByDepartment = filter.AllowedDepartmentIds.Count > 0;
        var includeTrackers = filter.IncludeTrackers;

        var activeStates = new HashSet<HealthState>();

        if (filter.ShowHealthy)
            activeStates.Add(HealthState.Healthy);
        if (filter.ShowGood)
            activeStates.Add(HealthState.Good);
        if (filter.ShowNotGreat)
            activeStates.Add(HealthState.NotGreat);
        if (filter.ShowBad)
            activeStates.Add(HealthState.Bad);
        if (filter.ShowTerrible)
            activeStates.Add(HealthState.Terrible);
        if (filter.ShowCritical)
            activeStates.Add(HealthState.Critical);
        if (filter.ShowDead)
            activeStates.Add(HealthState.Dead);
        if (filter.ShowUnknown)
            activeStates.Add(HealthState.Unknown);

        if (activeStates.Count == 0 && !filterByDepartment)
            return;

        HashSet<string>? allowedDepartmentNames = null;
        if (filterByDepartment)
            allowedDepartmentNames = BuildAllowedDepartmentNameSet(filter.AllowedDepartmentIds);

        var filteredSensors = new List<SuitSensorStatus>(sensors.Count);

        foreach (var sensor in sensors)
        {
            var healthState = HealthStateHelper.GetHealthState(sensor.DamagePercentage, sensor.IsAlive);

            if (activeStates.Count > 0 && !activeStates.Contains(healthState))
                continue;

            if (allowedDepartmentNames != null && !IsInAllowedDepartments(sensor, allowedDepartmentNames, includeTrackers))
                continue;

            filteredSensors.Add(sensor);
        }

        sensors = filteredSensors;
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

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

        var showOnlyWoundedOrDead = filter.OnlyShowWoundedOrDead;
        var filterByDepartment = filter.AllowedDepartmentIds.Count > 0;

        if (!showOnlyWoundedOrDead && !filterByDepartment)
            return;

        HashSet<string>? allowedDepartmentNames = null;
        if (filterByDepartment)
            allowedDepartmentNames = BuildAllowedDepartmentNameSet(filter.AllowedDepartmentIds);

        var includeTrackers = filter.IncludeTrackers;
        var filteredSensors = new List<SuitSensorStatus>(sensors.Count);
        foreach (var sensor in sensors)
        {
            if (showOnlyWoundedOrDead && !IsWoundedOrDead(sensor))
                continue;

            if (allowedDepartmentNames != null)
            {
                if (!IsInAllowedDepartments(sensor, allowedDepartmentNames, includeTrackers))
                    continue;
            }

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

    private bool IsWoundedOrDead(SuitSensorStatus sensor)
    {
        if (!sensor.IsAlive)
            return true;

        return sensor.DamagePercentage is >= CriticalDamagePercentage;
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

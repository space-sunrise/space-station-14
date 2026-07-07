using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Roles;

public static class CentCommJobHelper
{
    private const string CentralCommandDepartment = "CentralCommand";

    public static bool IsCentCommJob(IPrototypeManager prototypeManager, string jobId)
    {
        return prototypeManager.TryIndex<DepartmentPrototype>(CentralCommandDepartment, out var department)
            && department.Roles.Contains(jobId);
    }

    public static bool IsCentCommJob(IPrototypeManager prototypeManager, ProtoId<JobPrototype> jobId)
    {
        return prototypeManager.TryIndex<DepartmentPrototype>(CentralCommandDepartment, out var department)
            && department.Roles.Contains(jobId);
    }
}

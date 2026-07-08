using System.Collections.Generic;
using System.Linq;
using Content.Shared._Sunrise.Roles;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared.Roles;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.StationCentComm;

public sealed partial class StationCentCommSystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public bool IsCentCommEnabled()
    {
        return _cfg.GetCVar(SunriseCCVars.CentCommEnabled);
    }

    public bool IsCentCommJob(string jobId)
    {
        return CentCommJobHelper.IsCentCommJob(_prototypeManager, jobId);
    }

    public bool FilterDisabledCentCommJobs(Dictionary<ProtoId<JobPrototype>, int?> jobs)
    {
        if (IsCentCommEnabled())
            return false;

        var removed = false;
        foreach (var jobId in jobs.Keys.ToArray())
        {
            if (!IsCentCommJob(jobId))
                continue;

            jobs.Remove(jobId);
            removed = true;
        }

        return removed;
    }
}

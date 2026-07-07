using Content.Server._Sunrise.StationCentComm;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Server.Station.Systems;

public sealed partial class StationJobsSystem
{
    [Dependency] private readonly StationCentCommSystem _sunriseCentComm = default!;

    partial void InitializeStationJobsPortal()
    {
        Subs.CVar(_configurationManager, SunriseCCVars.CentCommEnabled, _ => UpdateJobsAvailable(), true);
    }

    partial void FilterJobsAvailablePortal(Dictionary<ProtoId<JobPrototype>, int?> jobs, ref bool skipStation)
    {
        skipStation = _sunriseCentComm.FilterDisabledCentCommJobs(jobs) && jobs.Count == 0;
    }
}

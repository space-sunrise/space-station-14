using Content.Server._Sunrise.GameTicking.PlayerJoinableMaps;
using Content.Server.GameTicking;
using Content.Shared._Sunrise.GameTicking.PlayerJoinableMaps;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

#pragma warning disable IDE0130 // Namespace не соответствует папке из-за partial-портала.
namespace Content.Server.Station.Systems;

public sealed partial class StationJobsSystem
{
    [Dependency] private readonly PlayerJoinableMapSystem _playerJoinableMap = default!;

    partial void FilterRoundStartJobsPortal(EntityUid station, Dictionary<ProtoId<JobPrototype>, int?> jobs);

    partial void FilterJobsAvailablePortal(EntityUid station, Dictionary<ProtoId<JobPrototype>, int?> jobs, ref bool skipStation)
    {
        _playerJoinableMap.FilterAvailableJobs((station, null), jobs, PlayerJoinKind.LateJoin);
        skipStation = jobs.Count == 0 && _playerJoinableMap.HasAnyPlayerJoinableJob(GetJobs(station));
    }

    partial void FilterRoundStartJobsPortal(EntityUid station, Dictionary<ProtoId<JobPrototype>, int?> jobs)
    {
        _playerJoinableMap.FilterAvailableJobs((station, null), jobs, PlayerJoinKind.RoundStart);
    }
}

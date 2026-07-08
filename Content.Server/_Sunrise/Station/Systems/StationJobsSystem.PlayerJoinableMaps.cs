using Content.Server._Sunrise.GameTicking.PlayerJoinableMaps;
using Content.Shared._Sunrise.GameTicking.PlayerJoinableMaps;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

#pragma warning disable IDE0130 // Namespace не соответствует папке из-за partial-портала.
namespace Content.Server.Station.Systems;

public sealed partial class StationJobsSystem
{
    [Dependency] private readonly PlayerJoinableMapSystem _playerJoinableMaps = default!;

    partial void InitializeStationJobsPortal()
    {
        foreach (var map in _prototypeManager.EnumeratePrototypes<PlayerJoinableMapPrototype>())
        {
            if (map.PlayerAccessEnabledCVar == null)
                continue;

            Subs.CVar<bool>(_configurationManager, map.PlayerAccessEnabledCVar, _ => UpdateJobsAvailable(), true);
        }
    }

    partial void FilterJobsAvailablePortal(EntityUid station, Dictionary<ProtoId<JobPrototype>, int?> jobs, ref bool skipStation)
    {
        _playerJoinableMaps.FilterAvailableJobs((station, null), jobs, PlayerJoinKind.LateJoin);
        skipStation = jobs.Count == 0 && _playerJoinableMaps.HasAnyPlayerJoinableJob(GetJobs(station));
    }

    partial void FilterRoundStartJobSelectionPortal(
        EntityUid station,
        Dictionary<ProtoId<JobPrototype>, int?> jobs)
    {
        _playerJoinableMaps.FilterAvailableJobs((station, null), jobs, PlayerJoinKind.RoundStart);
    }
}

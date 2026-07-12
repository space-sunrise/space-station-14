using Content.Server._Sunrise.GameTicking.PlayerJoinableMaps;
using Content.Server.GameTicking;
using Content.Shared._Sunrise.GameTicking.PlayerJoinableMaps;
using Content.Shared.Roles;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

#pragma warning disable IDE0130 // Namespace не соответствует папке из-за partial-портала.
namespace Content.Server.Station.Systems;

public sealed partial class StationJobsSystem
{
    [Dependency] private readonly PlayerJoinableMapSystem _playerJoinableMap = default!;

    partial void InitializeStationJobsPortal()
    {
        foreach (var map in _prototypeManager.EnumeratePrototypes<PlayerJoinableMapPrototype>())
        {
            SubscribePlayerJoinableMapAccessCVar(PlayerJoinableMapAccess.GetEnabledCVar(map));
            SubscribePlayerJoinableMapAccessCVar(PlayerJoinableMapAccess.GetMinPlayersCVar(map));
        }
    }

    partial void FilterJobsAvailablePortal(EntityUid station, Dictionary<ProtoId<JobPrototype>, int?> jobs, ref bool skipStation)
    {
        _playerJoinableMap.FilterAvailableJobs((station, null), jobs, PlayerJoinKind.LateJoin);
        skipStation = jobs.Count == 0 && _playerJoinableMap.HasAnyPlayerJoinableJob(GetJobs(station));
    }

    partial void FilterRoundStartJobSelectionPortal(
        EntityUid station,
        Dictionary<ProtoId<JobPrototype>, int?> jobs)
    {
        _playerJoinableMap.FilterAvailableJobs((station, null), jobs, PlayerJoinKind.RoundStart);
    }

    private void SubscribePlayerJoinableMapAccessCVar<T>(CVarDef<T>? cvar)
        where T : notnull
    {
        if (cvar == null)
            return;

        Subs.CVar<T>(_configurationManager, cvar, _ => UpdateJobsAvailable(), true);
    }

}

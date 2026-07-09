using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Sunrise.Interfaces.Shared;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

#pragma warning disable IDE0130 // Namespace не соответствует папке из-за partial-портала.
namespace Content.Server.Station.Systems;

public sealed partial class StationJobsSystem
{
    private ISharedSponsorsManager? _sponsorsManager;

    partial void InitializeRoundStartPortal()
    {
        IoCManager.Instance!.TryResolveType(out _sponsorsManager);
    }

    partial void PickRoundStartRoleSessionPortal(
        HashSet<NetUserId> players,
        ProtoId<JobPrototype> job,
        ref NetUserId? player,
        ref bool handled)
    {
        if (_sponsorsManager == null)
            return;

        player = _sponsorsManager.PickRoleSession(players, job);
        handled = true;
    }

    partial void FilterJobCandidatePortal(JobPrototype job, HumanoidCharacterProfile profile, ref bool canUseJob)
    {
        if (!canUseJob)
            return;

        if (job.SpeciesBlacklist.Contains(profile.Species))
            canUseJob = false;
    }
}

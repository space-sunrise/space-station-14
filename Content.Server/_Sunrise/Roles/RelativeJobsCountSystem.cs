using Content.Server.Station.Systems;
using Content.Shared.GameTicking;

namespace Content.Server._Sunrise.Roles;

public sealed class RelativeJobsCountSystem : EntitySystem
{
    [Dependency] private readonly StationJobsSystem _jobsSystem = default!;
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerJoinedStation);
    }

    private void OnPlayerJoinedStation(PlayerSpawnCompleteEvent args)
    {
        if (!TryComp<RelativeJobsCountComponent>(args.Station, out var relativeJobsComponent))
            return;

        foreach (var (targetJob, relativeJobDict) in relativeJobsComponent.Jobs)
        {
            foreach (var (relativeJob, modifier) in relativeJobDict)
            {
                if (args.JobId != relativeJob.ToString())
                    continue;

                if (!_jobsSystem.TryGetJobSlot(args.Station, targetJob, out var jobCount))
                    continue;

                if (jobCount >= relativeJobsComponent.MaxCount)
                    continue;

                _jobsSystem.TryAdjustJobSlot(args.Station, targetJob, modifier, true);
            }
        }
    }
}

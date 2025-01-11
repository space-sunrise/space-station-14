using Content.Server.GameTicking;
using Content.Server.Station.Systems;
using Content.Shared.GameTicking;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._Sunrise.Roles;

/// <summary>
/// This handles...
/// </summary>
public sealed class OtherJobsTakenRequirementSystem : EntitySystem
{
    [Dependency] private readonly StationJobsSystem _jobsSystem = default!;
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<PlayerBeforeSpawnEvent>(OnPlayerJoinedStation);
    }

    private void OnPlayerJoinedStation(PlayerBeforeSpawnEvent args)
    {
        if (!TryComp<OtherJobsTakenRequirementComponent>(args.Station, out var jobsTakenRequirementComponent))
            return;

        if (args.JobId == null)
            return;

        if (jobsTakenRequirementComponent.TargetJob != args.JobId)
            return;

        _jobsSystem.TryAdjustJobSlot(args.Station, jobsTakenRequirementComponent.AdjustJob, jobsTakenRequirementComponent.Modifier, true);
    }
}

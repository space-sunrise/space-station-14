using Content.Server.Bed.Cryostorage;
using Content.Server.GameTicking;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Bed.Cryostorage;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._Sunrise.CryoTeleport;

/// <summary>
/// This handles...
/// </summary>
public sealed class CryoTeleportSystem : EntitySystem
{
    [Dependency] private readonly CryostorageSystem _cryostorage = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;

    public TimeSpan NextTick = TimeSpan.Zero;
    public TimeSpan RefreshCooldown = TimeSpan.FromSeconds(5);

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnCompleteSpawn);
        // SubscribeLocalEvent<CryoTeleportTargetComponent, BeingGibbedEvent>(OnGibbed);
        SubscribeLocalEvent<CryoTeleportTargetComponent, PlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<CryoTeleportTargetComponent, PlayerAttachedEvent>(OnPlayerAttached);
    }

    public override void Update(float delay)
    {
        if (NextTick > _timing.CurTime)
            return;

        NextTick += RefreshCooldown;

        var query = AllEntityQuery<CryoTeleportTargetComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Station == null)
                continue;

            if (comp.ExitTime == null)
                continue;

            if (!TryComp<StationCryoTeleportComponent>(comp.Station, out var stationCryoTeleportComponent) ||
                !TryComp<StationDataComponent>(comp.Station, out var stationData))
                continue;

            var stationGrid = _stationSystem.GetLargestGrid(stationData);

            if (stationGrid == null)
                continue;

            if (!(_timing.CurTime - comp.ExitTime >= stationCryoTeleportComponent.TransferDelay))
                continue;

            var cryoStorage = FindCryoStorage(Transform(stationGrid.Value));

            if (cryoStorage == null)
                continue;

            if (HasComp<CryostorageContainedComponent>(uid))
                continue;

            var containedComp = AddComp<CryostorageContainedComponent>(uid);

            containedComp.Cryostorage = cryoStorage.Value;
            containedComp.GracePeriodEndTime = _timing.CurTime;

            var portalCoordinates = _transformSystem.GetMapCoordinates(Transform(uid));

            var portal = _entity.SpawnEntity(stationCryoTeleportComponent.PortalPrototype, portalCoordinates);
            _audio.PlayPvs(stationCryoTeleportComponent.TransferSound, portal);

            _cryostorage.HandleEnterCryostorage((uid, containedComp), comp.UserId);
        }
    }

    private void OnCompleteSpawn(PlayerSpawnCompleteEvent ev)
    {
        if (!TryComp<StationCryoTeleportComponent>(ev.Station, out var cryoTeleportComponent))
            return;
        if (ev.JobId == null)
            return;

        if (ev.Player.AttachedEntity == null)
            return;

        var cryoTeleportTargetComponent = EnsureComp<CryoTeleportTargetComponent>(ev.Player.AttachedEntity.Value);
        cryoTeleportTargetComponent.Station = ev.Station;
    }

    private void OnPlayerDetached(EntityUid uid, CryoTeleportTargetComponent comp, PlayerDetachedEvent ev)
    {
        comp.ExitTime = _timing.CurTime;
    }

    private void OnPlayerAttached(EntityUid uid, CryoTeleportTargetComponent comp, PlayerAttachedEvent ev)
    {
        comp.ExitTime = null;
        comp.UserId = ev.Player.UserId;
    }

    // private void OnGibbed(EntityUid uid, CryoTeleportTargetComponent comp, BeingGibbedEvent ev)
    // {
    //     if (comp.Station == null)
    //         return;
    //
    //     if (!TryComp<TransformComponent>(uid, out var entityXform))
    //         return;
    //
    //     if (!TryComp<StationCryoTeleportComponent>(comp.Station, out var stationCryoTeleportComponent))
    //         return;
    //
    //     // var stationJobsComponent = Comp<StationJobsComponent>(comp.Station.Value);
    //     _mind.TryGetMind(uid, out var mindId, out var mindComp);
    //
    //     if (mindComp == null)
    //     {
    //         Log.Error($"mindComp null");
    //         return;
    //     }
    //
    //     if (mindComp.UserId == null)
    //         return;
    //
    //     foreach (var uniqueStation in _station.GetStationsSet())
    //     {
    //         if (!TryComp<StationJobsComponent>(uniqueStation, out var stationJobs))
    //             continue;
    //
    //         if (!_stationJobs.TryGetPlayerJobs(uniqueStation, mindComp.UserId.Value, out var jobs, stationJobs))
    //             continue;
    //
    //         foreach (var job in jobs)
    //         {
    //             _stationJobs.TryAdjustJobSlot(uniqueStation, job, 1, clamp: true);
    //         }
    //
    //         _stationJobs.TryRemovePlayerJobs(uniqueStation, mindComp.UserId.Value, stationJobs);
    //     }
    //
    //     if (!TryComp<StationRecordsComponent>(comp.Station, out var stationRecords))
    //         return;
    //
    //     var jobName = Loc.GetString("earlyleave-cryo-job-unknown");
    //     var recordId = _stationRecords.GetRecordByName(comp.Station.Value, Name(uid));
    //     if (recordId != null)
    //     {
    //         var key = new StationRecordKey(recordId.Value, comp.Station.Value);
    //         if (_stationRecords.TryGetRecord<GeneralStationRecord>(key, out var entry, stationRecords))
    //             jobName = entry.JobTitle;
    //
    //         _stationRecords.RemoveRecord(key, stationRecords);
    //     }
    //
    //     _chat.DispatchStationAnnouncement(comp.Station.Value,
    //         Loc.GetString(
    //             "earlyleave-cryo-announcement",
    //             ("character", Name(uid)),
    //             ("job", CultureInfo.CurrentCulture.TextInfo.ToTitleCase(jobName))
    //         ),
    //         Loc.GetString("earlyleave-cryo-sender"),
    //         playDefault: false
    //     );
    // }

    private EntityUid? FindCryoStorage(TransformComponent stationGridTransform)
    {
        var query = AllEntityQuery<CryostorageComponent, TransformComponent>();
        while (query.MoveNext(out var cryoUid, out _, out var cryoTransform))
        {
            if (stationGridTransform.MapUid != cryoTransform.MapUid)
                continue;

            return cryoUid;
        }

        return null;
    }
}

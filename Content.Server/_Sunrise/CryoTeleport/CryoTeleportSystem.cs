using System.Linq;
using Content.Server.Bed.Cryostorage;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared.Bed.Cryostorage;
using Content.Shared.GameTicking;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
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
    [Dependency] private readonly IPlayerManager _playerMan = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private bool _enable;
    public TimeSpan NextTick = TimeSpan.Zero;
    public TimeSpan RefreshCooldown = TimeSpan.FromSeconds(5);
    public override void Initialize()
    {
        _cfg.OnValueChanged(SunriseCCVars.CryoTeleportEnable, OnCryoTeleportEnableChanged, true);

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnCompleteSpawn);
        _playerMan.PlayerStatusChanged += OnSessionStatus;
    }

    private void OnCryoTeleportEnableChanged(bool enable)
    {
        _enable = enable;
    }

    public override void Update(float delay)
    {
        if (NextTick > _timing.CurTime)
            return;

        NextTick += RefreshCooldown;

        if (!_enable)
            return;

        var query = AllEntityQuery<CryoTeleportTargetComponent, MobStateComponent>();
        while (query.MoveNext(out var uid, out var comp, out var mobStateComponent))
        {
            if (comp.Station == null)
                continue;

            if (comp.ExitTime == null)
                continue;

            if (mobStateComponent.CurrentState != MobState.Alive)
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
        if (!_enable)
            return;

        if (!TryComp<StationCryoTeleportComponent>(ev.Station, out var cryoTeleportComponent))
            return;

        if (ev.JobId == null)
            return;

        if (ev.Player.AttachedEntity == null)
            return;

        var cryoTeleportTargetComponent = EnsureComp<CryoTeleportTargetComponent>(ev.Player.AttachedEntity.Value);
        cryoTeleportTargetComponent.Station = ev.Station;
        cryoTeleportTargetComponent.UserId = ev.Player.UserId;
    }

    private void OnSessionStatus(object? sender, SessionStatusEventArgs args)
    {
        if (!_enable)
            return;

        if (!TryComp<CryoTeleportTargetComponent>(args.Session.AttachedEntity, out var comp))
            return;

        if (args.Session.Status == SessionStatus.Disconnected && comp.ExitTime == null)
        {
            comp.ExitTime = _timing.CurTime;
        }
        else if (args.Session.Status == SessionStatus.Connected)
        {
            comp.ExitTime = null;
        }

        comp.UserId = args.Session.UserId;
    }

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

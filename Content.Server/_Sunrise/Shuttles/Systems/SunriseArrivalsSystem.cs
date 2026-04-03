using System.Numerics;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Systems;
using Content.Shared.Shuttles.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Server._Sunrise.Shuttles.Components;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared._Sunrise.UnbuildableGrid;
using Content.Server._Sunrise.ImmortalGrid;
using Content.Server.Spawners.Components;
using Content.Shared.Chat;
using Content.Shared.Bed.Cryostorage;
using Content.Server.Chat.Managers;
using Content.Shared.Shuttles.Systems;
using Content.Shared.Tiles;
using Content.Shared.Timing;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._Sunrise.Shuttles.Systems;

public sealed class SunriseArrivalsSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly MapLoaderSystem _loader = default!;
    [Dependency] private readonly MapSystem _mapSystem = default!;
    [Dependency] private readonly ShuttleSystem _shuttle = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly DockingSystem _docking = default!;

    private bool _enabled;
    private string _shuttlePath = string.Empty;
    private float _ftlTime = 15f;
    private readonly List<EntityUid> _shuttleQueue = new();

    private const float DockWaitTime = 30f;
    private const float QueueWaitTime = 30f;
    private const float FTLRetryTime = 10f;
    private const float ExitTime = 30f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawningEvent>(OnPlayerSpawning, before: new[] { typeof(ArrivalsSystem) });
        SubscribeLocalEvent<SunriseArrivalsShuttleComponent, FTLCompletedEvent>(OnFTLCompleted);

        _cfg.OnValueChanged(SunriseCCVars.ArrivalsSingleShuttle, b => _enabled = b, true);
        _cfg.OnValueChanged(SunriseCCVars.ArrivalsSingleShuttlePath, s => _shuttlePath = s, true);
        _cfg.OnValueChanged(SunriseCCVars.ArrivalsShuttleFTLTime, f => _ftlTime = f, true);
    }

    private void OnPlayerSpawning(PlayerSpawningEvent ev)
    {
        if (!_enabled || ev.SpawnResult != null || _ticker.RunLevel != GameRunLevel.InRound)
            return;

        if (ev.DesiredSpawnPointType == SpawnPointType.Job)
            return;

        if (ev.Station == null || !HasComp<StationArrivalsComponent>(ev.Station.Value))
            return;

        var station = ev.Station.Value;

        // Spawn a temporary map for the shuttle
        var dummyMap = _mapSystem.CreateMap(out var dummyMapId);

        // Load the shuttle grid
        if (!_loader.TryLoadGrid(dummyMapId, new ResPath(_shuttlePath), out var shuttleGrid))
        {
            Log.Error($"Failed to load single-shuttle grid at {_shuttlePath}");
            QueueDel(dummyMap);
            return;
        }

        EnsureComp<ProtectedGridComponent>(shuttleGrid.Value);
        EnsureComp<UnbuildableGridComponent>(shuttleGrid.Value);
        EnsureComp<ImmortalGridComponent>(shuttleGrid.Value);
        EnsureComp<PreventPilotComponent>(shuttleGrid.Value);

        // Find a spawn point on the shuttle
        var spawnPoints = EntityQueryEnumerator<SpawnPointComponent, TransformComponent>();
        EntityCoordinates? spawnLoc = null;
        while (spawnPoints.MoveNext(out _, out var spawnPoint, out var xform))
        {
            if (xform.GridUid == shuttleGrid.Value && spawnPoint.SpawnType == SpawnPointType.LateJoin)
            {
                spawnLoc = xform.Coordinates;
                break;
            }
        }

        // Fallback to center if no spawn point found
        spawnLoc ??= new EntityCoordinates(shuttleGrid.Value, 0, 0);

        ev.SpawnResult = _stationSpawning.SpawnPlayerMob(
            spawnLoc.Value,
            ev.Job,
            ev.HumanoidCharacterProfile,
            station);

        if (ev.SpawnResult == null)
        {
            QueueDel(shuttleGrid.Value);
            QueueDel(dummyMap);
            return;
        }

        // Track the shuttle
        var arrivals = EnsureComp<SunriseArrivalsShuttleComponent>(shuttleGrid.Value);
        arrivals.Station = station;
        arrivals.Player = ev.SpawnResult.Value;

        // Start in queued state with infinite FTL to Nullspace
        arrivals.Station = station;
        arrivals.Player = ev.SpawnResult.Value;
        arrivals.QueuedStartTime = _timing.CurTime;
        arrivals.State = SunriseArrivalsShuttleState.Queued;
        arrivals.NextRetry = _timing.CurTime + TimeSpan.FromSeconds(FTLRetryTime);
        arrivals.NextAnnouncement = _timing.CurTime + TimeSpan.FromSeconds(30);

        _shuttleQueue.Add(shuttleGrid.Value);

        var shuttleComp = Comp<ShuttleComponent>(shuttleGrid.Value);
        _shuttle.FTLToCoordinates(shuttleGrid.Value, shuttleComp, Transform(shuttleGrid.Value).Coordinates, Angle.Zero, hyperspaceTime: 3600f);

        var attendant = FindAttendant(shuttleGrid.Value);
        arrivals.Attendant = attendant;
        arrivals.PlayerName = ev.HumanoidCharacterProfile?.Name ?? "Unknown";
        arrivals.PlayerJob = ev.Job != null ? _prototypeManager.Index(ev.Job.Value).LocalizedName : Loc.GetString("job-name-unknown");
        arrivals.Greeted = false;
    }

    private void OnFTLCompleted(EntityUid uid, SunriseArrivalsShuttleComponent component, ref FTLCompletedEvent args)
    {
        if (component.State != SunriseArrivalsShuttleState.Travelling)
            return;

        if (IsDocked(uid))
        {
            component.State = SunriseArrivalsShuttleState.Docked;
            component.DockedStartTime = _timing.CurTime;
            component.NextAnnouncement = _timing.CurTime + TimeSpan.FromSeconds(DockWaitTime);
            component.Warned = false;

            if (component.Attendant != null)
            {
                var mapId = _transform.GetMapId(args.MapUid);
                var station = _station.GetStationInMap(mapId);
                var stationName = station != null ? Name(station.Value) : "Unknown";
                var msg = Loc.GetString("sunrise-arrivals-attendant-arrival", ("station", stationName));
                _chat.TrySendInGameICMessage(component.Attendant.Value, msg, InGameICChatType.Speak, hideChat: false);
            }
        }
        else
        {
            // If we're not docked (arrived in space), go back to waiting instead of staying in space
            component.State = SunriseArrivalsShuttleState.Waiting;
            component.NextRetry = _timing.CurTime + TimeSpan.FromSeconds(FTLRetryTime);

            // Immediately FTL back to holding target to stay in "hyperspace"
            var shuttleComp = Comp<ShuttleComponent>(uid);
            _shuttle.FTLToCoordinates(uid, shuttleComp, Transform(uid).Coordinates, Angle.Zero, hyperspaceTime: 3600f);
        }
    }

    private bool IsDocked(EntityUid uid)
    {
        var query = EntityQueryEnumerator<DockingComponent, TransformComponent>();
        while (query.MoveNext(out _, out var dock, out var xform))
        {
            if (xform.GridUid == uid && dock.Docked)
                return true;
        }
        return false;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;

        var query = EntityQueryEnumerator<SunriseArrivalsShuttleComponent, ShuttleComponent>();
        while (query.MoveNext(out var uid, out var arrivals, out var shuttle))
        {
            // Delayed greeting logic
            if (!arrivals.Greeted && arrivals.Attendant != null && TryComp<ActorComponent>(arrivals.Player, out var actor))
            {
                if (arrivals.GreetTime == null)
                {
                    arrivals.GreetTime = curTime + TimeSpan.FromSeconds(2);
                }
                else if (curTime >= arrivals.GreetTime)
                {
                    var msg = Loc.GetString("sunrise-arrivals-attendant-welcome",
                        ("name", arrivals.PlayerName),
                        ("job", arrivals.PlayerJob),
                        ("station", Name(arrivals.Station)),
                        ("eta", (int)QueueWaitTime));
                    _chat.TrySendInGameICMessage(arrivals.Attendant.Value, msg, InGameICChatType.Speak, hideChat: false);
                    arrivals.Greeted = true;
                }
            }

            switch (arrivals.State)
            {
                case SunriseArrivalsShuttleState.Queued:
                    if (_shuttleQueue.Count > 0 && _shuttleQueue[0] == uid)
                    {
                        if (arrivals.NextRetry != null && curTime >= arrivals.NextRetry)
                        {
                            var station = arrivals.Station;
                            var targetGrid = _station.GetLargestGrid(station) ?? station;
                            var config = _docking.GetDockingConfig(uid, targetGrid, "DockArrivals", false);

                            if (config != null && TryComp<FTLComponent>(uid, out var ftl) && ftl.State == FTLState.Travelling)
                            {
                                 var mapCoords = config.Coordinates.ToMap(EntityManager, _transform);
                                 var worldAngle = _transform.GetWorldRotation(targetGrid) + config.Angle;

                                 if (IsArrivalZoneClear(uid, mapCoords, worldAngle))
                                {
                                    // Redirect the shuttle to the dock
                                    ftl.TargetCoordinates = config.Coordinates;
                                    ftl.TargetAngle = config.Angle;
                                    ftl.PriorityTag = "DockArrivals";

                                    // Shorten the arrival time to CVar seconds
                                    ftl.TravelTime = _ftlTime;
                                    ftl.StateTime = StartEndTime.FromCurTime(_timing, _ftlTime);

                                    // Reserve the docks manually
                                    foreach (var docks in config.Docks)
                                    {
                                        var reservation = EnsureComp<FtlReservationComponent>(docks.DockBUid);
                                        reservation.ReservedBy = uid;
                                        arrivals.ReservedDocks.Add(docks.DockBUid);
                                    }

                                    arrivals.State = SunriseArrivalsShuttleState.Travelling;
                                    arrivals.NextRetry = null;
                                    arrivals.NextAnnouncement = null;
                                    _shuttleQueue.RemoveAt(0);
                                }
                                else
                                {
                                    // Zone blocked by another arrival shuttle, retry later and postpone announcement
                                    arrivals.NextRetry = curTime + TimeSpan.FromSeconds(FTLRetryTime);
                                    arrivals.NextAnnouncement = curTime + TimeSpan.FromSeconds(30);
                                }
                            }
                            else
                            {
                                // No dock found. Check if it's because arrivals shuttles are already docked there.
                                if (IsDocksOccupiedByArrivals(targetGrid))
                                {
                                    arrivals.NextAnnouncement = curTime + TimeSpan.FromSeconds(30);
                                }

                                arrivals.NextRetry = curTime + TimeSpan.FromSeconds(FTLRetryTime);
                            }
                        }

                        if (arrivals.NextAnnouncement != null && arrivals.NextAnnouncement < curTime)
                        {
                            AnnounceDockingBlocked(arrivals.Station);
                            arrivals.NextAnnouncement = null;
                        }
                    }
                    break;

                case SunriseArrivalsShuttleState.Travelling:
                    // Handled by FTLCompletedEvent
                    break;

                case SunriseArrivalsShuttleState.Waiting:
                    if (HasComp<FTLComponent>(uid))
                        continue;

                    if (arrivals.NextRetry != null && arrivals.NextRetry < curTime)
                    {
                        // Try to dock again
                        var targetGrid = _station.GetLargestGrid(arrivals.Station) ?? arrivals.Station;
                        _shuttle.FTLToDock(uid, shuttle, targetGrid, priorityTag: "DockArrivals");
                        arrivals.State = SunriseArrivalsShuttleState.Travelling;
                        arrivals.NextRetry = null;
                    }

                    if (arrivals.NextAnnouncement != null && arrivals.NextAnnouncement < curTime)
                    {
                        AnnounceDockingBlocked(arrivals.Station);
                        arrivals.NextAnnouncement = null;
                    }
                    break;

                case SunriseArrivalsShuttleState.Docked:
                    if (!IsPlayerOnShuttle(uid))
                    {
                        StartDeparture(uid, arrivals);
                        continue;
                    }

                    // Single warning 15s after docking
                    if (!arrivals.Warned && arrivals.DockedStartTime != null && curTime >= arrivals.DockedStartTime + TimeSpan.FromSeconds(15))
                    {
                        if (arrivals.Attendant != null)
                        {
                            var msg = Loc.GetString("sunrise-arrivals-attendant-evac");
                            _chat.TrySendInGameICMessage(arrivals.Attendant.Value, msg, InGameICChatType.Speak, hideChat: false);
                        }
                        arrivals.Warned = true;
                    }

                    // Departure 30s after docking
                    if (arrivals.NextAnnouncement != null && arrivals.NextAnnouncement < curTime)
                    {
                        TryTeleportPlayer(uid, arrivals);
                        StartDeparture(uid, arrivals);
                    }
                    break;

                case SunriseArrivalsShuttleState.Leaving:
                    if (!HasComp<FTLComponent>(uid))
                    {
                        if (arrivals.NextAnnouncement == null)
                        {
                            // Use NextAnnouncement field to store deletion timer. 5 seconds for FTL sound.
                            arrivals.NextAnnouncement = curTime + TimeSpan.FromSeconds(5);
                        }
                        else if (curTime >= arrivals.NextAnnouncement)
                        {
                            // Delete the shuttle after a 5 second delay to let sounds finish
                            QueueDel(uid);
                        }
                    }
                    break;
            }
        }
    }

    private void StartDeparture(EntityUid uid, SunriseArrivalsShuttleComponent component)
    {
        if (component.State == SunriseArrivalsShuttleState.Leaving)
            return;

        component.State = SunriseArrivalsShuttleState.Leaving;
        _shuttle.FTLToCoordinates(uid, Comp<ShuttleComponent>(uid), new EntityCoordinates(uid, Vector2.Zero), Angle.Zero, hyperspaceTime: ExitTime);
    }

    private void TryTeleportPlayer(EntityUid gridUid, SunriseArrivalsShuttleComponent arrivals)
    {
        if (!IsPlayerOnShuttle(gridUid))
            return;

        var station = arrivals.Station;
        if (!station.IsValid())
            return;

        // Find a late join spawn point on the station
        var spawnQuery = EntityQueryEnumerator<SpawnPointComponent, TransformComponent>();
        EntityCoordinates? target = null;

        while (spawnQuery.MoveNext(out var spawnUid, out var spawn, out var xform))
        {
            if (spawn.SpawnType == SpawnPointType.LateJoin && _station.GetOwningStation(spawnUid) == station)
            {
                target = xform.Coordinates;
                break;
            }
        }

        // Fallback to station grid center
        if (target == null)
        {
            // Search for cryopods as a fallback
            var cryoQuery = EntityQueryEnumerator<CryostorageComponent, TransformComponent>();
            while (cryoQuery.MoveNext(out var cryoUid, out _, out var xform))
            {
                if (_station.GetOwningStation(cryoUid) == station)
                {
                    target = xform.Coordinates;
                    break;
                }
            }

            if (target == null)
            {
                var targetGrid = _station.GetLargestGrid(station);
                if (targetGrid != null && TryComp<MapGridComponent>(targetGrid, out var grid))
                    target = new EntityCoordinates(targetGrid.Value, grid.LocalAABB.Center);
            }
        }

        if (target != null && arrivals.Player != null)
        {
            _transform.SetCoordinates(arrivals.Player.Value, target.Value);
            if (TryComp<ActorComponent>(arrivals.Player.Value, out var actor))
            {
                _chatManager.ChatMessageToOne(ChatChannel.Server, Loc.GetString("sunrise-arrivals-forced-evac"), Loc.GetString("sunrise-arrivals-forced-evac"), EntityUid.Invalid, false, actor.PlayerSession.Channel);
            }
        }
    }

    private EntityUid? FindAttendant(EntityUid gridUid)
    {
        var query = EntityQueryEnumerator<SunriseArrivalsAttendantComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var childXform))
        {
            if (childXform.GridUid == gridUid)
                return uid;
        }

        return null;
    }

    private bool IsPlayerOnShuttle(EntityUid uid)
    {
        var xform = Transform(uid);
        var query = EntityQueryEnumerator<ActorComponent, TransformComponent>();
        while (query.MoveNext(out var playerUid, out _, out var playerXform))
        {
            if (playerXform.GridUid == uid)
                return true;
        }
        return false;
    }

    private bool IsArrivalZoneClear(EntityUid shuttleUid, MapCoordinates target, Angle worldAngle)
    {
        if (!TryComp<MapGridComponent>(shuttleUid, out var grid))
            return true;

        // Wide buffer removed to allow adjacent dock arrivals. 0.01m to prevent literal overlap.
        var localBounds = grid.LocalAABB.Enlarged(0.01f);
        var myTargetBox = GetArrivalBox(localBounds, target.Position, worldAngle);

        var query = EntityQueryEnumerator<SunriseArrivalsShuttleComponent>();
        while (query.MoveNext(out var otherUid, out var otherArrivals))
        {
            if (otherUid == shuttleUid)
                continue;

            // Check if other shuttle is in FTL or already docked
            if (otherArrivals.State is SunriseArrivalsShuttleState.Travelling or SunriseArrivalsShuttleState.Waiting or SunriseArrivalsShuttleState.Docked)
            {
                Box2 otherTargetBox;

                if (otherArrivals.State == SunriseArrivalsShuttleState.Docked)
                {
                    // Already docked, check its current world position
                    var otherXform = Transform(otherUid);
                    if (otherXform.MapID != target.MapId)
                        continue;

                    var (pos, rot) = _transform.GetWorldPositionRotation(otherUid);
                    if (!TryComp<MapGridComponent>(otherUid, out var otherGrid))
                        continue;

                    var otherBounds = otherGrid.LocalAABB.Enlarged(0.01f);
                    otherTargetBox = GetArrivalBox(otherBounds, pos, rot);
                }
                else
                {
                    // In FTL, check its target zone
                    if (!TryComp<FTLComponent>(otherUid, out var otherFtl))
                        continue;

                    var otherTargetMap = otherFtl.TargetCoordinates.ToMap(EntityManager, _transform);
                    if (otherTargetMap.MapId != target.MapId)
                        continue;

                    if (!TryComp<MapGridComponent>(otherUid, out var otherGrid))
                        continue;

                    var otherTargetWorldAngle = _transform.GetWorldRotation(otherFtl.TargetCoordinates.EntityId) + otherFtl.TargetAngle;
                    var otherBounds = otherGrid.LocalAABB.Enlarged(0.01f);
                    otherTargetBox = GetArrivalBox(otherBounds, otherTargetMap.Position, otherTargetWorldAngle);
                }

                if (myTargetBox.Intersects(otherTargetBox))
                    return false;
            }
        }

        return true;
    }

    private Box2 GetArrivalBox(Box2 localBounds, Vector2 position, Angle angle)
    {
        var corners = new[]
        {
            new Vector2(localBounds.Left, localBounds.Top),
            new Vector2(localBounds.Right, localBounds.Top),
            new Vector2(localBounds.Right, localBounds.Bottom),
            new Vector2(localBounds.Left, localBounds.Bottom)
        };

        var minX = float.MaxValue;
        var minY = float.MaxValue;
        var maxX = float.MinValue;
        var maxY = float.MinValue;

        foreach (var corner in corners)
        {
            var rotated = angle.RotateVec(corner) + position;
            minX = MathF.Min(minX, rotated.X);
            minY = MathF.Min(minY, rotated.Y);
            maxX = MathF.Max(maxX, rotated.X);
            maxY = MathF.Max(maxY, rotated.Y);
        }

        return new Box2(minX, minY, maxX, maxY);
    }

    private void AnnounceDockingBlocked(EntityUid station)
    {
        var message = Loc.GetString("sunrise-arrivals-shuttle-docking-blocked");
        var sender = Loc.GetString("sunrise-arrivals-shuttle-cc-sender");
        _chat.DispatchStationAnnouncement(station, message, sender, colorOverride: Color.Gold);
    }

    private bool IsDocksOccupiedByArrivals(EntityUid targetGrid)
    {
        var docks = _docking.GetDocks(targetGrid);
        foreach (var dock in docks)
        {
            if (!TryComp<PriorityDockComponent>(dock.Owner, out var priority) || priority.Tag != "DockArrivals")
                continue;

            if (dock.Comp.DockedWith != null)
            {
                var otherGrid = Transform(dock.Comp.DockedWith.Value).GridUid;
                if (HasComp<SunriseArrivalsShuttleComponent>(otherGrid))
                    return true;
            }
        }
        return false;
    }
}

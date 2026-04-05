using System.Numerics;
using Content.Server.Chat.Systems;
using Content.Shared.Tag;
using Content.Shared.Timing;
using Content.Server.GameTicking;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Systems;
using Content.Shared.CCVar;
using Content.Shared.Shuttles.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
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
    private bool _arrivalsEnabled;
    private string _shuttlePath = string.Empty;
    private float _ftlTime = 15f;

    /// <summary>
    /// Time the shuttle waits at the station dock before forced departure.
    /// </summary>
    private const float DockWaitTime = 15f;

    /// <summary>
    /// FTL travel time for leaving departure (short, shuttle gets deleted after).
    /// </summary>
    private const float DepartFtlTime = 5f;

    /// <summary>
    /// Delay after FTL completion before deleting the leaving shuttle.
    /// </summary>
    private const float DeleteDelayTime = 2f;

    /// <summary>
    /// Maximum time a shuttle can exist before failsafe kicks in.
    /// </summary>
    private static readonly TimeSpan FailsafeTimeout = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Buffer space between shuttles on the pool map.
    /// </summary>
    private const float PoolBuffer = 10f;

    /// <summary>
    /// Grace period after the player leaves the shuttle before it departs.
    /// Gives them time to clear the airlock.
    /// </summary>
    private static readonly TimeSpan ExitGracePeriod = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Delay before greeting the player after spawn.
    /// </summary>
    private static readonly TimeSpan GreetDelay = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Time after docking before issuing the evac warning.
    /// </summary>
    private static readonly TimeSpan WarnDelay = TimeSpan.FromSeconds(8);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawningEvent>(OnPlayerSpawning, before: new[] { typeof(ArrivalsSystem) });
        SubscribeLocalEvent<SunriseArrivalsShuttleComponent, FTLCompletedEvent>(OnFTLCompleted);
        SubscribeLocalEvent<SunriseArrivalsShuttleComponent, ComponentShutdown>(OnShuttleShutdown);

        _cfg.OnValueChanged(SunriseCCVars.ArrivalsSingleShuttle, b => _enabled = b, true);
        _cfg.OnValueChanged(SunriseCCVars.ArrivalsSingleShuttlePath, s => _shuttlePath = s, true);
        _cfg.OnValueChanged(SunriseCCVars.ArrivalsShuttleFTLTime, f => _ftlTime = f, true);
        _cfg.OnValueChanged(CCVars.ArrivalsShuttles, b => _arrivalsEnabled = b, true);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _cfg.UnsubValueChanged(SunriseCCVars.ArrivalsSingleShuttle, b => _enabled = b);
        _cfg.UnsubValueChanged(SunriseCCVars.ArrivalsSingleShuttlePath, s => _shuttlePath = s);
        _cfg.UnsubValueChanged(SunriseCCVars.ArrivalsShuttleFTLTime, f => _ftlTime = f);
        _cfg.UnsubValueChanged(CCVars.ArrivalsShuttles, b => _arrivalsEnabled = b);
    }

    #region Event Handlers

    private void OnPlayerSpawning(PlayerSpawningEvent ev)
    {
        if (!_enabled || !_arrivalsEnabled || ev.SpawnResult != null || _ticker.RunLevel != GameRunLevel.InRound)
            return;

        if (ev.DesiredSpawnPointType == SpawnPointType.Job)
            return;

        if (ev.Station == null || !HasComp<StationArrivalsComponent>(ev.Station.Value))
            return;

        var station = ev.Station.Value;

        try
        {
            var shuttleUid = SpawnShuttle(station);
            if (shuttleUid == null)
            {
                Log.Error("Failed to spawn arrivals shuttle, falling back to station spawn");
                return; // Let vanilla system handle it
            }

            var spawnLoc = FindShuttleSpawnPoint(shuttleUid.Value);
            ev.SpawnResult = _stationSpawning.SpawnPlayerMob(
                spawnLoc,
                ev.Job,
                ev.HumanoidCharacterProfile,
                station);

            if (ev.SpawnResult == null)
            {
                Log.Error($"Failed to spawn player mob on arrivals shuttle {ToPrettyString(shuttleUid.Value)}");
                CleanupShuttle(shuttleUid.Value);
                return;
            }

            var arrivals = EnsureComp<SunriseArrivalsShuttleComponent>(shuttleUid.Value);
            arrivals.Station = station;
            arrivals.Player = ev.SpawnResult.Value;
            arrivals.SpawnTime = _timing.CurTime;
            arrivals.State = SunriseArrivalsShuttleState.Queued;
            arrivals.Attendant = FindAttendant(shuttleUid.Value);
            arrivals.PlayerName = ev.HumanoidCharacterProfile?.Name ?? "Unknown";
            arrivals.PlayerJob = ev.Job != null
                ? _prototypeManager.Index(ev.Job.Value).LocalizedName
                : Loc.GetString("job-name-unknown");
            arrivals.Greeted = false;

            EnqueueShuttle(shuttleUid.Value);

            // Put shuttle in infinite FTL so it appears to be in hyperspace
            var shuttleComp = Comp<ShuttleComponent>(shuttleUid.Value);
            _shuttle.FTLToCoordinates(shuttleUid.Value, shuttleComp,
                Transform(shuttleUid.Value).Coordinates, Angle.Zero, hyperspaceTime: 3600f);

            Log.Info($"Arrivals shuttle {ToPrettyString(shuttleUid.Value)} spawned for player " +
                     $"'{arrivals.PlayerName}' heading to {ToPrettyString(station)}");
        }
        catch (Exception e)
        {
            Log.Error($"Exception in arrivals shuttle spawn: {e}");
            // Don't set ev.SpawnResult — let vanilla system handle fallback
        }
    }

    private void OnFTLCompleted(EntityUid uid, SunriseArrivalsShuttleComponent component, ref FTLCompletedEvent args)
    {
        if (component.State != SunriseArrivalsShuttleState.Travelling)
            return;

        if (IsDocked(uid))
        {
            component.State = SunriseArrivalsShuttleState.Docked;
            component.DockTime = _timing.CurTime;
            component.Warned = false;

            if (component.Attendant != null)
            {
                var mapId = _transform.GetMapId(args.MapUid);
                var station = _station.GetStationInMap(mapId);
                var stationName = station != null ? Name(station.Value) : "Unknown";
                var msg = Loc.GetString("sunrise-arrivals-attendant-arrival", ("station", stationName));
                _chat.TrySendInGameICMessage(component.Attendant.Value, msg, InGameICChatType.Speak, hideChat: false);
            }

            Log.Debug($"Arrivals shuttle {ToPrettyString(uid)} docked at station");
        }
        else
        {
            // Failed to dock — re-enqueue so it retries on next dispatch cycle
            Log.Warning($"Arrivals shuttle {ToPrettyString(uid)} completed FTL but not docked, re-enqueueing");
            component.State = SunriseArrivalsShuttleState.Queued;
            EnqueueShuttle(uid);

            // Put back into infinite FTL
            var shuttleComp = Comp<ShuttleComponent>(uid);
            _shuttle.FTLToCoordinates(uid, shuttleComp,
                Transform(uid).Coordinates, Angle.Zero, hyperspaceTime: 3600f);
        }
    }

    private void OnShuttleShutdown(EntityUid uid, SunriseArrivalsShuttleComponent component, ComponentShutdown args)
    {
        // Clean up dock reservations
        foreach (var dock in component.ReservedDocks)
        {
            RemCompDeferred<FtlReservationComponent>(dock);
        }
        component.ReservedDocks.Clear();

        // Remove from queue if present
        var poolQuery = EntityQueryEnumerator<SunriseArrivalsPoolComponent>();
        while (poolQuery.MoveNext(out _, out var pool))
        {
            pool.Queue.Remove(uid);
        }
    }

    #endregion

    #region Update Loop

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_enabled || !_arrivalsEnabled)
            return;

        var curTime = _timing.CurTime;

        TryDispatchFromQueue(curTime);

        var query = EntityQueryEnumerator<SunriseArrivalsShuttleComponent>();
        while (query.MoveNext(out var uid, out var arrivals))
        {
            ProcessGreeting(uid, arrivals, curTime);
            ProcessFailsafe(uid, arrivals, curTime);

            switch (arrivals.State)
            {
                case SunriseArrivalsShuttleState.Queued:
                    // Handled by TryDispatchFromQueue
                    break;
                case SunriseArrivalsShuttleState.Travelling:
                    // Handled by OnFTLCompleted / failsafe
                    break;
                case SunriseArrivalsShuttleState.Docked:
                    ProcessDocked(uid, arrivals, curTime);
                    break;
                case SunriseArrivalsShuttleState.Leaving:
                    ProcessLeaving(uid, arrivals, curTime);
                    break;
            }
        }
    }

    /// <summary>
    /// Tries to dispatch shuttles from the queue to free docks.
    /// Dispatches one shuttle per free dock per tick.
    /// </summary>
    private void TryDispatchFromQueue(TimeSpan curTime)
    {
        var poolQuery = EntityQueryEnumerator<SunriseArrivalsPoolComponent>();
        while (poolQuery.MoveNext(out _, out var pool))
        {
            if (pool.Queue.Count == 0)
                continue;

            // Process queue — try to dispatch as many as we have free docks
            for (var i = 0; i < pool.Queue.Count; i++)
            {
                var shuttleUid = pool.Queue[i];

                if (!TryComp<SunriseArrivalsShuttleComponent>(shuttleUid, out var arrivals))
                {
                    pool.Queue.RemoveAt(i);
                    i--;
                    continue;
                }

                if (arrivals.State != SunriseArrivalsShuttleState.Queued)
                {
                    pool.Queue.RemoveAt(i);
                    i--;
                    continue;
                }

                if (!TryDispatchShuttle(shuttleUid, arrivals))
                    break; // No free docks — stop trying

                pool.Queue.RemoveAt(i);
                i--;
            }
        }
    }

    /// <summary>
    /// Attempts to dispatch a queued shuttle to the station dock.
    /// Returns true if the shuttle was dispatched, false if no dock available.
    /// </summary>
    private bool TryDispatchShuttle(EntityUid uid, SunriseArrivalsShuttleComponent arrivals)
    {
        var station = arrivals.Station;
        var targetGrid = _station.GetLargestGrid(station) ?? station;

        var config = _docking.GetDockingConfig(uid, targetGrid, "DockArrivals", false);
        if (config == null)
            return false;

        if (!TryComp<FTLComponent>(uid, out var ftl) || ftl.State != FTLState.Travelling)
            return false;

        // Redirect the existing infinite FTL to the dock
        ftl.TargetCoordinates = config.Coordinates;
        ftl.TargetAngle = config.Angle;
        ftl.PriorityTag = new ProtoId<TagPrototype>("DockArrivals");
        ftl.TravelTime = _ftlTime;
        ftl.StateTime = StartEndTime.FromCurTime(_timing, _ftlTime);

        // Reserve the docks
        foreach (var docks in config.Docks)
        {
            var reservation = EnsureComp<FtlReservationComponent>(docks.DockBUid);
            reservation.ReservedBy = uid;
            arrivals.ReservedDocks.Add(docks.DockBUid);
        }

        arrivals.State = SunriseArrivalsShuttleState.Travelling;

        Log.Debug($"Dispatched arrivals shuttle {ToPrettyString(uid)} to dock");
        return true;
    }

    /// <summary>
    /// Processes greeting logic — delayed welcome message from attendant.
    /// </summary>
    private void ProcessGreeting(EntityUid uid, SunriseArrivalsShuttleComponent arrivals, TimeSpan curTime)
    {
        if (arrivals.Greeted || arrivals.Attendant == null)
            return;

        if (!TryComp<ActorComponent>(arrivals.Player, out _))
            return;

        if (arrivals.GreetTime == null)
        {
            arrivals.GreetTime = curTime + GreetDelay;
            return;
        }

        if (curTime < arrivals.GreetTime)
            return;

        var msg = Loc.GetString("sunrise-arrivals-attendant-welcome",
            ("name", arrivals.PlayerName),
            ("job", arrivals.PlayerJob),
            ("station", Name(arrivals.Station)),
            ("eta", (int)_ftlTime));
        _chat.TrySendInGameICMessage(arrivals.Attendant.Value, msg, InGameICChatType.Speak, hideChat: false);
        arrivals.Greeted = true;
    }

    /// <summary>
    /// Failsafe: if a shuttle has existed for more than 2 minutes, teleport the player and delete it.
    /// </summary>
    private void ProcessFailsafe(EntityUid uid, SunriseArrivalsShuttleComponent arrivals, TimeSpan curTime)
    {
        if (arrivals.State == SunriseArrivalsShuttleState.Leaving)
            return;

        if (curTime <= arrivals.SpawnTime + FailsafeTimeout)
            return;

        Log.Warning($"Failsafe triggered for arrivals shuttle {ToPrettyString(uid)}, " +
                     $"player: '{arrivals.PlayerName}', state: {arrivals.State}");

        TryTeleportPlayer(uid, arrivals);

        if (arrivals.Attendant != null)
        {
            var msg = Loc.GetString("sunrise-arrivals-failsafe-teleport");
            if (arrivals.Player != null && TryComp<ActorComponent>(arrivals.Player.Value, out var actor))
            {
                _chatManager.ChatMessageToOne(ChatChannel.Server, msg, msg,
                    EntityUid.Invalid, false, actor.PlayerSession.Channel);
            }
        }

        CleanupShuttle(uid);
    }

    /// <summary>
    /// Processes docked state: warn, force evac, depart.
    /// </summary>
    private void ProcessDocked(EntityUid uid, SunriseArrivalsShuttleComponent arrivals, TimeSpan curTime)
    {
        var playerOnShuttle = IsPlayerOnShuttle(uid);

        if (!playerOnShuttle)
        {
            // Player left — start the grace timer
            arrivals.PlayerExitTime ??= curTime;

            // Depart after grace period so the airlock doesn't crush the player
            if (curTime >= arrivals.PlayerExitTime + ExitGracePeriod)
            {
                StartDeparture(uid, arrivals);
                return;
            }
        }
        else
        {
            // Player came back — reset the exit timer
            arrivals.PlayerExitTime = null;
        }

        var dockTime = arrivals.DockTime ?? curTime;

        // Warning ~8 seconds after docking
        if (!arrivals.Warned && curTime >= dockTime + WarnDelay)
        {
            if (arrivals.Attendant != null)
            {
                var msg = Loc.GetString("sunrise-arrivals-attendant-evac");
                _chat.TrySendInGameICMessage(arrivals.Attendant.Value, msg, InGameICChatType.Speak, hideChat: false);
            }
            arrivals.Warned = true;
        }

        // Forced departure after DockWaitTime
        if (curTime >= dockTime + TimeSpan.FromSeconds(DockWaitTime))
        {
            TryTeleportPlayer(uid, arrivals);
            StartDeparture(uid, arrivals);
        }
    }

    /// <summary>
    /// Processes leaving state: wait for FTL to finish, then delete.
    /// </summary>
    private void ProcessLeaving(EntityUid uid, SunriseArrivalsShuttleComponent arrivals, TimeSpan curTime)
    {
        if (HasComp<FTLComponent>(uid))
            return; // Still in FTL

        if (arrivals.LeaveStartTime == null)
        {
            arrivals.LeaveStartTime = curTime;
            return;
        }

        if (curTime >= arrivals.LeaveStartTime + TimeSpan.FromSeconds(DeleteDelayTime))
        {
            CleanupShuttle(uid);
        }
    }

    #endregion

    #region Main Logic

    /// <summary>
    /// Initiates shuttle departure from the station dock.
    /// Immediately frees dock reservations so queued shuttles can dispatch.
    /// </summary>
    private void StartDeparture(EntityUid uid, SunriseArrivalsShuttleComponent component)
    {
        if (component.State == SunriseArrivalsShuttleState.Leaving)
            return;

        component.State = SunriseArrivalsShuttleState.Leaving;
        component.LeaveStartTime = null;

        // Clear dock reservations immediately so queued shuttles can dispatch on next tick
        foreach (var dock in component.ReservedDocks)
        {
            RemCompDeferred<FtlReservationComponent>(dock);
        }
        component.ReservedDocks.Clear();

        // Remove any existing FTL component (e.g. arrival cooldown) that would
        // block TrySetupFTL. Without this, FTLToCoordinates silently fails and
        // the shuttle stays docked for the entire 10s FTL cooldown.
        RemComp<FTLComponent>(uid);

        Log.Debug($"Arrivals shuttle {ToPrettyString(uid)} departing");

        if (TryComp<ShuttleComponent>(uid, out var shuttleComp))
        {
            _shuttle.FTLToCoordinates(uid, shuttleComp,
                new EntityCoordinates(uid, Vector2.Zero), Angle.Zero,
                startupTime: 0f,
                hyperspaceTime: DepartFtlTime);
        }
    }

    /// <summary>
    /// Teleports the player from the shuttle to a station spawn point.
    /// </summary>
    private void TryTeleportPlayer(EntityUid gridUid, SunriseArrivalsShuttleComponent arrivals)
    {
        if (!IsPlayerOnShuttle(gridUid))
            return;

        var station = arrivals.Station;
        if (!station.IsValid())
            return;

        var target = FindStationSpawnPoint(station);

        if (target != null && arrivals.Player != null)
        {
            _transform.SetCoordinates(arrivals.Player.Value, target.Value);
            if (TryComp<ActorComponent>(arrivals.Player.Value, out var actor))
            {
                _chatManager.ChatMessageToOne(ChatChannel.Server,
                    Loc.GetString("sunrise-arrivals-forced-evac"),
                    Loc.GetString("sunrise-arrivals-forced-evac"),
                    EntityUid.Invalid, false, actor.PlayerSession.Channel);
            }
        }
    }

    /// <summary>
    /// Spawns a shuttle grid on the pool map.
    /// </summary>
    private EntityUid? SpawnShuttle(EntityUid station)
    {
        var (poolMapUid, poolMapId, pool) = EnsurePoolMap();

        if (!_loader.TryLoadGrid(poolMapId, new ResPath(_shuttlePath), out var shuttleGrid))
        {
            Log.Error($"Failed to load arrivals shuttle grid at {_shuttlePath}");
            return null;
        }

        // Offset the shuttle on the pool map
        if (TryComp<MapGridComponent>(shuttleGrid.Value, out var grid))
        {
            var width = grid.LocalAABB.Width;
            var offset = new Vector2(pool.NextOffset + width / 2f, 0f);
            _transform.SetLocalPosition(shuttleGrid.Value, offset);
            pool.NextOffset += width + PoolBuffer;
        }

        EnsureComp<ProtectedGridComponent>(shuttleGrid.Value);
        EnsureComp<UnbuildableGridComponent>(shuttleGrid.Value);
        EnsureComp<ImmortalGridComponent>(shuttleGrid.Value);
        EnsureComp<PreventPilotComponent>(shuttleGrid.Value);

        return shuttleGrid.Value;
    }

    /// <summary>
    /// Cleans up a shuttle: teleport any remaining player, delete the entity.
    /// </summary>
    private void CleanupShuttle(EntityUid uid)
    {
        if (TryComp<SunriseArrivalsShuttleComponent>(uid, out var arrivals))
        {
            // Safety: teleport player if still on shuttle
            if (arrivals.Player != null && IsPlayerOnShuttle(uid))
            {
                TryTeleportPlayer(uid, arrivals);
            }
        }

        QueueDel(uid);
        Log.Debug($"Cleaned up arrivals shuttle {ToPrettyString(uid)}");
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Gets or creates the shared pool map for arrivals shuttles.
    /// </summary>
    private (EntityUid MapUid, MapId MapId, SunriseArrivalsPoolComponent Pool) EnsurePoolMap()
    {
        var poolQuery = EntityQueryEnumerator<SunriseArrivalsPoolComponent>();
        while (poolQuery.MoveNext(out var uid, out var pool))
        {
            var mapComp = Comp<MapComponent>(uid);
            return (uid, mapComp.MapId, pool);
        }

        // Create new pool map
        var mapUid = _mapSystem.CreateMap(out var mapId);
        var newPool = AddComp<SunriseArrivalsPoolComponent>(mapUid);
        return (mapUid, mapId, newPool);
    }

    /// <summary>
    /// Adds a shuttle to the dispatch queue.
    /// </summary>
    private void EnqueueShuttle(EntityUid shuttleUid)
    {
        var (_, _, pool) = EnsurePoolMap();
        if (!pool.Queue.Contains(shuttleUid))
            pool.Queue.Add(shuttleUid);
    }

    /// <summary>
    /// Finds a late-join spawn point on the shuttle grid.
    /// </summary>
    private EntityCoordinates FindShuttleSpawnPoint(EntityUid shuttleGrid)
    {
        var spawnPoints = EntityQueryEnumerator<SpawnPointComponent, TransformComponent>();
        while (spawnPoints.MoveNext(out _, out var spawnPoint, out var xform))
        {
            if (xform.GridUid == shuttleGrid && spawnPoint.SpawnType == SpawnPointType.LateJoin)
                return xform.Coordinates;
        }

        return new EntityCoordinates(shuttleGrid, 0, 0);
    }

    /// <summary>
    /// Finds a spawn point on the station for emergency teleportation.
    /// </summary>
    private EntityCoordinates? FindStationSpawnPoint(EntityUid station)
    {
        // 1. Late-join spawn points
        var spawnQuery = EntityQueryEnumerator<SpawnPointComponent, TransformComponent>();
        while (spawnQuery.MoveNext(out var spawnUid, out var spawn, out var xform))
        {
            if (spawn.SpawnType == SpawnPointType.LateJoin && _station.GetOwningStation(spawnUid) == station)
                return xform.Coordinates;
        }

        // 2. Cryopods as fallback
        var cryoQuery = EntityQueryEnumerator<CryostorageComponent, TransformComponent>();
        while (cryoQuery.MoveNext(out var cryoUid, out _, out var xform))
        {
            if (_station.GetOwningStation(cryoUid) == station)
                return xform.Coordinates;
        }

        // 3. Grid center as last resort
        var targetGrid = _station.GetLargestGrid(station);
        if (targetGrid != null && TryComp<MapGridComponent>(targetGrid, out var grid))
            return new EntityCoordinates(targetGrid.Value, grid.LocalAABB.Center);

        return null;
    }

    /// <summary>
    /// Finds the attendant NPC entity on the shuttle grid.
    /// </summary>
    private EntityUid? FindAttendant(EntityUid gridUid)
    {
        var query = EntityQueryEnumerator<SunriseArrivalsAttendantComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var xform))
        {
            if (xform.GridUid == gridUid)
                return uid;
        }

        return null;
    }

    /// <summary>
    /// Checks if any player (actor) is currently on the shuttle grid.
    /// </summary>
    private bool IsPlayerOnShuttle(EntityUid gridUid)
    {
        var query = EntityQueryEnumerator<ActorComponent, TransformComponent>();
        while (query.MoveNext(out _, out _, out var playerXform))
        {
            if (playerXform.GridUid == gridUid)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Checks if the shuttle is docked to any dock.
    /// </summary>
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

    #endregion
}

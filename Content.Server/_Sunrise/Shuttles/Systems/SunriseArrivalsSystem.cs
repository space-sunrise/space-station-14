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
using Content.Server.Spawners.EntitySystems;
using Content.Shared.Roles;
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

    /// <summary>
    /// Время ожидания шаттла у дока станции перед принудительным отправлением.
    /// </summary>
    private const float DockWaitTime = 15f;

    /// <summary>
    /// Время FTL-перелета при отправлении, после которого шаттл быстро удаляется.
    /// </summary>
    private const float DepartFtlTime = 5f;

    /// <summary>
    /// Время FTL-перелета при отправке из очереди к доку.
    /// </summary>
    private const float DispatchFtlTime = 5f;

    /// <summary>
    /// Начальное время FTL-перелета для первого рейса прибытия.
    /// </summary>
    private const float InitialFtlTime = 15f;

    /// <summary>
    /// Задержка после завершения FTL перед удалением улетающего шаттла.
    /// </summary>
    private const float DeleteDelayTime = 2f;

    /// <summary>
    /// Максимальное время существования шаттла до срабатывания аварийной защиты.
    /// </summary>
    private static readonly TimeSpan FailsafeTimeout = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Буферное расстояние между шаттлами на карте пула.
    /// </summary>
    private const float PoolBuffer = 10f;

    /// <summary>
    /// Grace period после выхода игрока из шаттла перед отправлением.
    /// Дает время покинуть шлюз.
    /// </summary>
    private static readonly TimeSpan ExitGracePeriod = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Задержка перед приветствием игрока после спавна.
    /// </summary>
    private static readonly TimeSpan GreetDelay = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Время после стыковки до предупреждения об эвакуации.
    /// </summary>
    private static readonly TimeSpan WarnDelay = TimeSpan.FromSeconds(8);

    /// <summary>
    /// Время ожидания перед предупреждением игрока, что доки станции заблокированы.
    /// </summary>
    private static readonly TimeSpan BlockedWarnDelay = TimeSpan.FromSeconds(20);

    /// <summary>
    /// Интервал между сообщениями ЦК станции о заблокированных прибытиях.
    /// </summary>
    private static readonly TimeSpan StationWarnInterval = TimeSpan.FromMinutes(1);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawningEvent>(OnPlayerSpawning, after: new []{ typeof(ContainerSpawnPointSystem) }, before: new []{ typeof(SpawnPointSystem) });
        SubscribeLocalEvent<SunriseArrivalsShuttleComponent, FTLCompletedEvent>(OnFTLCompleted);
        SubscribeLocalEvent<SunriseArrivalsShuttleComponent, ComponentShutdown>(OnShuttleShutdown);

        _cfg.OnValueChanged(SunriseCCVars.ArrivalsSingleShuttle, b => _enabled = b, true);
        _cfg.OnValueChanged(SunriseCCVars.ArrivalsSingleShuttlePath, s => _shuttlePath = s, true);
        _cfg.OnValueChanged(CCVars.ArrivalsShuttles, b => _arrivalsEnabled = b, true);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _cfg.UnsubValueChanged(SunriseCCVars.ArrivalsSingleShuttle, b => _enabled = b);
        _cfg.UnsubValueChanged(SunriseCCVars.ArrivalsSingleShuttlePath, s => _shuttlePath = s);
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
                return; // Даем vanilla system обработать fallback
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

            // Помещаем шаттл в бесконечный FTL, чтобы он выглядел находящимся в гиперпространстве
            var shuttleComp = Comp<ShuttleComponent>(shuttleUid.Value);
            _shuttle.FTLToCoordinates(shuttleUid.Value, shuttleComp,
                Transform(shuttleUid.Value).Coordinates, Angle.Zero, hyperspaceTime: 3600f);

            Log.Info($"Arrivals shuttle {ToPrettyString(shuttleUid.Value)} spawned for player " +
                     $"'{arrivals.PlayerName}' heading to {ToPrettyString(station)}");
        }
        catch (Exception e)
        {
            Log.Error($"Exception in arrivals shuttle spawn: {e}");
            // Не задаем ev.SpawnResult, чтобы vanilla system обработала fallback
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
            // Стыковка не удалась — возвращаем в очередь для повтора на следующем dispatch cycle
            Log.Warning($"Arrivals shuttle {ToPrettyString(uid)} completed FTL but not docked, re-enqueueing");
            component.State = SunriseArrivalsShuttleState.Queued;
            EnqueueShuttle(uid);

            // Возвращаем в бесконечный FTL
            var shuttleComp = Comp<ShuttleComponent>(uid);
            _shuttle.FTLToCoordinates(uid, shuttleComp,
                Transform(uid).Coordinates, Angle.Zero, hyperspaceTime: 3600f);
        }
    }

    private void OnShuttleShutdown(EntityUid uid, SunriseArrivalsShuttleComponent component, ComponentShutdown args)
    {
        // Очищаем резервирования доков.
        foreach (var dock in component.ReservedDocks)
        {
            RemCompDeferred<FtlReservationComponent>(dock);
        }
        component.ReservedDocks.Clear();

        // Удаляем из очереди, если там есть
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
                    // Обрабатывается в TryDispatchFromQueue
                    break;
                case SunriseArrivalsShuttleState.Travelling:
                    // Обрабатывается в OnFTLCompleted или аварийной защите.
                    break;
                case SunriseArrivalsShuttleState.Docked:
                    ProcessDocked(uid, arrivals, curTime);
                    break;
                case SunriseArrivalsShuttleState.Leaving:
                    ProcessLeaving(uid, arrivals, curTime);
                    break;
            }

            if (arrivals.State == SunriseArrivalsShuttleState.Queued)
                ProcessBlockedDock(uid, arrivals, curTime);
        }
    }

    /// <summary>
    /// Предупреждает игрока, если шаттл застрял на орбите из-за заблокированных docks.
    /// </summary>
    private void ProcessBlockedDock(EntityUid uid, SunriseArrivalsShuttleComponent arrivals, TimeSpan curTime)
    {
        if (arrivals.DockBlockedWarned || arrivals.Attendant == null)
            return;

        // Начинаем отсчет от SpawnTime или первой попытки
        if (curTime < arrivals.SpawnTime + BlockedWarnDelay)
            return;

        // Предупреждаем только если шаттл еще даже не летит
        var msg = Loc.GetString("sunrise-arrivals-attendant-blocked", ("station", Name(arrivals.Station)));
        _chat.TrySendInGameICMessage(arrivals.Attendant.Value, msg, InGameICChatType.Speak, hideChat: false);
        arrivals.DockBlockedWarned = true;

        Log.Warning($"Arrivals shuttle {ToPrettyString(uid)} is stuck in queue - docks blocked at {ToPrettyString(arrivals.Station)}");

        // Станционное объявление ЦК с throttling по станции/глобально.
        var poolQuery = EntityQueryEnumerator<SunriseArrivalsPoolComponent>();
        while (poolQuery.MoveNext(out _, out var pool))
        {
            if (curTime < pool.LastAlertTime + StationWarnInterval)
                continue;

            var stationMsg = Loc.GetString("sunrise-arrivals-shuttle-docking-blocked");
            var sender = Loc.GetString("sunrise-arrivals-shuttle-cc-sender");
            _chat.DispatchStationAnnouncement(arrivals.Station, stationMsg, sender, colorOverride: Color.Gold);
            pool.LastAlertTime = curTime;
        }
    }

    /// <summary>
    /// Пытается отправить шаттлы из очереди к свободным docks.
    /// Отправляет один шаттл на каждый свободный док за тик.
    /// </summary>
    private void TryDispatchFromQueue(TimeSpan curTime)
    {
        var poolQuery = EntityQueryEnumerator<SunriseArrivalsPoolComponent>();
        while (poolQuery.MoveNext(out _, out var pool))
        {
            if (pool.Queue.Count == 0)
                continue;

            // Обрабатываем очередь и пытаемся отправить столько шаттлов, сколько есть свободных docks
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
                    break; // Свободных docks нет — прекращаем попытки

                pool.Queue.RemoveAt(i);
                i--;
            }
        }
    }

    /// <summary>
    /// Пытается отправить шаттл из очереди к доку станции.
    /// Возвращает true, если шаттл отправлен, или false, если доступного дока нет.
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

        // Перенаправляем существующий бесконечный FTL к доку.
        ftl.TargetCoordinates = config.Coordinates;
        ftl.TargetAngle = config.Angle;
        ftl.PriorityTag = new ProtoId<TagPrototype>("DockArrivals");
        ftl.TravelTime = DispatchFtlTime;
        ftl.StateTime = StartEndTime.FromCurTime(_timing, DispatchFtlTime);

        // Резервируем docks
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
    /// Обрабатывает логику приветствия: отложенное сообщение от сопровождающего.
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
            ("eta", (int)InitialFtlTime));
        _chat.TrySendInGameICMessage(arrivals.Attendant.Value, msg, InGameICChatType.Speak, hideChat: false);
        arrivals.Greeted = true;
    }

    /// <summary>
    /// Failsafe: если шаттл существует больше 2 минут, телепортирует игрока и удаляет шаттл.
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
    /// Обрабатывает состояние стыковки: предупреждение, принудительная эвакуация, отправление.
    /// </summary>
    private void ProcessDocked(EntityUid uid, SunriseArrivalsShuttleComponent arrivals, TimeSpan curTime)
    {
        var playerOnShuttle = IsPlayerOnShuttle(uid);

        if (!playerOnShuttle)
        {
            // Игрок вышел — запускаем grace timer
            arrivals.PlayerExitTime ??= curTime;

            // Отправляем после льготной паузы, чтобы шлюз не раздавил игрока.
            if (curTime >= arrivals.PlayerExitTime + ExitGracePeriod)
            {
                StartDeparture(uid, arrivals);
                return;
            }
        }
        else
        {
            // Игрок вернулся — сбрасываем exit timer
            arrivals.PlayerExitTime = null;
        }

        var dockTime = arrivals.DockTime ?? curTime;

        // Предупреждение примерно через 8 секунд после docking
        if (!arrivals.Warned && curTime >= dockTime + WarnDelay)
        {
            if (arrivals.Attendant != null)
            {
                var msg = Loc.GetString("sunrise-arrivals-attendant-evac");
                _chat.TrySendInGameICMessage(arrivals.Attendant.Value, msg, InGameICChatType.Speak, hideChat: false);
            }
            arrivals.Warned = true;
        }

        // Принудительное отправление после DockWaitTime
        if (curTime >= dockTime + TimeSpan.FromSeconds(DockWaitTime))
        {
            TryTeleportPlayer(uid, arrivals);
            StartDeparture(uid, arrivals);
        }
    }

    /// <summary>
    /// Обрабатывает состояние отправления: ждет завершения FTL, затем удаляет.
    /// </summary>
    private void ProcessLeaving(EntityUid uid, SunriseArrivalsShuttleComponent arrivals, TimeSpan curTime)
    {
        if (HasComp<FTLComponent>(uid))
            return; // Все еще в FTL

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
    /// Запускает отправление шаттла от дока станции.
    /// Сразу освобождает резервирования доков, чтобы шаттлы из очереди могли отправиться.
    /// </summary>
    private void StartDeparture(EntityUid uid, SunriseArrivalsShuttleComponent component)
    {
        if (component.State == SunriseArrivalsShuttleState.Leaving)
            return;

        component.State = SunriseArrivalsShuttleState.Leaving;
        component.LeaveStartTime = null;

        // Сразу очищаем резервирования доков, чтобы шаттлы из очереди могли отправиться на следующем тике.
        foreach (var dock in component.ReservedDocks)
        {
            RemCompDeferred<FtlReservationComponent>(dock);
        }
        component.ReservedDocks.Clear();

        // Удаляем существующий FTL-компонент, например cooldown прибытия, который
        // заблокировал бы TrySetupFTL. Без этого FTLToCoordinates тихо падает,
        // и шаттл остается пристыкованным на весь 10-секундный FTL cooldown.
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
    /// Телепортирует игрока из шаттла в spawn point станции.
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
    /// Спавнит grid шаттла на карте пула.
    /// </summary>
    private EntityUid? SpawnShuttle(EntityUid station)
    {
        var (poolMapUid, poolMapId, pool) = EnsurePoolMap();

        if (!_loader.TryLoadGrid(poolMapId, new ResPath(_shuttlePath), out var shuttleGrid))
        {
            Log.Error($"Failed to load arrivals shuttle grid at {_shuttlePath}");
            return null;
        }

        // Смещаем шаттл на карте пула.
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
    /// Очищает шаттл: телепортирует оставшегося игрока и удаляет сущность.
    /// </summary>
    private void CleanupShuttle(EntityUid uid)
    {
        if (TryComp<SunriseArrivalsShuttleComponent>(uid, out var arrivals))
        {
            // Безопасность: телепортируем игрока, если он все еще на шаттле
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
    /// Получает или создает общую карту пула для шаттлов прибытия.
    /// </summary>
    private (EntityUid MapUid, MapId MapId, SunriseArrivalsPoolComponent Pool) EnsurePoolMap()
    {
        var poolQuery = EntityQueryEnumerator<SunriseArrivalsPoolComponent>();
        while (poolQuery.MoveNext(out var uid, out var pool))
        {
            var mapComp = Comp<MapComponent>(uid);
            return (uid, mapComp.MapId, pool);
        }

        // Создаем новую карту пула.
        var mapUid = _mapSystem.CreateMap(out var mapId);
        var newPool = AddComp<SunriseArrivalsPoolComponent>(mapUid);
        return (mapUid, mapId, newPool);
    }

    /// <summary>
    /// Добавляет шаттл в dispatch queue.
    /// </summary>
    private void EnqueueShuttle(EntityUid shuttleUid)
    {
        var (_, _, pool) = EnsurePoolMap();
        if (!pool.Queue.Contains(shuttleUid))
            pool.Queue.Add(shuttleUid);
    }

    /// <summary>
    /// Ищет late-join spawn point на grid шаттла.
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
    /// Ищет spawn point на станции для аварийной телепортации.
    /// </summary>
    private EntityCoordinates? FindStationSpawnPoint(EntityUid station)
    {
        // 1. Late-join spawn points.
        var spawnQuery = EntityQueryEnumerator<SpawnPointComponent, TransformComponent>();
        while (spawnQuery.MoveNext(out var spawnUid, out var spawn, out var xform))
        {
            if (spawn.SpawnType == SpawnPointType.LateJoin && _station.GetOwningStation(spawnUid) == station)
                return xform.Coordinates;
        }

        // 2. Cryopods как fallback
        var cryoQuery = EntityQueryEnumerator<CryostorageComponent, TransformComponent>();
        while (cryoQuery.MoveNext(out var cryoUid, out _, out var xform))
        {
            if (_station.GetOwningStation(cryoUid) == station)
                return xform.Coordinates;
        }

        // 3. Центр grid как последний вариант
        var targetGrid = _station.GetLargestGrid(station);
        if (targetGrid != null && TryComp<MapGridComponent>(targetGrid, out var grid))
            return new EntityCoordinates(targetGrid.Value, grid.LocalAABB.Center);

        return null;
    }

    /// <summary>
    /// Ищет сущность NPC-сопровождающего на grid шаттла.
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
    /// Проверяет, находится ли сейчас какой-либо игрок (actor) на grid шаттла.
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
    /// Проверяет, пристыкован ли шаттл к любому доку.
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

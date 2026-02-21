using System;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Server.Ghost;
using Content.Server.Mind;
using Content.Shared.Mind.Components;
using Content.Server.Roles;
using Content.Server.Spawners.Components;
using Content.Server.Station.Systems;
using Content.Server.Station.Events;
using Content.Shared.Station.Components;
using Content.Server.Preferences.Managers;
using Robust.Server.Audio;
using Content.Server.Maps;
using Content.Server.Parallax;
using Content.Server.Shuttles.Systems;
using Robust.Shared.EntitySerialization.Systems;
using Content.Shared._Sunrise.PlanetPrison;
using Content.Shared._Sunrise.Shuttles;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared.GameTicking;
using Robust.Shared.GameStates;
using Content.Shared.Light.Components;
using Content.Shared.Maps;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Salvage;
using Content.Shared.Shuttles.Components;
using Content.Shared.Parallax.Biomes;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Ghost;
using Content.Shared.Players;
using Robust.Shared.Console;
using Robust.Server.GameObjects;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Utility;
using Robust.Shared.Configuration;
using Robust.Shared.EntitySerialization;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Enums;

namespace Content.Server._Sunrise.PlanetPrison;

/// <summary>
/// Состояние голосования за конкретную карту
/// </summary>
internal readonly record struct MapVotingState(
    Dictionary<NetUserId, int> Votes,
    CancellationTokenSource Timer,
    int? RemainingSeconds = null
);

/// <summary>
/// Состояние конкретной роли
/// </summary>
internal readonly record struct RoleState(
    Dictionary<NetUserId, int> Priorities,
    NetUserId? AssignedPlayer
);

// TODO: Рефактор с целью устранения варнингов и перехода системы на более современное API
public sealed class PlanetPrisonStationSystem : EntitySystem
{
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly BiomeSystem _biomeSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly ShuttleSystem _shuttle = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IConsoleHost _console = default!;
    [Dependency] private readonly AudioSystem _audioSystem = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly RoleSystem _roles = default!;
    [Dependency] private readonly IServerPreferencesManager _prefsManager = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly MapLoaderSystem _loader = default!;
    [Dependency] private readonly GhostSystem _ghosts = default!;

    // Состояние голосования карт
    private readonly Dictionary<string, MapVotingState> _mapStates = new();

    // Состояние ролей
    private readonly Dictionary<string, RoleState> _roleStates = new();

    // Игроки с активными приоритетами карт
    private readonly HashSet<NetUserId> _priorityPlayers = new();

    private static readonly Dictionary<string, int> DefaultRoleQuotas = new()
    {
        {"HeadOfPrison", 1},
        {"PlanetPrisoner", 1}
    };

    // Запущенные карты
    private readonly Dictionary<string, MapId> _launchedMaps = new();

    // Кэшированные карты для быстрой загрузки (очередь для множественных копий)
    private readonly Dictionary<string, Queue<MapId>> _cachedMaps = new();

    // Замороженные карты тюрьмы (ожидают удаления при рестарте раунда)
    private readonly HashSet<MapId> _pausedOrphanMaps = new();

    private bool _isCleaningPausedMaps;

    // Флаг для предотвращения повторного кэширования в одном лобби
    private bool _cachePreloaded = false;

    // Активная карта, для которой в данный момент идет глобальное голосование / отсчет таймера
    private string? _activeVotingMapId = null;

    private readonly List<EntityCoordinates> _observerSpawnPositionsBuffer = new();

    /// <summary>
    /// Возвращает все protoId карт тюрьмы (Modern + Old) для перебора.
    /// При отсутствии компонента использует fallback: прототипы GameMap с PrisonMapFamily или PrisonCacheSize > 0.
    /// </summary>
    private IEnumerable<string> GetPrisonMapProtoIds()
    {
        var component = GetPrisonComponent();
        if (component != null)
        {
            foreach (var id in component.StationsModern)
                yield return id.ToString();
            foreach (var id in component.StationsOld)
                yield return id.ToString();
            yield break;
        }

        foreach (var proto in _prototypeManager.EnumeratePrototypes<GameMapPrototype>())
        {
            if (!string.IsNullOrEmpty(proto.PrisonMapFamily) || proto.PrisonCacheSize > 0)
                yield return proto.ID;
        }
    }

    /// <summary>
    /// Объединённый список карт: из компонента + из _mapStates (карты с голосами).
    /// Гарантирует учёт карт, по которым уже голосовали, даже если компонент недоступен (лобби).
    /// </summary>
    private List<string> GetPrisonMapIdsForBroadcast()
    {
        var fromComponent = GetPrisonMapProtoIds().ToList();
        var fromStates = _mapStates.Keys.Where(k => k != "GLOBAL").ToList();
        return fromComponent.Union(fromStates).Distinct().ToList();
    }

    /// <summary>
    /// Возвращает квоты ролей из прототипа или дефолт.
    /// </summary>
    private IReadOnlyDictionary<string, int> GetRoleQuotas()
    {
        var component = GetPrisonComponent();
        if (component?.GameplaySettings.RequiredRoles != null && component.GameplaySettings.RequiredRoles.Count > 0)
            return component.GameplaySettings.RequiredRoles;
        return DefaultRoleQuotas;
    }

    /// <summary>
    /// Получает компонент PlanetPrisonSharedComponent или возвращает null
    /// </summary>
    private PlanetPrisonSharedComponent? GetPrisonComponent()
    {
        var planetPrisonQuery = EntityQueryEnumerator<PlanetPrisonSharedComponent>();
        if (planetPrisonQuery.MoveNext(out var component))
            return component;
        return null;
    }

    /// <summary>
    /// Получает настройки завершения карты для конкретного protoId карты.
    /// Сначала проверяет переопределения из PlanetPrisonMapSettingsComponent на станции карты,
    /// затем использует глобальные настройки из PlanetPrisonSharedComponent.
    /// </summary>
    private PrisonMapCompletionSettings GetCompletionSettingsForMap(string protoId)
    {
        var mapId = _launchedMaps.GetValueOrDefault(protoId);
        if (mapId != MapId.Nullspace && _mapManager.MapExists(mapId))
        {
            var stationQuery = EntityQueryEnumerator<StationDataComponent>();
            while (stationQuery.MoveNext(out var uid, out var stationData))
            {
                if (_transform.GetMapId(uid) != mapId)
                    continue;

                if (TryComp<PlanetPrisonMapSettingsComponent>(uid, out var mapSettings) &&
                    mapSettings.CompletionSettingsOverride != null)
                {
                    Console.WriteLine($"[SETTINGS] Using map-specific completion settings for {protoId} from PlanetPrisonMapSettingsComponent");
                    return mapSettings.CompletionSettingsOverride;
                }
            }
        }

        if (_prototypeManager.TryIndex<GameMapPrototype>(protoId, out var gameMapProto))
        {
            var mode = gameMapProto.PrisonCompletionMode;
            if (!string.IsNullOrEmpty(gameMapProto.PrisonMapFamily) || mode != PrisonMapCompletionMode.Delete)
            {
                Console.WriteLine($"[SETTINGS] Using completion from GameMapPrototype for {protoId}: {mode}");
                return new PrisonMapCompletionSettings { CompletionMode = mode };
            }
        }

        var component = GetPrisonComponent();
        var settings = component?.CompletionSettings ?? new PrisonMapCompletionSettings();
        Console.WriteLine($"[SETTINGS] Using global completion settings for {protoId}: {settings.CompletionMode}");
        return settings;
    }

    /// <summary>
    /// Получает минимальное количество игроков из компонента
    /// </summary>
    private int GetMinPlayersRequired()
    {
        var component = GetPrisonComponent();
        return component?.GameplaySettings.MinPlayersRequired ?? 2;
    }

    /// <summary>
    /// Размер кэша для карты с данным protoId: из прототипа (PrisonCacheSize) или дефолт из base.
    /// </summary>
    private int GetPrisonCacheSizeForProto(string protoId)
    {
        if (_prototypeManager.TryIndex<GameMapPrototype>(protoId, out var gameMapProto) &&
            !string.IsNullOrEmpty(gameMapProto.PrisonMapFamily))
        {
            var size = gameMapProto.PrisonCacheSize;
            if (size > 0)
                return size;
        }
        var component = GetPrisonComponent();
        return component?.CacheSettings.DefaultPrisonCacheSize ?? 1;
    }

    /// <summary>
    /// Карта тюрьмы с планетой (создаём планету при активации). По соглашению — PrisonMapFamily == "Metus".
    /// </summary>
    private bool IsPlanetPrisonMap(string protoId)
    {
        return _prototypeManager.TryIndex<GameMapPrototype>(protoId, out var p) &&
               string.Equals(p.PrisonMapFamily, "Metus", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Проверяет, можно ли запустить карту с текущими приоритетами игроков
    /// </summary>
    private bool CanStartRound()
    {
        // Получаем участвующих игроков (голосующих за карты)
        var participatingPlayers = _player.Sessions
            .Select(p => p.UserId)
            .Where(userId => _mapStates.Any(mapState =>
                mapState.Value.Votes.GetValueOrDefault(userId, 0) > 0))
            .ToList();

        Logger.Debug($"CanStartRound - participatingPlayers: {string.Join(", ", participatingPlayers)}");

        // Проверяем, что среди участвующих хватает приоритетов на роли
        foreach (var (roleId, requiredCount) in GetRoleQuotas())
        {
            var candidates = participatingPlayers
                .Where(playerId => GetOrCreateRoleState(roleId).Priorities.GetValueOrDefault(playerId, 0) > 0)
                .ToList();

            Logger.Debug($"CanStartRound - Role {roleId} - required: {requiredCount}, candidates: {candidates.Count}");

            if (candidates.Count < requiredCount)
            {
                Logger.Debug($"CanStartRound returning false due to insufficient {roleId}");
                return false;
            }
        }

        Logger.Debug($"CanStartRound returning true");
        return true;
    }

    /// <summary>
    /// Возвращает количество игроков, участвующих в распределении ролей
    /// </summary>
    private int GetParticipatingPlayerCount()
    {
        // Участвующие игроки - это те, кто голосует за карты
        var count = _player.Sessions
            .Select(p => p.UserId)
            .Count(userId => _mapStates.Any(mapState =>
                mapState.Value.Votes.GetValueOrDefault(userId, 0) > 0));

        Logger.Debug($"GetParticipatingPlayerCount returning {count}, mapStates: {string.Join(", ", _mapStates.Select(ms => $"{ms.Key}: {string.Join(",", ms.Value.Votes)}"))}");
        return count;
    }

        /// <summary>
    /// Возвращает любую запущенную карту (MapId) или MapId.Nullspace если ни одной нет.
    /// </summary>
    public MapId GetAnyLaunchedMapId()
    {
        return _launchedMaps.Values.FirstOrDefault();
    }

    /// <summary>
    /// Обновляет статус игрока в списке приоритетных на основе голосов за карты
    /// Игрок считается участвующим если у него есть голоса за карты
    /// </summary>
    private void UpdatePlayerPriorityStatus(NetUserId playerId)
    {
        // Проверяем, есть ли у игрока голоса за карты
        bool hasMapVotes = _mapStates.Any(mapState =>
            mapState.Value.Votes.GetValueOrDefault(playerId, 0) > 0);

        // Обновляем список игроков с приоритетами
        if (hasMapVotes && !_priorityPlayers.Contains(playerId))
        {
            _priorityPlayers.Add(playerId);
            Console.WriteLine($"Added player {playerId} to priority list (has map votes)");

            // Автоматически устанавливаем высокий приоритет для PlanetPrisoner всем новым участникам
            var prisonerState = GetOrCreateRoleState("PlanetPrisoner");
            prisonerState.Priorities[playerId] = 3; // High priority
            Console.WriteLine($"Automatically set high priority for {playerId} on PlanetPrisoner (new participant)");

            // Отправляем обновление состояния роли игроку
            SendRoleUpdateToPlayer(playerId, "PlanetPrisoner");
        }
        else if (!hasMapVotes && _priorityPlayers.Contains(playerId))
        {
            _priorityPlayers.Remove(playerId);
            Console.WriteLine($"Removed player {playerId} from priority list (no map votes)");

            // Сбрасываем приоритет PlanetPrisoner до значения по умолчанию (0) при выходе из списка участников
            var prisonerState = GetOrCreateRoleState("PlanetPrisoner");
            prisonerState.Priorities[playerId] = 0; // Reset to Never
            Console.WriteLine($"Reset priority for {playerId} on PlanetPrisoner (removed from participants)");

            // Отправляем обновление состояния роли игроку
            SendRoleUpdateToPlayer(playerId, "PlanetPrisoner");
        }
    }

    /// <summary>
    /// Отправляет обновление состояния голосования для всех карт конкретному игроку
    /// </summary>
    private void SendVotingStateToPlayer(NetUserId playerId)
    {
        if (!_player.TryGetSessionById(playerId, out var session))
            return;

        var participatingCount = GetParticipatingPlayerCount();
        var canStartRound = CanStartRound();
        var hasActiveVoting = HasActiveTimer("GLOBAL");
        var insufficientRoles = GetInsufficientRoles();

        foreach (var mapId in GetPrisonMapProtoIds().ToList())
        {
            var voteCount = GetVoteCount(mapId);
            var isLaunched = _launchedMaps.ContainsKey(mapId);

            var hasTimer = false;
            int? remainingSeconds = null;

            // Если активен глобальный таймер, показываем его только для выбранной карты
            if (!isLaunched && HasActiveTimer("GLOBAL") && _activeVotingMapId != null)
            {
                if (mapId == _activeVotingMapId && _mapStates.TryGetValue("GLOBAL", out var globalState))
                {
                    hasTimer = true;
                    remainingSeconds = globalState.RemainingSeconds;
                }
            }
            else if (!isLaunched && HasActiveTimer(mapId))
            {
                hasTimer = true;
                if (_mapStates.TryGetValue(mapId, out var mapState) && mapState.RemainingSeconds.HasValue)
                    remainingSeconds = mapState.RemainingSeconds;
            }

            var updateEvent = BuildVoteUpdateEvent(mapId, voteCount, hasTimer, isLaunched, remainingSeconds ?? 0, participatingCount, insufficientRoles.ToArray());
            RaiseNetworkEvent(updateEvent, session);
        }
    }

    /// <summary>
    /// Отправляет обновление состояния конкретной роли конкретному игроку
    /// </summary>
    private void SendRoleUpdateToPlayer(NetUserId playerId, string roleId)
    {
        if (!_player.TryGetSessionById(playerId, out var session))
            return;

        var roleState = GetOrCreateRoleState(roleId);
        var isTaken = roleState.AssignedPlayer != null;
        var isAssigned = roleState.AssignedPlayer == playerId;

        var updateEvent = new PlanetPrisonRoleUpdateEvent(roleId, isTaken, isAssigned);
        RaiseNetworkEvent(updateEvent, session);
    }

    /// <summary>
    /// Отправляет обновление состояния голосования для всех карт всем клиентам
    /// </summary>
    private void UpdateAllVotingStates()
    {
        // Получаем минимальное количество игроков из компонента
        var minPlayersRequired = GetMinPlayersRequired();

        var participatingCount = GetParticipatingPlayerCount();
        var canStartRound = CanStartRound();
        var hasActiveVoting = HasActiveTimer("GLOBAL");

        // Всегда показываем текущий список недостаточных ролей
        var insufficientRoles = GetInsufficientRoles();

        Console.WriteLine($"UpdateAllVotingStates called: participatingCount={participatingCount}, canStartRound={canStartRound}, hasActiveVoting={hasActiveVoting}, insufficientRoles={string.Join(",", insufficientRoles)}");

        foreach (var mapId in GetPrisonMapIdsForBroadcast())
        {
            var voteCount = GetVoteCount(mapId);
            var isLaunched = _launchedMaps.ContainsKey(mapId);

            // Запущенные карты не участвуют в голосовании
            var hasTimer = false;
            int? remainingSeconds = null;

            // Если активен глобальный таймер, отображаем его только для выбранной карты
            if (!isLaunched && HasActiveTimer("GLOBAL") && _activeVotingMapId != null)
            {
                if (mapId == _activeVotingMapId && _mapStates.TryGetValue("GLOBAL", out var globalState))
                {
                    hasTimer = true;
                    remainingSeconds = globalState.RemainingSeconds;
                }
            }
            // Поддержка возможных таймеров, привязанных к конкретной карте
            else if (!isLaunched && HasActiveTimer(mapId))
            {
                hasTimer = true;
                if (_mapStates.TryGetValue(mapId, out var mapState) && mapState.RemainingSeconds.HasValue)
                    remainingSeconds = mapState.RemainingSeconds;
            }

            var updateEvent = BuildVoteUpdateEvent(mapId, voteCount, hasTimer, isLaunched, remainingSeconds ?? 0, participatingCount, insufficientRoles.ToArray());
            RaiseNetworkEvent(updateEvent);
        }
    }

    /// <summary>
    /// Возвращает список ролей, для которых не хватает кандидатов
    /// </summary>
    private List<string> GetInsufficientRoles()
    {
        var insufficientRoles = new List<string>();
        var participatingPlayers = _player.Sessions
            .Select(p => p.UserId)
            .Where(userId => _mapStates.Any(mapState =>
                mapState.Value.Votes.GetValueOrDefault(userId, 0) > 0))
            .ToList();

        Logger.Debug($"GetInsufficientRoles - participatingPlayers: {string.Join(", ", participatingPlayers)}");

        foreach (var (roleId, requiredCount) in GetRoleQuotas())
        {
            var candidates = participatingPlayers
                .Where(playerId => GetOrCreateRoleState(roleId).Priorities.GetValueOrDefault(playerId, 0) > 0)
                .ToList();

            Logger.Debug($"Role {roleId} - required: {requiredCount}, candidates: {candidates.Count} ({string.Join(", ", candidates)})");

            if (candidates.Count < requiredCount)
                insufficientRoles.Add(roleId);
        }

        Logger.Debug($"GetInsufficientRoles returning: [{string.Join(", ", insufficientRoles)}]");

        return insufficientRoles;
    }

    /// <summary>
    /// Получить или создать состояние голосования для карты
    /// </summary>
    private MapVotingState GetOrCreateMapState(string mapId)
    {
        if (!_mapStates.TryGetValue(mapId, out var state))
        {
            var timer = new CancellationTokenSource();
            timer.Cancel(); // По умолчанию таймер отменен
            state = new MapVotingState(new Dictionary<NetUserId, int>(), timer);
            _mapStates[mapId] = state;
        }
        return state;
    }

    /// <summary>
    /// Получить или создать состояние роли
    /// </summary>
    private RoleState GetOrCreateRoleState(string roleId)
    {
        if (!_roleStates.TryGetValue(roleId, out var state))
        {
            state = new RoleState(new Dictionary<NetUserId, int>(), null);
            _roleStates[roleId] = state;
        }
        return state;
    }

    /// <summary>
    /// Получить количество активных голосов для карты
    /// </summary>
    private int GetVoteCount(string mapId)
    {
        return _mapStates.TryGetValue(mapId, out var state)
            ? state.Votes.Count(kvp => kvp.Value > 0)
            : 0;
    }

    /// <summary>
    /// Проверить, есть ли активный таймер для карты
    /// </summary>
    private bool HasActiveTimer(string mapId)
    {
        return _mapStates.TryGetValue(mapId, out var state) && !state.Timer.IsCancellationRequested;
    }

    public override void Initialize()
    {
        SubscribeLocalEvent<PlanetPrisonStationComponent, ComponentInit>(OnPlanetPrisonStationInit);
        SubscribeLocalEvent<PlanetPrisonStationComponent, ComponentShutdown>(OnPrisonShutdown);
        SubscribeLocalEvent<MapRemovedEvent>(OnMapRemoved);

        SubscribeNetworkEvent<PlanetPrisonVoteMessage>(OnPrisonVote);
        SubscribeNetworkEvent<PlanetPrisonStatusRequestMessage>(OnPrisonStatusRequest);
        SubscribeNetworkEvent<PlanetPrisonRolePriorityMessage>(OnRolePriority);

        // Сбрасываем состояние при старте нового раунда
        SubscribeLocalEvent<RoundStartedEvent>(OnRoundStarted);
        SubscribeLocalEvent<StationPostInitEvent>(OnStationPostInit);
        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRunLevelChanged);

        // Подписка на события смерти и превращения в призрака для автоматического удаления пустых карт
        SubscribeLocalEvent<PlanetPrisonSpawnedComponent, MobStateChangedEvent>(OnPrisonPlayerMobStateChanged);
        SubscribeLocalEvent<PlanetPrisonSpawnedComponent, PlayerDetachedEvent>(OnPrisonPlayerDetached);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
        SubscribeLocalEvent<GetObserverSpawnPointEvent>(OnGetObserverSpawnPoint);

        Log.Level = LogLevel.Info;
    }

    /// <summary>
    /// Находит координаты спавна для указанной роли на станции
    /// </summary>
    private EntityCoordinates? GetJobSpawnCoordinates(EntityUid station, string jobId)
    {
        var possiblePositions = new List<EntityCoordinates>();

        // Ищем все spawn points на станции
        var query = EntityQueryEnumerator<SpawnPointComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var spawnPoint, out var xform))
        {
            // Проверяем, принадлежит ли spawn point нашей станции
            if (_stationSystem.GetOwningStation(uid, xform) != station)
                continue;

            // Ищем spawn points для данной роли
            if (spawnPoint.SpawnType == SpawnPointType.Job &&
                spawnPoint.Job == jobId)
            {
                possiblePositions.Add(xform.Coordinates);
            }
        }

        // Если не нашли специальных spawn points для роли, ищем общие job spawn points
        if (possiblePositions.Count == 0)
        {
            query = EntityQueryEnumerator<SpawnPointComponent, TransformComponent>();
            while (query.MoveNext(out var uid, out var spawnPoint, out var xform))
            {
                if (_stationSystem.GetOwningStation(uid, xform) != station)
                    continue;

                if (spawnPoint.SpawnType == SpawnPointType.Job &&
                    string.IsNullOrEmpty(spawnPoint.Job))
                {
                    possiblePositions.Add(xform.Coordinates);
                }
            }
        }

        // Если всё ещё нет, берем любой LateJoin spawn point
        if (possiblePositions.Count == 0)
        {
            query = EntityQueryEnumerator<SpawnPointComponent, TransformComponent>();
            while (query.MoveNext(out var uid, out var spawnPoint, out var xform))
            {
                if (_stationSystem.GetOwningStation(uid, xform) != station)
                    continue;

                if (spawnPoint.SpawnType == SpawnPointType.LateJoin)
                {
                    possiblePositions.Add(xform.Coordinates);
                }
            }
        }

        // Возвращаем случайную позицию или null если ничего не найдено
        return possiblePositions.Count > 0 ? _random.Pick(possiblePositions) : null;
    }

    private void OnPrisonShutdown(EntityUid uid, PlanetPrisonStationComponent component, ComponentShutdown args)
    {
        QueueDel(component.Entity);
        component.Entity = EntityUid.Invalid;

        if (_mapManager.MapExists(component.MapId))
            _mapManager.DeleteMap(component.MapId);

        component.MapId = MapId.Nullspace;
    }

    private void OnPlanetPrisonStationInit(EntityUid uid, PlanetPrisonStationComponent component, ComponentInit args)
    {
        var enable = _cfg.GetCVar(SunriseCCVars.MinPlayersEnable);
        if (!enable)
            return;

        var minPlayers = _cfg.GetCVar(SunriseCCVars.MinPlayersPlanetPrison);
        if (_player.PlayerCount <= minPlayers)
        {
            _chat.DispatchServerAnnouncement(Loc.GetString("planet-prison-not-enough-players", ("minimumPlayers", minPlayers)), Color.OrangeRed);
            return;
        }

        if (component.MapId != MapId.Nullspace)
            return;

        AddPlanetPrison(component);
    }

    private void AddPlanetPrison(PlanetPrisonStationComponent component)
    {
        var query = AllEntityQuery<PlanetPrisonStationComponent>();

        while (query.MoveNext(out var otherComp))
        {
            if (otherComp == component)
                continue;

            component.MapId = otherComp.MapId;
            return;
        }

        var station = ChooseType(component);

        if (!_prototypeManager.TryIndex(_random.Pick(component.Biomes), out var biome))
        {
            Log.Warning("No Prison map found, skipping setup.");
            return;
        }

        if (!_prototypeManager.TryIndex(station, out var gameMap))
        {
            Log.Warning("No Prison map found, skipping setup.");
            return;
        }

        _chat.DispatchServerAnnouncement(Loc.GetString("planet-prison-select-map", ("stationName", gameMap.MapName)), Color.LightBlue);
        _chat.DispatchServerAnnouncement(Loc.GetString("planet-prison-select-biome", ("biomeName", biome.ID)), Color.LightBlue);

        var opts = DeserializationOptions.Default with {InitializeMaps = true};
        var uids = _gameTicker.LoadGameMap(gameMap, out var mapId, opts, rot: Angle.Zero);

        component.MapId = mapId;

        if (uids.Count != 1)
        {
            Log.Warning("Prison station have more 1 grid.");
            QueueDel(component.Entity);
            component.Entity = EntityUid.Invalid;

            if (_mapManager.MapExists(component.MapId))
                _mapManager.DeleteMap(component.MapId);

            component.MapId = MapId.Nullspace;
            return;
        }

        EnsureComp<IgnoreFtlCheckComponent>(uids[0]);
        component.PrisonGrid = uids[0];

        var mapUid = _mapManager.GetMapEntityId(mapId);
        _biomeSystem.EnsurePlanet(mapUid, biome);

        var restricted = new RestrictedRangeComponent
        {
            Origin = new Vector2(0, 0),
            Range = 200,
        };
        AddComp(mapUid, restricted);

        EnsureComp<LightCycleComponent>(mapUid);

        var destComp = _entManager.EnsureComponent<FTLDestinationComponent>(mapUid);
        destComp.BeaconsOnly = true;
        _shuttle.SetFTLWhitelist(mapUid, component.ShuttleWhitelist);
    }

    private ProtoId<GameMapPrototype> ChooseType(PlanetPrisonStationComponent component)
    {
        return _cfg.GetCVar(SunriseCCVars.PlanetPrisonModern)
            ? _random.Pick(component.StationsModern)
            : _random.Pick(component.StationsOld);
    }

    private void OnPrisonVote(PlanetPrisonVoteMessage msg, EntitySessionEventArgs args)
    {
        var player = args.SenderSession;
        if (player == null)
            return;

        // Блокируем голосование за карту, которая уже запущена
        if (_launchedMaps.ContainsKey(msg.MapId))
        {
            Console.WriteLine($"BLOCKED: Map {msg.MapId} already launched, ignoring vote from {player.Name}");
            return;
        }

        // Блокируем голосование, если глобальное голосование уже запущено
        if (HasActiveTimer("GLOBAL"))
        {
            Console.WriteLine($"BLOCKED: Global vote in progress, ignoring vote from {player.Name}");
            return;
        }

        Console.WriteLine($"Received prison vote from {player.Name}: map={msg.MapId}, priority={msg.Priority}");

        // Игрок может голосовать за несколько карт одновременно, не удаляем старые голоса

        // Получаем или создаем состояние карты
        var mapState = GetOrCreateMapState(msg.MapId);
        var oldPriority = mapState.Votes.GetValueOrDefault(player.UserId, 0);
        mapState.Votes[player.UserId] = msg.Priority;

        // Обновляем статус игрока в списке приоритетных после изменения голоса за карту
        UpdatePlayerPriorityStatus(player.UserId);

        // Обновляем состояние голосования для всех карт после изменения голосов
        UpdateAllVotingStates();

        var playerCount = _priorityPlayers.Count;

        // Получаем минимальное количество игроков из компонента
        var minPlayersRequired = GetMinPlayersRequired();

        // UpdateAllVotingStates() уже отправляет обновления всем клиентам выше

        // Проверяем выполнение квоты ролей для запуска голосования
        if (!CanStartRound())
        {
            var insufficientRoles = GetInsufficientRoles();
            Console.WriteLine($"BLOCKED: Cannot start vote - insufficient role quotas: {string.Join(", ", insufficientRoles)}");
            return; // Ранний выход, если квота не выполнена
        }

        // Если квота выполнена, начинаем глобальное голосование
        Console.WriteLine($"Checking vote start: quota fulfilled, hasGlobalTimer={HasActiveTimer("GLOBAL")}");
        if (!HasActiveTimer("GLOBAL"))
        {
            Console.WriteLine("STARTING global prison vote - quota fulfilled");
            StartGlobalPrisonVote();
        }
        else
        {
            Console.WriteLine("Vote already in progress");
        }
    }

    private void OnRoundStarted(RoundStartedEvent ev)
    {
        Logger.Info("[CACHE] === ROUND STARTED EVENT RECEIVED ===");
        Logger.Info($"[CACHE] Round ID: {ev.RoundId}");

        // Сбрасываем голосования карт и запущенные карты при старте нового раунда
        _launchedMaps.Clear();

        // Сбрасываем флаг кэширования для следующего раунда
        _cachePreloaded = false;

        foreach (var mapState in _mapStates.Values)
        {
            mapState.Timer.Cancel();
        }
        _mapStates.Clear();

        // Сбрасываем только голоса карт и пул игроков, сохраняем приоритеты ролей между раундами
        ResetMapVotingOnly();

        // Загружаем карты тюрьмы ПОСЛЕ спавна игроков и начала раунда
        Logger.Info("[CACHE] Round started - starting prison maps loading immediately");

        try
        {
            Console.WriteLine("[CACHE] === STARTING PRISON MAPS LOADING SYNCHRONOUSLY ===");
            // Вызываем синхронную версию загрузки карт напрямую
            PreloadAllPrisonMapsAtRoundStart();
            Console.WriteLine("[CACHE] Prison maps loaded successfully after round start");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CACHE] Failed to load prison maps after round start: {ex.Message}");
            Console.WriteLine($"[CACHE] Stack trace: {ex.StackTrace}");
        }

        Logger.Info("Prison voting reset on round start, role priorities preserved");
    }

    /// <summary>
    /// Обработчик изменения уровня выполнения игры
    /// </summary>
    private void OnRunLevelChanged(GameRunLevelChangedEvent ev)
    {
        // При переходе в лобби сбрасываем флаг для следующего раунда
        if (ev.New == GameRunLevel.PreRoundLobby)
        {
            _cachePreloaded = false;
        }
    }

    /// <summary>
    /// Запускает предварительную загрузку кэша карт тюрьмы
    /// </summary>
    private void StartPrisonCachePreload()
    {
        if (_cachePreloaded)
        {
            Console.WriteLine("[CACHE] Cache preload already completed");
            return;
        }

        _cachePreloaded = true;
        Console.WriteLine("[CACHE] Starting prison map preload");

        // Загружаем все карты при старте раунда, до спавна игроков
        PreloadAllPrisonMapsAtRoundStart();
    }


    /// <summary>
    /// Обработчик события инициализации станции
    /// </summary>
    private void OnStationPostInit(ref StationPostInitEvent ev)
    {
        // Станция инициализирована
        // Загрузка карт тюрьмы теперь происходит в OnRoundStarted (после спавна игроков)
        Logger.Info($"[STATION] Station initialized: {ev.Station}");
    }


    private int GetMinPlayersRequiredForMap(string protoId)
    {
        const int defaultMin = 2;

        if (string.IsNullOrEmpty(protoId))
            return defaultMin;

        var mapId = _launchedMaps.GetValueOrDefault(protoId);
        if (mapId != MapId.Nullspace && _mapManager.MapExists(mapId))
        {
            var stationQuery = EntityQueryEnumerator<StationDataComponent>();
            while (stationQuery.MoveNext(out var uid, out _))
            {
                if (_transform.GetMapId(uid) != mapId)
                    continue;
                if (TryComp<PlanetPrisonMapSettingsComponent>(uid, out var mapSettings) &&
                    mapSettings.GameplaySettingsOverride is { } gs)
                {
                    return gs.MinPlayersRequired;
                }
            }
        }

        if (_prototypeManager.TryIndex<GameMapPrototype>(protoId, out var proto) && proto.MinPlayers > 0)
            return (int)proto.MinPlayers;

        return defaultMin;
    }

    /// <summary>
    /// Helper to build a PlanetPrisonVoteUpdateEvent with MinPlayers populated.
    /// Centralizes event construction so all broadcasts use consistent fields.
    /// </summary>
    private PlanetPrisonVoteUpdateEvent BuildVoteUpdateEvent(string mapId, int voteCount, bool isVoting, bool isLaunched, double remainingSeconds, int totalPriorityPlayers, string[] insufficientRoles, bool isHidden = false)
    {
        var minPlayersForMap = GetMinPlayersRequiredForMap(mapId);
        var launchedState = _launchedMaps.ContainsKey(mapId) ? "launched" : "not-launched";

        // Sunrise-Edit: Add logging to track state consistency
        Console.WriteLine($"[SERVER-LOG] Building vote update for {mapId}: votes={voteCount}, isVoting={isVoting}, isLaunched={isLaunched}, remaining={remainingSeconds:F1}s, launchedMaps={launchedState}");

        // Additional validation: if map is in _launchedMaps, isLaunched should be true
        if (_launchedMaps.ContainsKey(mapId) && !isLaunched)
        {
            Console.WriteLine($"[SERVER-WARN] Inconsistency detected for {mapId}: map is in _launchedMaps but isLaunched=false. Fixing to true.");
            isLaunched = true;
        }

        return new PlanetPrisonVoteUpdateEvent(mapId, voteCount, isVoting, isLaunched, remainingSeconds, totalPriorityPlayers, insufficientRoles, minPlayersForMap, isHidden);
    }

    /// <summary>
    /// Гарантирует, что очереди кэша для карт тюрьмы инициализированы (по одному на каждый protoId из списка).
    /// </summary>
    private void InitializeCacheQueues()
    {
        var protoIds = GetPrisonMapProtoIds().ToList();
        foreach (var protoId in protoIds)
        {
            if (!_cachedMaps.ContainsKey(protoId) || _cachedMaps[protoId] == null)
                _cachedMaps[protoId] = new Queue<MapId>();
        }
    }

    /// <summary>
    /// Асинхронно загружает все карты тюрьмы после начала раунда.
    /// </summary>
    private async Task PreloadAllPrisonMapsAsync()
    {
        Console.WriteLine("[CACHE] === STARTING ASYNC PRISON MAP LOADING ===");
        Console.WriteLine($"[CACHE] Current thread: {Thread.CurrentThread.ManagedThreadId}");

        try
        {
            InitializeCacheQueues();
            var startTime = System.Diagnostics.Stopwatch.StartNew();
            var protoIds = GetPrisonMapProtoIds().ToList();

            foreach (var protoId in protoIds)
            {
                var displayName = _prototypeManager.TryIndex<GameMapPrototype>(protoId, out var p) ? p.MapName : protoId;
                var count = GetPrisonCacheSizeForProto(protoId);
                Console.WriteLine($"[CACHE] Loading {displayName} ({protoId}) x{count}...");
                await LoadMapsOfTypeAsync(protoId, count, displayName);
            }

            startTime.Stop();
            LogCacheCompletion(startTime);
            Console.WriteLine("[CACHE] Async prison map loading completed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CACHE] Failed to preload prison maps asynchronously: {ex.Message}");
            Console.WriteLine($"[CACHE] Exception details: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// Синхронно загружает все карты тюрьмы (для совместимости).
    /// </summary>
    private void PreloadAllPrisonMapsAtRoundStart()
    {
        Console.WriteLine("[CACHE] Loading all prison maps synchronously (fallback)");

        InitializeCacheQueues();
        var startTime = System.Diagnostics.Stopwatch.StartNew();
        var protoIds = GetPrisonMapProtoIds().ToList();

        try
        {
            foreach (var protoId in protoIds)
            {
                var displayName = _prototypeManager.TryIndex<GameMapPrototype>(protoId, out var p) ? p.MapName : protoId;
                var count = GetPrisonCacheSizeForProto(protoId);
                LoadMapsOfType(protoId, count, displayName);
            }

            startTime.Stop();
            LogCacheCompletion(startTime);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CACHE] Failed to preload prison maps: {ex.Message}");
        }
    }

    private async Task LoadMapsOfTypeAsync(string mapType, int count, string displayName)
    {
        for (int i = 1; i <= count; i++)
        {
            Console.WriteLine($"[CACHE] Loading {displayName} #{i}");
            var mapStart = System.Diagnostics.Stopwatch.StartNew();
            await PreloadSingleMapAsync(mapType, i - 1);
            mapStart.Stop();
            Console.WriteLine($"[CACHE] {displayName} #{i} loaded in {mapStart.Elapsed.TotalSeconds:F2}s");
        }
    }

    private void LoadMapsOfType(string mapType, int count, string displayName)
    {
        for (int i = 1; i <= count; i++)
        {
            Console.WriteLine($"[CACHE] Loading {displayName} #{i}");
            var mapStart = System.Diagnostics.Stopwatch.StartNew();
            PreloadSingleMap(mapType, i - 1);
            mapStart.Stop();
            Console.WriteLine($"[CACHE] {displayName} #{i} loaded in {mapStart.Elapsed.TotalSeconds:F2}s");
        }
    }

    private void LogCacheCompletion(System.Diagnostics.Stopwatch startTime)
    {
        startTime.Stop();
        var parts = new List<string>();
        foreach (var protoId in GetPrisonMapProtoIds())
        {
            var count = _cachedMaps.TryGetValue(protoId, out var q) ? q.Count : 0;
            var name = _prototypeManager.TryIndex<GameMapPrototype>(protoId, out var p) ? p.MapName : protoId;
            parts.Add($"{count} {name}");
        }
        Console.WriteLine($"[CACHE] All prison maps loaded: {string.Join(", ", parts)}");
        Console.WriteLine($"[CACHE] Total load time: {startTime.Elapsed.TotalSeconds:F1}s");
    }



    /// <summary>
    /// Асинхронно предварительно загружает одну карту тюрьмы
    /// </summary>
    private async Task PreloadSingleMapAsync(string mapProtoId, int? cacheIndex = null)
    {
        var mapLoadStart = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            Console.WriteLine($"[CACHE] Starting preload of {mapProtoId}...");

            var cacheMapId = GetNextFreeMapIdForProto(mapProtoId, cacheIndex);
            if (cacheMapId == -1)
            {
                Console.WriteLine($"[CACHE] Cannot find free map ID for {mapProtoId}");
                return;
            }

            var mapIdObj = new MapId(cacheMapId);
            if (_mapManager.MapExists(mapIdObj))
            {
                Console.WriteLine($"[CACHE] MapId {cacheMapId} already exists for {mapProtoId}");
                return;
            }

            // Загружаем карту БЕЗ инициализации, чтобы не создавать станции
            var loadMapStart = System.Diagnostics.Stopwatch.StartNew();
            var opts = new DeserializationOptions { InitializeMaps = false };

            if (!_prototypeManager.TryIndex<GameMapPrototype>(mapProtoId, out var gameMapProto))
            {
                Console.WriteLine($"[CACHE] Unknown GameMapPrototype: {mapProtoId}");
                return;
            }

            var uids = _gameTicker.LoadGameMapWithId(gameMapProto, mapIdObj, opts);
            loadMapStart.Stop();

            Console.WriteLine($"[CACHE] Map loading: {uids.Count} entities in {loadMapStart.Elapsed.TotalSeconds:F2}s");

            if (uids.Count == 0)
            {
                Console.WriteLine($"[CACHE] Failed to preload {mapProtoId} - no entities loaded");
                return;
            }

            // Инициализируем карту вручную (поскольку загружали с InitializeMaps = false)
            var initStart = System.Diagnostics.Stopwatch.StartNew();
            if (_entManager.System<SharedMapSystem>().IsInitialized(mapIdObj))
            {
                Console.WriteLine($"[CACHE] Map {mapProtoId} is already initialized");
            }
            else
            {
                Console.WriteLine($"[CACHE] Manually initializing map {mapProtoId}...");
                _entManager.System<SharedMapSystem>().InitializeMap(mapIdObj);
            }
            initStart.Stop();
            Console.WriteLine($"[CACHE] Map initialization: {initStart.Elapsed.TotalMilliseconds:F0}ms");

            // Добавляем в очередь кэша
            if (_cachedMaps.TryGetValue(mapProtoId, out var queue))
            {
                queue.Enqueue(mapIdObj);
                Console.WriteLine($"[CACHE] Added {mapProtoId} to cache queue (total in queue: {queue.Count})");
            }

            mapLoadStart.Stop();
            Console.WriteLine($"[CACHE] {mapProtoId} fully cached in {mapLoadStart.Elapsed.TotalSeconds:F2}s (MapId: {cacheMapId})");
        }
        catch (Exception ex)
        {
            mapLoadStart.Stop();
            Console.WriteLine($"[CACHE] Failed to preload {mapProtoId} after {mapLoadStart.Elapsed.TotalSeconds:F2}s: {ex.Message}");
        }
    }

    /// <summary>
    /// Предварительно загружает одну карту тюрьмы
    /// </summary>
    private void PreloadSingleMap(string mapProtoId, int? cacheIndex = null)
    {
        var mapLoadStart = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            Console.WriteLine($"[CACHE] Starting preload of {mapProtoId}...");

            var cacheMapId = GetNextFreeMapIdForProto(mapProtoId, cacheIndex);
            if (cacheMapId == -1)
            {
                Console.WriteLine($"[CACHE] Cannot find free map ID for {mapProtoId}");
                return;
            }

            var mapIdObj = new MapId(cacheMapId);
            if (_mapManager.MapExists(mapIdObj))
            {
                Console.WriteLine($"[CACHE] MapId {cacheMapId} already exists for {mapProtoId}");
                return;
            }

            // Загружаем карту БЕЗ инициализации, чтобы не создавать станции
            var loadMapStart = System.Diagnostics.Stopwatch.StartNew();
            var opts = new DeserializationOptions { InitializeMaps = false };

            if (!_prototypeManager.TryIndex<GameMapPrototype>(mapProtoId, out var gameMapProto))
            {
                Console.WriteLine($"[CACHE] Unknown GameMapPrototype: {mapProtoId}");
                return;
            }

            var uids = _gameTicker.LoadGameMapWithId(gameMapProto, mapIdObj, opts);
            loadMapStart.Stop();

            Console.WriteLine($"[CACHE] Map loading: {uids.Count} entities in {loadMapStart.Elapsed.TotalSeconds:F2}s");

            if (uids.Count == 0)
            {
                Console.WriteLine($"[CACHE] Failed to preload {mapProtoId} - no entities loaded");
                return;
            }

            // Инициализируем карту вручную (поскольку загружали с InitializeMaps = false)
            var initStart = System.Diagnostics.Stopwatch.StartNew();
            if (_entManager.System<SharedMapSystem>().IsInitialized(mapIdObj))
            {
                Console.WriteLine($"[CACHE] Map {mapProtoId} is already initialized");
            }
            else
            {
                Console.WriteLine($"[CACHE] Manually initializing map {mapProtoId}...");
                _entManager.System<SharedMapSystem>().InitializeMap(mapIdObj);
            }
            initStart.Stop();
            Console.WriteLine($"[CACHE] Map initialization: {initStart.Elapsed.TotalMilliseconds:F0}ms");

            // Добавляем в очередь кэша
            if (_cachedMaps.TryGetValue(mapProtoId, out var queue))
            {
                queue.Enqueue(mapIdObj);
                Console.WriteLine($"[CACHE] Added {mapProtoId} to cache queue (total in queue: {queue.Count})");
            }

            // Делаем карту паузурованной
            _map.SetPaused(mapIdObj, true);

            mapLoadStart.Stop();
            Console.WriteLine($"[CACHE] {mapProtoId} fully cached in {mapLoadStart.Elapsed.TotalSeconds:F2}s (MapId: {cacheMapId})");
        }
        catch (Exception ex)
        {
            mapLoadStart.Stop();
            Console.WriteLine($"[CACHE] Failed to preload {mapProtoId} after {mapLoadStart.Elapsed.TotalSeconds:F2}s: {ex.Message}");
        }
    }

    /// <summary>
    /// Очищает кэш загруженных карт тюрьмы
    /// </summary>
    private void ClearMapCache()
    {
        Console.WriteLine($"[CACHE] Clearing prison map cache ({_cachedMaps.Count} map types)...");

        var totalMapsDeleted = 0;
        foreach (var (protoId, mapQueue) in _cachedMaps)
        {
            Console.WriteLine($"[CACHE] Clearing cache for {protoId} ({mapQueue.Count} maps)");
            foreach (var mapId in mapQueue)
            {
                try
                {
                    if (_mapManager.MapExists(mapId))
                    {
                        Console.WriteLine($"[CACHE] Deleting cached prison map {protoId} (MapId: {mapId})");
                        _mapManager.DeleteMap(mapId);
                        totalMapsDeleted++;
                    }
                    else
                    {
                        Console.WriteLine($"[CACHE] Map {mapId} for {protoId} no longer exists");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CACHE] Failed to delete cached prison map {protoId} (MapId: {mapId}): {ex.Message}");
                }
            }
        }

        _cachedMaps.Clear();
        Console.WriteLine($"[CACHE] Prison map cache cleared - deleted {totalMapsDeleted} cached maps");
    }

    private void AssignPrisonRoles(string selectedMapId)
    {

        // Получаем участвующих игроков (голосующих за карты)
        var participatingPlayers = _player.Sessions
            .Select(p => p.UserId)
            .Where(userId => _mapStates.Any(mapState =>
                mapState.Value.Votes.GetValueOrDefault(userId, 0) > 0))
            .ToList();

        Console.WriteLine($"Found {participatingPlayers.Count} participating players: {string.Join(", ", participatingPlayers.OrderBy(id => id.UserId))}");

        if (participatingPlayers.Count == 0)
        {
            Console.WriteLine("No participating players, skipping role assignment");
            return;
        }

        // Распределяем роли по уровням приоритета (от высшего к низшему)
        var assignedPlayers = new HashSet<NetUserId>();
        var assignedRoles = new HashSet<string>();

        // Проходим по уровням приоритета: Высокий (3) → Средний (2) → Низкий (1)
        for (int priorityLevel = 3; priorityLevel >= 1; priorityLevel--)
        {

            // Для каждой роли собираем кандидатов с текущим уровнем приоритета
            foreach (var roleId in GetRoleQuotas().Keys)
            {
                if (assignedRoles.Contains(roleId))
                    continue; // Роль уже назначена

                var requiredCount = GetRoleQuotas()[roleId];
                var alreadyAssigned = assignedRoles.Count(r => r == roleId);
                var slotsLeft = requiredCount - alreadyAssigned;

                if (slotsLeft <= 0)
                    continue; // Все слоты для этой роли заняты

                // Находим кандидатов с нужным приоритетом на эту роль
                var candidates = participatingPlayers
                    .Where(playerId =>
                        !assignedPlayers.Contains(playerId) && // Не назначен другой роли
                        GetOrCreateRoleState(roleId).Priorities.GetValueOrDefault(playerId, 0) == priorityLevel)
                    .ToList();


                if (candidates.Count == 0)
                    continue;

                // Случайно выбираем игроков для доступных слотов
                var selectedCandidates = candidates
                    .OrderBy(_ => _random.Next()) // Случайный порядок
                    .Take(slotsLeft)
                    .ToList();

                foreach (var playerId in selectedCandidates)
                {
                    // Назначаем роль игроку
                    var roleState = GetOrCreateRoleState(roleId);
                    _roleStates[roleId] = roleState with { AssignedPlayer = playerId };
                    assignedPlayers.Add(playerId);
                    assignedRoles.Add(roleId);


                    // Отправляем обновление игроку
                    var updateEvent = new PlanetPrisonRoleUpdateEvent(roleId, false, true);
                    RaiseNetworkEvent(updateEvent);

                    // Спавним игрока с ролью на правильной карте
                    SpawnPlayerWithRole(playerId, roleId, selectedMapId);
                }
            }
        }

        // Проверяем, все ли роли распределены
        var unfilledRoles = GetRoleQuotas()
            .Where(kvp => assignedRoles.Count(r => r == kvp.Key) < kvp.Value)
            .Select(kvp => $"{kvp.Key}({assignedRoles.Count(r => r == kvp.Key)}/{kvp.Value})")
            .ToList();


        if (unfilledRoles.Any())
        {
            Console.WriteLine($"Unfilled role quotas: {string.Join(", ", unfilledRoles)}");
        }
        else
        {
            Console.WriteLine("All role quotas filled successfully!");
        }
    }

    private void SpawnPlayerWithRole(NetUserId playerId, string roleId, string selectedMapId)
    {
        // Получаем сессию игрока
        if (!_player.TryGetSessionById(playerId, out var session))
        {
            Console.WriteLine($"Cannot find session for player {playerId} to assign role {roleId}");
            return;
        }

        // Определяем прототип роли
        var jobProtoId = roleId switch
        {
            "PlanetPrisoner" => "PlanetPrisoner",
            "HeadOfPrison" => "HeadOfPrison",
            "PrisonInspector" => "PrisonInspector",
            "PrisonWorker" => "PrisonWorker",
            "PrisonEngineer" => "PrisonEngineer",
            "PrisonScientist" => "PrisonScientist",
            "PrisonDoctor" => "PrisonDoctor",
            "PrisonChef" => "PrisonChef",
            "PrisonTrainee" => "PrisonTrainee",
            _ => null
        };

        if (jobProtoId == null)
        {
            Console.WriteLine($"Unknown role ID: {roleId}");
            return;
        }

        Console.WriteLine($"Spawning player {session.Name} with role {roleId} ({jobProtoId})");

        try
        {
            // Определяем карту для спавна игрока на основе его голосов
            // Если игрок голосовал за выбранную карту, используем её
            // Иначе находим карту с наивысшим приоритетом голоса игрока
            MapId prisonMapId = MapId.Nullspace;
            
            // Проверяем, голосовал ли игрок за выбранную карту
            if (_mapStates.TryGetValue(selectedMapId, out var selectedMapState) &&
                selectedMapState.Votes.GetValueOrDefault(playerId, 0) > 0 &&
                _launchedMaps.TryGetValue(selectedMapId, out var selectedMapIdValue))
            {
                prisonMapId = selectedMapIdValue;
                Console.WriteLine($"Player {session.Name} voted for selected map {selectedMapId}, spawning on MapId {prisonMapId}");
            }
            else
            {
                // Ищем карту с наивысшим приоритетом голоса игрока среди запущенных карт
                var bestMapId = string.Empty;
                var bestPriority = 0;
                
                foreach (var kvp in _launchedMaps)
                {
                    var mapProtoId = kvp.Key;
                    if (_mapStates.TryGetValue(mapProtoId, out var mapState))
                    {
                        var playerPriority = mapState.Votes.GetValueOrDefault(playerId, 0);
                        if (playerPriority > bestPriority)
                        {
                            bestPriority = playerPriority;
                            bestMapId = mapProtoId;
                        }
                    }
                }
                
                if (!string.IsNullOrEmpty(bestMapId) && _launchedMaps.TryGetValue(bestMapId, out var bestMapIdValue))
                {
                    prisonMapId = bestMapIdValue;
                    Console.WriteLine($"Player {session.Name} voted for map {bestMapId} with priority {bestPriority}, spawning on MapId {prisonMapId}");
                }
                else
                {
                    // Fallback: используем первую доступную запущенную карту
                    prisonMapId = _launchedMaps.Values.FirstOrDefault();
                    Console.WriteLine($"Player {session.Name} has no votes for launched maps, using fallback MapId {prisonMapId}");
                }
            }
            
            if (prisonMapId == MapId.Nullspace)
            {
                Console.WriteLine($"No prison map is currently launched for role assignment");
                return;
            }

            // Получаем станцию по MapId
            var station = _stationSystem.GetStationInMap(prisonMapId);
            if (station == null)
            {
                Console.WriteLine($"Cannot find station for map {prisonMapId}");
                return;
            }

            // Ищем координаты спавна для роли
            var coordinates = GetJobSpawnCoordinates(station.Value, jobProtoId);
            if (!coordinates.HasValue)
            {
                Console.WriteLine($"Cannot find spawn coordinates for job {jobProtoId} on station {station.Value}");
                return;
            }

            // Получаем реальный профиль игрока
            var preferences = _prefsManager.GetPreferences(playerId);
            var profile = (HumanoidCharacterProfile?)preferences?.SelectedCharacter ?? HumanoidCharacterProfile.DefaultWithSpecies();

            // Создаем персонажа без job (чтобы избежать автоматического применения starting gear)
            var character = _stationSpawning.SpawnPlayerMob(coordinates.Value, null, profile, station.Value);

            // Помечаем персонажа как заспавненного на тюремной карте, чтобы в дальнейшем
            // можно было ограничивать голосования только для игроков этой карты.
            var spawnedMarker = EnsureComp<PlanetPrisonSpawnedComponent>(character);
            spawnedMarker.MapId = prisonMapId;

            // Создаем Mind для игрока
            var newMind = _mind.CreateMind(playerId, session.Name);
            _mind.SetUserId(newMind, playerId);

            // Переносим Mind в персонажа
            _mind.TransferTo(newMind, character);

            // Назначаем роль персонажу
            _roles.MindAddJobRole(newMind, silent: false, jobPrototype: jobProtoId);

            // Применяем снаряжение роли
            if (_prototypeManager.TryIndex<JobPrototype>(jobProtoId, out var jobProto) &&
                jobProto != null &&
                jobProto.StartingGear != null)
            {
                // Используем OutfitSystem для применения снаряжения
                var outfitSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<Content.Server.Clothing.Systems.OutfitSystem>();
                outfitSystem.SetOutfit(character, jobProto.StartingGear);
            }

            // Закрываем интерфейс тюрьмы у игрока
            RaiseNetworkEvent(new PlanetPrisonCloseWindowEvent(), session);

            Console.WriteLine($"Successfully spawned player {session.Name} as {roleId} on prison map");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error spawning player {session.Name} with role {roleId}: {e.Message}");
        }
    }

    private void ResetAllPrioritiesAndPlayers()
    {
        // Сбрасываем список игроков с приоритетами
        _priorityPlayers.Clear();

        // Сбрасываем все голоса карт до "Никогда" (0)
        foreach (var mapState in _mapStates.Values)
        {
            foreach (var userId in mapState.Votes.Keys.ToList())
            {
                mapState.Votes[userId] = 0; // Никогда
            }
        }

        // Сбрасываем все приоритеты ролей до "Никогда" (0)
        foreach (var roleId in _roleStates.Keys.ToList())
        {
            var roleState = _roleStates[roleId];
            foreach (var userId in roleState.Priorities.Keys.ToList())
            {
                roleState.Priorities[userId] = 0; // Никогда
            }
            // Сбрасываем назначения ролей
            _roleStates[roleId] = roleState with { AssignedPlayer = null };
        }

        Console.WriteLine("All map and role priorities reset to Never, assignments cleared, and player list reset");
    }

    private void ResetMapVotingOnly()
    {
        // Сбрасываем список игроков с приоритетами
        _priorityPlayers.Clear();

        // Сбрасываем все голоса карт до "Никогда" (0)
        foreach (var mapState in _mapStates.Values)
        {
            foreach (var userId in mapState.Votes.Keys.ToList())
            {
                mapState.Votes[userId] = 0; // Никогда
            }
        }

        // Сбрасываем назначения ролей (но сохраняем приоритеты!)
        foreach (var roleId in _roleStates.Keys.ToList())
        {
            var roleState = _roleStates[roleId];
            // НЕ сбрасываем Priorities - сохраняем приоритеты ролей!
            // Сбрасываем только назначения ролей
            _roleStates[roleId] = roleState with { AssignedPlayer = null };
        }

        Console.WriteLine("Map voting reset but role priorities preserved");
    }

    private void SendFinalUpdatesAfterLaunch(string launchedMapId)
    {
        // Отправляем обновления для всех карт
        foreach (var mapId in GetPrisonMapProtoIds().ToList())
        {
            var voteCount = 0; // Все голоса сброшены
            // Для запущенной карты отправляем RemainingSeconds = 0 чтобы клиент показал "(запускается)" и затем "(запущен)"
            var remainingSeconds = (mapId == launchedMapId) ? 0 : (int?)null;
            var isLaunched = _launchedMaps.ContainsKey(mapId);
            var updateEvent = BuildVoteUpdateEvent(mapId, voteCount, false, isLaunched, remainingSeconds ?? 0, 0, Array.Empty<string>());
            RaiseNetworkEvent(updateEvent);
        }

        Console.WriteLine($"Final updates sent after launching {launchedMapId}");
    }

    private void SendGlobalStateReset()
    {
        Console.WriteLine("Sending global state reset to all clients");

        // Получаем минимальное количество игроков из компонента
        var minPlayersRequired = GetMinPlayersRequired();

        // Отправляем специальное сообщение о глобальном сбросе для каждой карты
        foreach (var mapId in GetPrisonMapProtoIds().ToList())
        {
            // Используем специальную комбинацию параметров для обозначения глобального сброса:
            // TotalPriorityPlayers = -1 (специальный флаг)
            var isLaunched = _launchedMaps.ContainsKey(mapId);
            var minForReset = GetMinPlayersRequiredForMap(mapId);
            var updateEvent = BuildVoteUpdateEvent(mapId, 0, false, isLaunched, 0, -1, Array.Empty<string>());
            RaiseNetworkEvent(updateEvent);
            Console.WriteLine($"Sent reset for {mapId}");
        }

        Console.WriteLine("Global state reset notifications sent to all clients");
    }

    private void OnPrisonStatusRequest(PlanetPrisonStatusRequestMessage msg, EntitySessionEventArgs args)
    {
        // Обновляем статусы всех игроков перед вычислением participatingCount
        foreach (var session in _player.Sessions)
        {
            UpdatePlayerPriorityStatus(session.UserId);
        }

        // Получаем минимальное количество игроков из компонента
        var minPlayersRequired = GetMinPlayersRequired();

        // Отправляем текущее состояние голосования клиенту
        var voteCount = GetVoteCount(msg.MapId);

        var isLaunched = _launchedMaps.ContainsKey(msg.MapId);
        var participatingCount = GetParticipatingPlayerCount();
        var canStartRound = CanStartRound();
        var hasActiveVoting = HasActiveTimer("GLOBAL");

        // Всегда показываем текущий список недостаточных ролей
        var insufficientRoles = GetInsufficientRoles();

        Console.WriteLine($"OnPrisonStatusRequest: map={msg.MapId}, player={args.SenderSession?.Name}, participatingCount={participatingCount}, canStartRound={canStartRound}, hasActiveVoting={hasActiveVoting}, insufficientRoles={string.Join(",", insufficientRoles)}");

        var hasTimer = false;
        int? remainingSeconds = null;

        // Глобальный таймер показываем только для активной карты голосования
        if (!isLaunched && HasActiveTimer("GLOBAL") && _activeVotingMapId != null)
        {
            if (msg.MapId == _activeVotingMapId && _mapStates.TryGetValue("GLOBAL", out var globalState))
            {
                hasTimer = true;
                remainingSeconds = globalState.RemainingSeconds;
            }
        }
        else if (!isLaunched && HasActiveTimer(msg.MapId))
        {
            hasTimer = true;
            if (_mapStates.TryGetValue(msg.MapId, out var mapState) && mapState.RemainingSeconds.HasValue)
                remainingSeconds = mapState.RemainingSeconds;
        }

        var updateEvent = BuildVoteUpdateEvent(msg.MapId, voteCount, hasTimer, isLaunched, remainingSeconds ?? 0, participatingCount, insufficientRoles.ToArray());
        RaiseNetworkEvent(updateEvent, args.SenderSession!);

        // Также отправляем состояние других карт этому игроку
        foreach (var otherMapId in GetPrisonMapProtoIds().Where(id => id != msg.MapId))
        {
            var otherVoteCount = GetVoteCount(otherMapId);
            var otherIsLaunched = _launchedMaps.ContainsKey(otherMapId);

            var otherHasTimer = false;
            int? otherRemainingSeconds = null;

            if (!otherIsLaunched && HasActiveTimer("GLOBAL") && _activeVotingMapId != null)
            {
                if (otherMapId == _activeVotingMapId && _mapStates.TryGetValue("GLOBAL", out var otherGlobalState))
                {
                    otherHasTimer = true;
                    otherRemainingSeconds = otherGlobalState.RemainingSeconds;
                }
            }
            else if (!otherIsLaunched && HasActiveTimer(otherMapId))
            {
                otherHasTimer = true;
                if (_mapStates.TryGetValue(otherMapId, out var mapState) && mapState.RemainingSeconds.HasValue)
                    otherRemainingSeconds = mapState.RemainingSeconds;
            }

            var otherUpdateEvent = BuildVoteUpdateEvent(otherMapId, otherVoteCount, otherHasTimer, otherIsLaunched, otherRemainingSeconds ?? 0, participatingCount, insufficientRoles.ToArray());
            RaiseNetworkEvent(otherUpdateEvent, args.SenderSession!);
        }
    }

    private void OnRolePriority(PlanetPrisonRolePriorityMessage msg, EntitySessionEventArgs args)
    {
        var player = args.SenderSession;
        if (player == null)
            return;

        Console.WriteLine($"Received role priority from {player.Name}: role={msg.RoleId}, priority={msg.Priority}");

        // Получаем или создаем состояние роли
        var currentRoleState = GetOrCreateRoleState(msg.RoleId);

        // Сохраняем приоритет игрока для роли
        var oldPriority = currentRoleState.Priorities.GetValueOrDefault(player.UserId, -1);

        // Если игрок пытается установить низкий приоритет для PlanetPrisoner, но у него есть голоса за карты,
        // автоматически устанавливаем высокий приоритет (игроки, голосующие за карты, должны быть готовы к роли заключенного)
        var finalPriority = msg.Priority;
        if (msg.RoleId == "PlanetPrisoner" && msg.Priority < 3)
        {
            // Проверяем, есть ли у игрока голоса за карты
            bool hasMapVotes = _mapStates.Any(mapState =>
                mapState.Value.Votes.GetValueOrDefault(player.UserId, 0) > 0);

            if (hasMapVotes && msg.Priority < 3)
            {
                finalPriority = 3; // Принудительно устанавливаем высокий приоритет
                Console.WriteLine($"Forced high priority for {player.Name} on PlanetPrisoner (has map votes)");
            }
        }

        currentRoleState.Priorities[player.UserId] = finalPriority;
        Console.WriteLine($"Updated priority for {player.Name} on {msg.RoleId}: {oldPriority} -> {finalPriority}");

        // Проверяем, можно ли назначить эту роль игроку
        // Пока что просто отправляем обновление статуса роли
        var isTaken = currentRoleState.AssignedPlayer != null;
        var isAssigned = currentRoleState.AssignedPlayer == player.UserId;

        var updateEvent = new PlanetPrisonRoleUpdateEvent(msg.RoleId, isTaken, isAssigned);
        RaiseNetworkEvent(updateEvent, player);

        // Обновляем состояние голосования для карт игроку, который изменил приоритет
        SendVotingStateToPlayer(player.UserId);

        // Также обновляем состояние для всех клиентов, чтобы они увидели изменения в квотах
        UpdateAllVotingStates();
    }

    /// <summary>
    /// Проверяет все запущенные карты и удаляет те, на которых нет живых игроков (все умерли или стали призраками)
    /// </summary>
    /// <summary>
    /// Обработчик события изменения состояния моба для игроков на тюремных картах
    /// Также проверяет, стал ли игрок призраком (для случаев, когда игрок становится призраком без смерти)
    /// </summary>
    private void OnPrisonPlayerMobStateChanged(Entity<PlanetPrisonSpawnedComponent> ent, ref MobStateChangedEvent args)
    {
        // Проверяем, что игрок умер или стал призраком
        bool shouldCheck = false;
        
        if (args.NewMobState == MobState.Dead)
        {
            shouldCheck = true;
            Console.WriteLine($"[AUTO-DELETE] Player died on map {ent.Comp.MapId}");
        }
        else if (HasComp<GhostComponent>(ent))
        {
            // Игрок стал призраком без смерти (например, через криокапсулу)
            shouldCheck = true;
            Console.WriteLine($"[AUTO-DELETE] Player became ghost on map {ent.Comp.MapId}");
        }

        if (!shouldCheck)
            return;

        // Проверяем, что это действительно игрок с Mind, а не NPC
        if (!TryComp<MindContainerComponent>(ent, out var mindContainer) ||
            (mindContainer.Mind == null && mindContainer.LastMindStored == null))
            return;

        // Получаем MapId из компонента
        var mapId = ent.Comp.MapId;
        if (mapId == MapId.Nullspace)
            return;

        // Проверяем и удаляем карту, если она пустая
        CheckAndRemoveMapIfEmpty(mapId);
    }

    /// <summary>
    /// Обработчик отсоединения игрока от сущности на тюремной карте.
    /// Используется для удаления карт, если все игроки вышли из тела / отключились.
    /// </summary>
    private void OnPrisonPlayerDetached(Entity<PlanetPrisonSpawnedComponent> ent, ref PlayerDetachedEvent args)
    {
        var mapId = ent.Comp.MapId;
        if (mapId == MapId.Nullspace)
            return;

        Console.WriteLine($"[AUTO-DELETE] Player detached from map {mapId} (session status={args.Player.Status})");

        CheckAndRemoveMapIfEmpty(mapId);
    }

    private void OnGetObserverSpawnPoint(GetObserverSpawnPointEvent ev)
    {
        if (ev.ExcludeMapId is not { } excludeMapId || !IsPrisonMap(excludeMapId))
            return;
        var coords = GetObserverSpawnPointExcludingMap(excludeMapId);
        if (coords.IsValid(EntityManager))
            ev.Coordinates = coords;
    }

    /// <summary>
    /// Возвращает true, если карта является тюремной (запущенной или в кэше).
    /// </summary>
    private bool IsPrisonMap(MapId mapId)
    {
        if (_launchedMaps.Values.Contains(mapId))
            return true;
        foreach (var queue in _cachedMaps.Values)
        {
            if (queue != null && queue.Contains(mapId))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Возвращает координаты точки спавна наблюдателя, исключая указанную карту.
    /// Используется перед удалением карты тюрьмы, чтобы призраки не создавались на удаляемой карте.
    /// </summary>
    private EntityCoordinates GetObserverSpawnPointExcludingMap(MapId excludeMapId)
    {
        _observerSpawnPositionsBuffer.Clear();
        var spawnPointQuery = EntityQueryEnumerator<SpawnPointComponent, TransformComponent>();
        while (spawnPointQuery.MoveNext(out var uid, out var point, out var transform))
        {
            if (point.SpawnType != SpawnPointType.Observer
               || TerminatingOrDeleted(uid)
               || transform.MapUid == null
               || TerminatingOrDeleted(transform.MapUid.Value))
            {
                continue;
            }
            if (_transform.GetMapId(uid) == excludeMapId)
                continue;
            _observerSpawnPositionsBuffer.Add(transform.Coordinates);
        }

        var metaQuery = GetEntityQuery<MetaDataComponent>();

        if (_observerSpawnPositionsBuffer.Count == 0)
        {
            var query = AllEntityQuery<MapGridComponent>();
            while (query.MoveNext(out var uid, out var grid))
            {
                if (!metaQuery.TryGetComponent(uid, out var meta) || meta.EntityPaused || TerminatingOrDeleted(uid))
                    continue;
                if (_transform.GetMapId(uid) == excludeMapId)
                    continue;
                _observerSpawnPositionsBuffer.Add(new EntityCoordinates(uid, Vector2.Zero));
            }
        }

        if (_observerSpawnPositionsBuffer.Count != 0)
        {
            var spawn = _random.Pick(_observerSpawnPositionsBuffer);
            var toMap = _transform.ToMapCoordinates(spawn);
            if (_mapManager.TryFindGridAt(toMap, out var gridUid, out _))
            {
                var gridXform = Transform(gridUid);
                return new EntityCoordinates(gridUid, Vector2.Transform(toMap.Position, _transform.GetInvWorldMatrix(gridXform)));
            }
            return spawn;
        }

        if (_map.MapExists(_gameTicker.DefaultMap) && _gameTicker.DefaultMap != excludeMapId)
        {
            var mapUid = _map.GetMapOrInvalid(_gameTicker.DefaultMap);
            if (!TerminatingOrDeleted(mapUid))
                return new EntityCoordinates(mapUid, Vector2.Zero);
        }

        foreach (var map in _map.GetAllMapIds())
        {
            if (map == excludeMapId)
                continue;
            var mapUid = _map.GetMapOrInvalid(map);
            if (!metaQuery.TryGetComponent(mapUid, out var meta)
                || meta.EntityPaused
                || TerminatingOrDeleted(mapUid))
            {
                continue;
            }
            return new EntityCoordinates(mapUid, Vector2.Zero);
        }

        return EntityCoordinates.Invalid;
    }

    /// <summary>
    /// Переносит всех игроков (сессии), привязанных к сущностям на карте тюрьмы, на главную станцию перед удалением карты.
    /// Призраков перемещает в точку спавна призраков; тела/трупы — создаёт нового призрака и переносит ум.
    /// Возвращает false, если нет валидной точки спавна призраков (тогда удаление карты вызывать не следует).
    /// </summary>
    private bool TeleportPrisonGhostsToMainStation(MapId mapId)
    {
        var spawnCoords = GetObserverSpawnPointExcludingMap(mapId);
        if (!spawnCoords.IsValid(EntityManager))
            return false;

        var sessionsOnMap = new List<ICommonSession>();
        foreach (var session in _playerManager.Sessions)
        {
            if (session.AttachedEntity is not { } uid)
                continue;
            if (Deleted(uid) || !TryComp(uid, out TransformComponent? xform))
                continue;
            if (xform.MapID != mapId)
                continue;
            sessionsOnMap.Add(session);
        }

        foreach (var session in sessionsOnMap)
        {
            var uid = session.AttachedEntity!.Value;
            if (HasComp<GhostComponent>(uid))
            {
                RemComp<PlanetPrisonSpawnedComponent>(uid);
                _transform.SetCoordinates(uid, spawnCoords);
                continue;
            }

            if (!_mind.TryGetMind(uid, out var mindId, out _))
                continue;

            var newGhost = Spawn(GameTicker.ObserverPrototypeName, spawnCoords);
            EnsureComp<MindContainerComponent>(newGhost);
            var ghostComp = Comp<GhostComponent>(newGhost);
            _ghosts.SetCanReturnToBody((newGhost, ghostComp), false);
            _mind.TransferTo(mindId, newGhost, createGhost: false);
        }

        return true;
    }

    /// <summary>
    /// Отвязывает от сущностей все сессии, привязанные к сущностям на указанной карте.
    /// Используется при принудительном удалении карты (например, рестарт раунда), когда точки спавна призраков нет.
    /// </summary>
    private void DetachSessionsOnMap(MapId mapId)
    {
        foreach (var session in _playerManager.Sessions)
        {
            if (session.AttachedEntity is not { } uid)
                continue;
            if (Deleted(uid) || !TryComp(uid, out TransformComponent? xform))
                continue;
            if (xform.MapID != mapId)
                continue;
            _playerManager.SetAttachedEntity(session, null);
        }
    }

    /// <summary>
    /// Проверяет указанную карту и удаляет её, если на ней нет живых игроков (не призраков)
    /// </summary>
    private void CheckAndRemoveMapIfEmpty(MapId mapId)
    {
        if (mapId == MapId.Nullspace || !_mapManager.MapExists(mapId))
        {
            Console.WriteLine($"[AUTO-DELETE] Map {mapId} doesn't exist, skipping check");
            return;
        }

        // Находим protoId для этой карты
        var protoId = _launchedMaps.FirstOrDefault(kvp => kvp.Value == mapId).Key;
        if (string.IsNullOrEmpty(protoId))
        {
            Console.WriteLine($"[AUTO-DELETE] Map {mapId} is not in launched maps, skipping check");
            return;
        }

        // Проверяем, есть ли живые игроки (не призраки) на этой карте
        bool hasAlivePlayers = false;

        // Ищем всех игроков с PlanetPrisonSpawnedComponent на этой карте
        var query = EntityQueryEnumerator<PlanetPrisonSpawnedComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var spawned, out var xform))
        {
            // Проверяем, что сущность находится на нужной карте
            if (xform.MapID != mapId)
                continue;

            // Проверяем, что это действительно игрок с Mind (не обычный моб / NPC)
            if (!TryComp<MindContainerComponent>(uid, out var mindContainer) ||
                (mindContainer.Mind == null && mindContainer.LastMindStored == null))
                continue;

            // Проверяем, что у сущности есть актор и сессия в игровом состоянии
            if (!TryComp<ActorComponent>(uid, out var actor) ||
                actor.PlayerSession == null ||
                actor.PlayerSession.Status != SessionStatus.InGame)
                continue;

            // Проверяем, является ли игрок призраком
            if (HasComp<GhostComponent>(uid))
                continue;

            // Проверяем, жив ли игрок
            if (!_mobState.IsAlive(uid))
                continue;

            // Нашли живого подключенного игрока (не призрака) на карте
            hasAlivePlayers = true;
            break;
        }

        if (!hasAlivePlayers)
        {
            var completionSettings = GetCompletionSettingsForMap(protoId);
            Console.WriteLine($"[COMPLETION] Map {protoId} (MapId: {mapId}) has no alive players, CompletionMode: {completionSettings.CompletionMode}");

            if (!TeleportPrisonGhostsToMainStation(mapId))
            {
                Console.WriteLine($"[COMPLETION] Cannot delete map {protoId} (MapId: {mapId}): no valid observer spawn point, will retry later");
                return;
            }

            _launchedMaps.Remove(protoId);

            if (_mapManager.MapExists(mapId))
            {
                if (completionSettings.CompletionMode == PrisonMapCompletionMode.Delete)
                {
                    DetachSessionsOnMap(mapId);
                    _mapManager.DeleteMap(mapId);
                    Console.WriteLine($"[COMPLETION] Map {protoId} (MapId: {mapId}) deleted immediately");
                }
                else
                {
                    _map.SetPaused(mapId, true);
                    _pausedOrphanMaps.Add(mapId);
                    Console.WriteLine($"[COMPLETION] Map {protoId} (MapId: {mapId}) frozen, will be deleted on round restart");
                }
            }

            OnPrisonMapEnded();
        }
        else
        {
            Console.WriteLine($"[AUTO-DELETE] Map {protoId} (MapId: {mapId}) still has alive players, keeping");
        }
    }

    /// <summary>
    /// Общая логика при завершении раунда карты тюрьмы (звук, сброс голосования, SendGlobalStateReset).
    /// Вызывается при заморозке карты и при её удалении.
    /// </summary>
    private void OnPrisonMapEnded()
    {
        _audioSystem.PlayGlobal("/Audio/Misc/notice1.ogg", Filter.Broadcast(), false, AudioParams.Default);

        foreach (var mapState in _mapStates.Values)
        {
            mapState.Timer.Cancel();
        }
        _mapStates.Clear();

        ResetMapVotingOnly();
        SendGlobalStateReset();
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        var toDelete = _pausedOrphanMaps.ToList();
        _pausedOrphanMaps.Clear();
        _isCleaningPausedMaps = true;
        try
        {
            foreach (var mapId in toDelete)
            {
                if (!_mapManager.MapExists(mapId))
                    continue;
                if (!TeleportPrisonGhostsToMainStation(mapId))
                    DetachSessionsOnMap(mapId);
                DetachSessionsOnMap(mapId);
                _mapManager.DeleteMap(mapId);
            }
        }
        finally
        {
            _isCleaningPausedMaps = false;
        }
    }

    private void OnMapRemoved(MapRemovedEvent ev)
    {
        var mapIdValue = (int)ev.MapId;
        if (mapIdValue >= 100) // Карта тюрьмы
        {
            Console.WriteLine($"Prison map (ID: {mapIdValue}) removed, checking if it's cached");

            // Проверяем, была ли эта карта в кэше (в какой-либо очереди)
            var cachedProtoId = _cachedMaps.FirstOrDefault(kvp => kvp.Value.Contains(ev.MapId)).Key;
            if (!string.IsNullOrEmpty(cachedProtoId))
            {
                Console.WriteLine($"Prison map {cachedProtoId} (ID: {mapIdValue}) was cached and removed");

                // Карта была в кэше - она уже пересоздана при активации
                // Удаляем её из кэша, если она там осталась
                if (_cachedMaps.TryGetValue(cachedProtoId, out var queue))
                {
                    // Пересоздаем очередь без удаляемой карты
                    _cachedMaps[cachedProtoId] = new Queue<MapId>(queue.Where(id => id != ev.MapId));
                    Console.WriteLine($"Removed MapId {mapIdValue} from {cachedProtoId} cache queue");
                }
            }
            else if (_launchedMaps.ContainsValue(ev.MapId))
            {
                Console.WriteLine($"Prison map (ID: {mapIdValue}) was launched and removed");

                var protoId = _launchedMaps.FirstOrDefault(kvp => kvp.Value == ev.MapId).Key;
                if (!string.IsNullOrEmpty(protoId))
                {
                    _launchedMaps.Remove(protoId);
                    Console.WriteLine($"Removed {protoId} from launched maps");
                }
            }
            else
            {
                Console.WriteLine($"Prison map (ID: {mapIdValue}) removed, was not cached or launched");
            }

            _pausedOrphanMaps.Remove(ev.MapId);

            if (!_isCleaningPausedMaps)
                OnPrisonMapEnded();
        }
    }

    private async void StartGlobalPrisonVote()
    {
        Console.WriteLine($"Starting global prison vote... (players: {GetParticipatingPlayerCount()})");

        // Вычисляем минимальное количество игроков, необходимое для запуска любой из доступных карт.
        // Берем min из prototype.GameMapPrototype.MinPlayers для каждой карты, если задано.
        var defaultMin = 2;
        var minPlayersCandidates = new List<int>();

        foreach (var mapId in GetPrisonMapProtoIds().ToList())
        {
            if (_prototypeManager.TryIndex<GameMapPrototype>(mapId, out var proto) && proto.MinPlayers > 0)
            {
                minPlayersCandidates.Add((int)proto.MinPlayers);
            }
        }

        // Если нет ни одного значения в prototype, пробуем глобальную настройку в компоненте
        if (!minPlayersCandidates.Any())
        {
            minPlayersCandidates.Add(GetMinPlayersRequired());
        }

        var minPlayersRequired = minPlayersCandidates.Any() ? minPlayersCandidates.Min() : defaultMin;

        // Дополнительная проверка - должно быть минимум игроков
        if (GetParticipatingPlayerCount() < minPlayersRequired)
        {
            Console.WriteLine($"Cannot start prison vote with {GetParticipatingPlayerCount()} players, need at least {minPlayersRequired}");
            return;
        }

        // Вычисляем суммарный приоритет для каждой карты от проголосовавших игроков
        var mapScores = new Dictionary<string, double>();
        foreach (var mapId in GetPrisonMapIdsForBroadcast())
        {
            var mapState = GetOrCreateMapState(mapId);
            if (mapState.Votes.Count > 0)
            {
                // Суммируем приоритеты всех игроков, которые голосовали за эту карту
                var totalPriority = mapState.Votes.Values.Where(p => p > 0).Sum();
                mapScores[mapId] = totalPriority;
                Console.WriteLine($"Map {mapId} total priority: {totalPriority} from {mapState.Votes.Count} votes");
            }
            else
            {
                mapScores[mapId] = 0; // Нет голосов = приоритет 0
            }
        }

        // Находим карту с наивысшим приоритетом среди тех, за которые проголосовали
        var votedMaps = mapScores.Where(kvp => kvp.Value > 0).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        if (!votedMaps.Any())
        {
            Console.WriteLine($"No maps with votes found, cancelling vote");
            Console.WriteLine($"[DEBUG] mapScores: [{string.Join(", ", mapScores.Select(kvp => $"{kvp.Key}={kvp.Value}"))}], mapIds: [{string.Join(", ", GetPrisonMapIdsForBroadcast())}]");
            ResetMapVotingOnly();
            SendGlobalStateReset();
            return;
        }

        // Выбираем карту с наивысшим приоритетом (даже если она не подходит по игрокам)
        var maxScore = votedMaps.Values.Max();
        var bestMaps = votedMaps.Where(kvp => kvp.Value == maxScore).Select(kvp => kvp.Key).ToList();
        var selectedMapId = _random.Pick(bestMaps);

        // Проверяем, подходит ли выбранная карта по количеству игроков
        var minPlayersForSelectedMap = GetMinPlayersRequiredForMap(selectedMapId);
        if (GetParticipatingPlayerCount() < minPlayersForSelectedMap)
        {
            Console.WriteLine($"Selected map {selectedMapId} needs {minPlayersForSelectedMap} players, but only {GetParticipatingPlayerCount()} available. Waiting for more players...");
            // Не отменяем голосование, ждем больше игроков
            return;
        }

        Console.WriteLine($"Selected map {selectedMapId} with priority {maxScore} (meets player requirements: {GetParticipatingPlayerCount()} >= {minPlayersForSelectedMap})");

        // Запоминаем активную карту, для которой будет отображаться глобальный таймер
        _activeVotingMapId = selectedMapId;

        // Проигрываем звук только когда карта действительно выбрана и подходит
        _audioSystem.PlayGlobal("/Audio/Machines/beep.ogg", Filter.Broadcast(), false, AudioParams.Default);

        // Запускаем таймер для выбранной карты
        var globalState = GetOrCreateMapState("GLOBAL");
        // Обновляем таймер в состоянии
        _mapStates["GLOBAL"] = globalState with { Timer = new CancellationTokenSource(), RemainingSeconds = 5 };

        // Очищаем предупреждения о недостатке ролей, так как голосование началось
        UpdateAllVotingStates();

            var startEvent = BuildVoteUpdateEvent(selectedMapId, GetParticipatingPlayerCount(), true, false, 5, GetParticipatingPlayerCount(), Array.Empty<string>());
        RaiseNetworkEvent(startEvent);

        try
        {
            // Начинаем с 5 секунд
            var globalTimer = GetOrCreateMapState("GLOBAL").Timer;

            for (int i = 5; i >= 1; i--)
            {
                if (globalTimer.IsCancellationRequested)
                    return;

                // Обновляем оставшееся время в состоянии
                var currentState = _mapStates["GLOBAL"];
                _mapStates["GLOBAL"] = currentState with { RemainingSeconds = i };

                var updateEvent = BuildVoteUpdateEvent(selectedMapId, GetParticipatingPlayerCount(), true, false, i, GetParticipatingPlayerCount(), Array.Empty<string>());
                RaiseNetworkEvent(updateEvent);

                // Обновляем состояние всех карт, чтобы другие карты получили правильное состояние (isVoting=false)
                UpdateAllVotingStates();

                // Если время вышло и игроков недостаточно, отменить голосование
                if (i == 0 && GetParticipatingPlayerCount() < minPlayersRequired)
                {
                    Console.WriteLine("Voting timeout reached, not enough players. Cancelling vote.");
                    // Отменить голосование - сбросить состояния
                    ResetMapVotingOnly();
                    SendGlobalStateReset();
                    return;
                }

                await Task.Delay(1000, globalTimer.Token);
            }

            // Финальная проверка перед запуском - все ли роли еще доступны
            if (!CanStartRound())
            {
                Console.WriteLine("Cannot launch map - roles became unavailable during countdown");
                ResetMapVotingOnly();
                SendGlobalStateReset();
                return;
            }

            // При 0 секундах сразу показываем "(запускается)" без задержки
            var minForLaunch = GetMinPlayersRequiredForMap(selectedMapId);
            var launchEvent = new PlanetPrisonVoteUpdateEvent(selectedMapId, GetParticipatingPlayerCount(), true, false, 0, GetParticipatingPlayerCount(), Array.Empty<string>(), minForLaunch);
            RaiseNetworkEvent(launchEvent);

            // Небольшая задержка чтобы клиент успел показать "(запускается)" перед загрузкой карты
            await Task.Delay(100, globalTimer.Token);

            // Запускаем выбранную карту
            var launchedMapId = await SpawnPrisonMap(selectedMapId, selectedMapId);

            // Сохраняем соответствие protoId -> MapId
            _launchedMaps[selectedMapId] = launchedMapId;

            // Назначаем роли игрокам на основе их приоритетов ПЕРЕД сбросом
            AssignPrisonRoles(selectedMapId);

            // После назначения ролей сбрасываем только голоса карт и пул игроков, но сохраняем приоритеты ролей
            ResetMapVotingOnly();

            // Отправляем клиентам обновления о сбросе всех состояний
            SendGlobalStateReset();

            // Отправляем финальные обновления клиентам
            SendFinalUpdatesAfterLaunch(selectedMapId);
        }
        catch (TaskCanceledException)
        {
            // Голосование отменено
        }
        finally
        {
            // Убираем глобальный таймер из состояния
            if (_mapStates.ContainsKey("GLOBAL"))
            {
                _mapStates.Remove("GLOBAL");
            }
            // Сбрасываем активную карту голосования
            _activeVotingMapId = null;
            var minForEnd = GetMinPlayersRequiredForMap(selectedMapId);
            var endEvent = new PlanetPrisonVoteUpdateEvent(selectedMapId, GetParticipatingPlayerCount(), false, true, 0, GetParticipatingPlayerCount(), Array.Empty<string>(), minForEnd);
            RaiseNetworkEvent(endEvent);
        }
    }


    private async Task<MapId> SpawnPrisonMap(string mapId, string protoId)
    {
        Console.WriteLine($"Spawning prison map {mapId}");

        // Сначала проверяем, есть ли карта в кэше (в очереди)
        if (_cachedMaps.TryGetValue(protoId, out var mapQueue) && mapQueue.TryDequeue(out var cachedMapId))
        {
            Console.WriteLine($"[VOTE] Using cached prison map {protoId} (MapId: {cachedMapId})");
            Console.WriteLine($"[VOTE] Cache status: {mapQueue.Count} copies remaining for {protoId}");

            // Активируем кэшированную карту
            var activationStart = System.Diagnostics.Stopwatch.StartNew();
            _map.SetPaused(cachedMapId, false);
            activationStart.Stop();

            // Для карт с планетами (PrisonMapFamily == Metus) гарантируем создание/наличие планеты при активации
            if (IsPlanetPrisonMap(protoId))
            {
                await CreatePlanetForMap(cachedMapId, protoId);
            }

            Console.WriteLine($"[VOTE] Map {protoId} activated in {activationStart.Elapsed.TotalMilliseconds:F0}ms");
            return cachedMapId;
        }

        // Если карты нет в кэше, создаем новый кэш синхронно
        Console.WriteLine($"Prison map {protoId} not found in cache, creating new cache entry");
        PreloadSingleMap(protoId);

        // Теперь пытаемся снова получить карту из кэша
        if (_cachedMaps.TryGetValue(protoId, out mapQueue) && mapQueue.TryDequeue(out cachedMapId))
        {
            Console.WriteLine($"Using newly cached prison map {protoId} with MapId {cachedMapId}");

            // Активируем только что созданную карту
            _map.SetPaused(cachedMapId, false);

            // Для карт с планетами (PrisonMapFamily == Metus) гарантируем создание/наличие планеты при активации
            if (IsPlanetPrisonMap(protoId))
            {
                await CreatePlanetForMap(cachedMapId, protoId);
            }

            Console.WriteLine($"Activated newly cached prison map {protoId} (MapId: {cachedMapId})");
            return cachedMapId;
        }

        // Если и это не помогло, загружаем без кэша (fallback)
        Console.WriteLine($"Failed to create cache for {protoId}, loading from scratch");

        var freeMapId = GetNextFreeMapIdForProto(protoId, null);
        if (freeMapId == -1)
        {
            Console.WriteLine($"Cannot find free map ID for {protoId}");
            return MapId.Nullspace;
        }

        var mapIdObj = new MapId(freeMapId);
        if (_mapManager.MapExists(mapIdObj))
        {
            Console.WriteLine($"Map ID {freeMapId} already exists, cannot spawn prison map");
            return MapId.Nullspace;
        }

        try
        {
            // Используем тот же подход что и в GameTicker для надежной загрузки
            Console.WriteLine($"Loading prison map using GameTicker.LoadGameMapWithId...");

            // Используем GameTicker для загрузки карты с конкретным ID (без инициализации)
            var opts = new DeserializationOptions { InitializeMaps = false };
            if (!_prototypeManager.TryIndex<GameMapPrototype>(protoId, out var gameMapProto))
            {
                Console.WriteLine($"Unknown GameMapPrototype prototype: {protoId}");
                return MapId.Nullspace;
            }

            var uids = _gameTicker.LoadGameMapWithId(gameMapProto, mapIdObj, opts);

            // Вручную инициализируем карту после загрузки
            if (_entManager.System<SharedMapSystem>().IsInitialized(mapIdObj))
            {
                Console.WriteLine("Map is already initialized");
            }
            else
            {
                Console.WriteLine("Manually initializing map...");
                _entManager.System<SharedMapSystem>().InitializeMap(mapIdObj);
            }

            if (uids.Count == 0)
            {
                Console.WriteLine("Failed to load prison map - no entities loaded");
                return MapId.Nullspace;
            }


            // Для карт с планетами (PrisonMapFamily == Metus) создаем планету
            if (IsPlanetPrisonMap(protoId))
            {
                await CreatePlanetForMap(mapIdObj, protoId);
            }

            Console.WriteLine($"Map {freeMapId} initialized successfully. Prison map spawning complete!");
            return mapIdObj;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to spawn prison map: {e.Message}");
            return MapId.Nullspace;
        }
    }

    /// <summary>
    /// Возвращает первый свободный MapId в диапазоне [start, start+size-1] или -1.
    /// </summary>
    private int FindFreeMapIdInRange(int start, int size)
    {
        for (var i = 0; i < size; i++)
        {
            var id = start + i;
            if (!_mapManager.MapExists(new MapId(id)))
            {
                Console.WriteLine($"Found free map ID in range: {id}");
                return id;
            }
        }
        Console.WriteLine($"No free map IDs in range {start}..{start + size - 1}");
        return -1;
    }

    /// <summary>
    /// Свободный MapId для прелоада (cacheIndex задан) или on-demand (cacheIndex == null). Учитывает prisonCacheMapIdStart, затем диапазон семейства, затем глобальный 100..499.
    /// </summary>
    private int GetNextFreeMapIdForProto(string protoId, int? cacheIndex)
    {
        if (!_prototypeManager.TryIndex<GameMapPrototype>(protoId, out var gameMapProto))
            return FindFreeMapId();

        if (cacheIndex is >= 0 && gameMapProto.PrisonCacheMapIdStart is { } start)
        {
            var candidate = start + cacheIndex.Value;
            if (!_mapManager.MapExists(new MapId(candidate)))
            {
                Console.WriteLine($"Using explicit map ID for {protoId} cache #{cacheIndex}: {candidate}");
                return candidate;
            }
            Console.WriteLine($"[CACHE] MapId {candidate} already in use for {protoId}, falling back to family/global search");
        }

        var family = gameMapProto.PrisonMapFamily;
        var component = GetPrisonComponent();
        if (!string.IsNullOrEmpty(family) && component?.CacheSettings.FamilyMapIdRanges is { } ranges &&
            ranges.TryGetValue(family, out var range))
        {
            var id = FindFreeMapIdInRange(range.Start, range.Size);
            if (id >= 0)
                return id;
        }

        return FindFreeMapId();
    }

    private int FindFreeMapId()
    {
        for (int i = 100; i < 500; i++)
        {
            if (!_mapManager.MapExists(new MapId(i)))
            {
                Console.WriteLine($"Found free map ID: {i}");
                return i;
            }
        }
        Console.WriteLine("No free map IDs found in range 100-499");
        return -1;
    }

    private async Task CreatePlanetForMap(MapId mapId, string protoId)
    {
        try
        {
            // Пытаемся получить список биомов из активной станции тюрьмы.
            // Если компонент станции не найден (например, в отдельном режиме тюрьмы),
            // используем заранее известный набор биомов по умолчанию.
            var biomeIds = new List<ProtoId<BiomeTemplatePrototype>>();

            var query = AllEntityQuery<PlanetPrisonStationComponent>();
            if (query.MoveNext(out var stationComp))
            {
                biomeIds.AddRange(stationComp.Biomes);
            }
            else
            {
                Console.WriteLine("No PlanetPrisonStationComponent found for creating planet, using default biome set");
                // Fallback-набор биомов, совпадающий с настройками BaseStationPlanetPrison.
                biomeIds.Add(new ProtoId<BiomeTemplatePrototype>("PlainGrasslands"));
                biomeIds.Add(new ProtoId<BiomeTemplatePrototype>("PlainLowDesert"));
                biomeIds.Add(new ProtoId<BiomeTemplatePrototype>("PlainSnow"));
            }

            if (biomeIds.Count == 0)
            {
                Console.WriteLine("No biome IDs available for prison planet");
                return;
            }

            // Выбираем случайный биом из доступного списка без использования Pick (чтобы избежать nullable-предупреждений).
            var biomeIndex = _random.Next(biomeIds.Count);
            var biomeId = biomeIds[biomeIndex];

            // TryIndex по контракту может вернуть null в out-параметре, поэтому используем nullable-тип и явную проверку.
            if (!_prototypeManager.TryIndex(biomeId, out BiomeTemplatePrototype? biome) || biome == null)
            {
                Console.WriteLine($"No biome prototype found for prison planet (id={biomeId})");
                return;
            }

            // Создаем планету
            var mapUid = _mapManager.GetMapEntityId(mapId);
            _biomeSystem.EnsurePlanet(mapUid, biome);

            Console.WriteLine($"Created planet with biome {biome.ID} for map {mapId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to create planet for map {mapId}: {ex.Message}");
        }
    }

    // Метод для сброса голосования (можно вызвать после удаления карты)
    public void ResetPrisonVoting(string mapId = "PlanetPrison")
    {
        if (_mapStates.TryGetValue(mapId, out var mapState))
        {
            // Очищаем голоса и отменяем таймер
            mapState.Votes.Clear();
            mapState.Timer.Cancel();
        }

        // Отправляем обновление о сбросе
        var isLaunched = _launchedMaps.ContainsKey(mapId);
        var resetEvent = new PlanetPrisonVoteUpdateEvent(mapId, 0, false, isLaunched, 0, 0, Array.Empty<string>());
        RaiseNetworkEvent(resetEvent);

        Console.WriteLine($"Prison voting reset for {mapId}");
    }
}

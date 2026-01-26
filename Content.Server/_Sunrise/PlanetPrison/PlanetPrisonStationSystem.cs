using System;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Server.Spawners.Components;
using Content.Server.Station.Systems;
using Robust.Server.Audio;
using Content.Server.Maps;
using Content.Server.Parallax;
using Content.Server.Shuttles.Systems;
using Content.Shared._Sunrise.PlanetPrison;
using Content.Shared._Sunrise.Shuttles;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared.GameTicking;
using Content.Shared.Light.Components;
using Content.Shared.Maps;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Salvage;
using Content.Shared.Shuttles.Components;
using Robust.Shared.Console;
using Robust.Server.GameObjects;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Utility;
using Robust.Shared.Configuration;
using Robust.Shared.EntitySerialization;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Sunrise.PlanetPrison;

/// <summary>
/// Состояние голосования за конкретную карту
/// </summary>
internal readonly record struct MapVotingState(
    Dictionary<NetUserId, int> Votes,
    CancellationTokenSource Timer
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

    // Состояние голосования карт
    private readonly Dictionary<string, MapVotingState> _mapStates = new();

    // Состояние ролей
    private readonly Dictionary<string, RoleState> _roleStates = new();

    // Игроки с активными приоритетами карт
    private readonly HashSet<NetUserId> _priorityPlayers = new();

    // Запущенные карты
    private readonly Dictionary<string, MapId> _launchedMaps = new();

    private const string MetusMapId = "PlanetPrison";
    private const string NoxMapId = "PlanetPrisonOld";

    /// <summary>
    /// Получить или создать состояние голосования для карты
    /// </summary>
    private MapVotingState GetOrCreateMapState(string mapId)
    {
        if (!_mapStates.TryGetValue(mapId, out var state))
        {
            state = new MapVotingState(new Dictionary<NetUserId, int>(), new CancellationTokenSource());
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
            Logger.Info($"BLOCKED: Map {msg.MapId} already launched, ignoring vote from {player.Name}");
            return;
        }

        // Блокируем голосование, если глобальное голосование уже запущено
        if (HasActiveTimer("GLOBAL"))
        {
            Logger.Info($"BLOCKED: Global vote in progress, ignoring vote from {player.Name}");
            return;
        }

        Logger.Info($"Received prison vote from {player.Name}: map={msg.MapId}, priority={msg.Priority}");

        // Игрок может голосовать за несколько карт одновременно, не удаляем старые голоса

        // Получаем или создаем состояние карты
        var mapState = GetOrCreateMapState(msg.MapId);
        var oldPriority = mapState.Votes.GetValueOrDefault(player.UserId, 0);
        mapState.Votes[player.UserId] = msg.Priority;

        // Проверяем общее количество активных приоритетов игрока по всем картам
        int totalActivePriorities = _mapStates.Values.Sum(state => state.Votes.ContainsKey(player.UserId) && state.Votes[player.UserId] > 0 ? 1 : 0);

        // Обновляем список игроков с приоритетами на основе общего количества активных приоритетов
        if (totalActivePriorities > 0 && !_priorityPlayers.Contains(player.UserId))
        {
            _priorityPlayers.Add(player.UserId);
            Logger.Info($"Added player {player.UserId} to priority list (total active priorities: {totalActivePriorities})");
        }
        else if (totalActivePriorities == 0 && _priorityPlayers.Contains(player.UserId))
        {
            _priorityPlayers.Remove(player.UserId);
            Logger.Info($"Removed player {player.UserId} from priority list (no active priorities)");
        }

        var playerCount = _priorityPlayers.Count;
        Logger.Info($"Priority players list: {string.Join(", ", _priorityPlayers)}");
        Logger.Info($"Player {player.Name} set priority {msg.Priority} for {msg.MapId}. Total priority players: {playerCount}");

        // Получаем минимальное количество игроков из компонента
        var planetPrisonQuery = EntityQueryEnumerator<PlanetPrisonSharedComponent>();
        var minPlayersRequired = 2; // Значение по умолчанию

        if (planetPrisonQuery.MoveNext(out var planetPrisonComp))
        {
            minPlayersRequired = planetPrisonComp.MinPlayersRequired;
        }

        // Отправляем обновление всем клиентам для всех карт (даже если игроков недостаточно)
        foreach (var mapId in new[] { MetusMapId, NoxMapId })
        {
            var voteCount = GetVoteCount(mapId);
            var hasTimer = HasActiveTimer(mapId) || HasActiveTimer("GLOBAL");
            var isLaunched = _launchedMaps.ContainsKey(mapId);
            var updateEvent = new PlanetPrisonVoteUpdateEvent(mapId, voteCount, hasTimer, remainingSeconds: null, _priorityPlayers.Count, minPlayersRequired, isLaunched);
            RaiseNetworkEvent(updateEvent);
        }

        // Проверяем минимальное количество игроков для запуска голосования
        if (playerCount < minPlayersRequired)
        {
            Logger.Info($"BLOCKED: Not enough players for vote: {playerCount}/{minPlayersRequired} required");
            return; // Ранний выход, если игроков недостаточно
        }

        // Если набралось 2+ игрока с приоритетами, начинаем глобальное голосование
        Logger.Info($"Checking vote start: playerCount={playerCount}, hasGlobalTimer={HasActiveTimer("GLOBAL")}, priorityPlayers={string.Join(",", _priorityPlayers)}");
        if (!HasActiveTimer("GLOBAL"))
        {
            Logger.Info("STARTING global prison vote due to 2+ players");
            StartGlobalPrisonVote();
        }
        else
        {
            Logger.Info("Vote already in progress");
        }
    }

    private void OnRoundStarted(RoundStartedEvent ev)
    {
        // Сбрасываем голосования карт и запущенные карты при старте нового раунда
        _launchedMaps.Clear();

        foreach (var mapState in _mapStates.Values)
        {
            mapState.Timer.Cancel();
        }
        _mapStates.Clear();

        // Сбрасываем только голоса карт и пул игроков, сохраняем приоритеты ролей между раундами
        ResetMapVotingOnly();

        Logger.Info("Prison voting reset on round start, role priorities preserved");
    }

    private int GetMinPlayersRequiredForMap(string mapId)
    {
        // Настройки минимального количества игроков для каждой карты
        return mapId switch
        {
            "PlanetPrison" => 2,    // Metus
            "PlanetPrisonOld" => 3, // Nox
            _ => 2                  // Значение по умолчанию
        };
    }

    private void AssignPrisonRoles()
    {
        Logger.Info("Assigning prison roles to players who voted for the map based on their role priorities");

        // Получаем игроков, которые голосовали за карту (участвовали в голосовании)
        var votingPlayers = new HashSet<NetUserId>();
        foreach (var mapState in _mapStates.Values)
        {
            foreach (var playerId in mapState.Votes.Keys)
            {
                if (mapState.Votes[playerId] > 0) // Приоритет > Never
                    votingPlayers.Add(playerId);
            }
        }

        Logger.Info($"Found {votingPlayers.Count} players who voted for the map: {string.Join(", ", votingPlayers)}");

        if (votingPlayers.Count == 0)
        {
            Logger.Info("No players voted for the map, skipping role assignment");
            return;
        }

        // Определяем доступные роли в порядке приоритета (от высшего к низшему)
        var availableRoles = new[] { "HeadOfPrison", "PrisonInspector", "PrisonWorker", "PrisonEngineer", "PrisonScientist", "PrisonDoctor", "PrisonChef", "PrisonTrainee", "PlanetPrisoner" };

        // Разделяем игроков на две группы:
        // 1. Игроки, которые установили приоритеты ролей (хотя бы один приоритет > Never)
        // 2. Игроки, которые оставили все приоритеты "Никогда"
        var playersWithRolePriorities = new HashSet<NetUserId>();
        var playersWithoutRolePriorities = new HashSet<NetUserId>();

        foreach (var playerId in votingPlayers)
        {
            bool hasAnyRolePriority = false;
            foreach (var roleId in availableRoles)
            {
                var roleState = GetOrCreateRoleState(roleId);
                if (roleState.Priorities.GetValueOrDefault(playerId, 0) > 0) // Приоритет > Never
                {
                    hasAnyRolePriority = true;
                    break;
                }
            }

            if (hasAnyRolePriority)
            {
                playersWithRolePriorities.Add(playerId);
            }
            else
            {
                playersWithoutRolePriorities.Add(playerId);
            }
        }

        Logger.Info($"Players with role priorities: {string.Join(", ", playersWithRolePriorities)}");
        Logger.Info($"Players without role priorities: {string.Join(", ", playersWithoutRolePriorities)}");

        // Создаем список игроков с их приоритетами по ролям (только для игроков с приоритетами)
        var playerRolePreferences = new Dictionary<NetUserId, Dictionary<string, int>>();
        foreach (var playerId in playersWithRolePriorities)
        {
            playerRolePreferences[playerId] = new Dictionary<string, int>();
            foreach (var roleId in availableRoles)
            {
                var roleState = GetOrCreateRoleState(roleId);
                var priority = roleState.Priorities.GetValueOrDefault(playerId, 0); // 0 = Never
                playerRolePreferences[playerId][roleId] = priority;
            }
        }

        // Распределяем роли по приоритетам (жадный алгоритм) - только для игроков с приоритетами
        var assignedPlayers = new HashSet<NetUserId>();
        var assignedRoles = new HashSet<string>();

        // Для каждого уровня приоритета (от высшего к низшему)
        for (int priorityLevel = 3; priorityLevel >= 1; priorityLevel--) // High=3, Medium=2, Low=1
        {
            foreach (var roleId in availableRoles)
            {
                if (assignedRoles.Contains(roleId))
                    continue; // Роль уже назначена

                // Находим игроков, которые хотят эту роль с текущим приоритетом
                var candidates = playersWithRolePriorities
                    .Where(p => !assignedPlayers.Contains(p) &&
                               playerRolePreferences[p][roleId] == priorityLevel)
                    .ToList();

                if (candidates.Any())
                {
                    // Назначаем роль первому кандидату (можно рандомизировать)
                    var selectedPlayer = candidates.First();
                    var roleState = GetOrCreateRoleState(roleId);

                    // Назначаем роль игроку
                    _roleStates[roleId] = roleState with { AssignedPlayer = selectedPlayer };
                    assignedPlayers.Add(selectedPlayer);
                    assignedRoles.Add(roleId);

                    Logger.Info($"Assigned role {roleId} to player {selectedPlayer} (priority level {priorityLevel})");

                    // Отправляем обновление игроку
                    var updateEvent = new PlanetPrisonRoleUpdateEvent(roleId, false, true);
                    RaiseNetworkEvent(updateEvent);

                    // Спавним игрока с ролью
                    SpawnPlayerWithRole(selectedPlayer, roleId);
                }
            }
        }

        // Теперь распределяем оставшиеся роли среди игроков без приоритетов
        var remainingRoles = availableRoles.Where(r => !assignedRoles.Contains(r)).ToList();
        var playersNeedingRoles = playersWithoutRolePriorities.Where(p => !assignedPlayers.Contains(p)).ToList();

        Logger.Info($"Remaining roles: {string.Join(", ", remainingRoles)}");
        Logger.Info($"Players without priorities needing roles: {string.Join(", ", playersNeedingRoles)}");

        // Распределяем оставшиеся роли игрокам без приоритетов
        var roleIndex = 0;
        foreach (var playerId in playersNeedingRoles)
        {
            if (roleIndex >= remainingRoles.Count)
                break; // Все роли распределены

            var roleId = remainingRoles[roleIndex++];
            var roleState = GetOrCreateRoleState(roleId);

            // Назначаем роль игроку
            _roleStates[roleId] = roleState with { AssignedPlayer = playerId };
            assignedPlayers.Add(playerId);

            Logger.Info($"Assigned remaining role {roleId} to player {playerId} (no role priorities set)");

            // Отправляем обновление игроку
            var updateEvent = new PlanetPrisonRoleUpdateEvent(roleId, false, true);
            RaiseNetworkEvent(updateEvent);

            // Спавним игрока с ролью
            SpawnPlayerWithRole(playerId, roleId);
        }

        // Распределяем оставшиеся роли между игроками без назначений
        var remainingPlayers = votingPlayers.Where(p => !assignedPlayers.Contains(p)).ToList();
        var availableFallbackRoles = new[] { "PlanetPrisoner", "PrisonTrainee" }.Where(r => !assignedRoles.Contains(r)).ToList();

        Logger.Info($"Remaining players without roles: {string.Join(", ", remainingPlayers)}");
        Logger.Info($"Available fallback roles: {string.Join(", ", availableFallbackRoles)}");

        foreach (var playerId in remainingPlayers)
        {
            if (!availableFallbackRoles.Any())
                break; // Нет доступных ролей

            string selectedRole;
            if (availableFallbackRoles.Count == 1)
            {
                // Только одна роль доступна
                selectedRole = availableFallbackRoles[0];
            }
            else
            {
                // Две роли доступны - выбираем по приоритетам
                var planetPrisonerPriority = GetOrCreateRoleState("PlanetPrisoner").Priorities.GetValueOrDefault(playerId, 0);
                var prisonTraineePriority = GetOrCreateRoleState("PrisonTrainee").Priorities.GetValueOrDefault(playerId, 0);

                if (planetPrisonerPriority > prisonTraineePriority)
                {
                    selectedRole = "PlanetPrisoner";
                }
                else if (prisonTraineePriority > planetPrisonerPriority)
                {
                    selectedRole = "PrisonTrainee";
                }
                else
                {
                    // Приоритеты равны или оба 0 - выбираем рандомно
                    selectedRole = _random.Pick(availableFallbackRoles);
                    Logger.Info($"Player {playerId} has equal priorities for fallback roles, randomly selected {selectedRole}");
                }
            }

            // Назначаем выбранную роль
            var roleState = GetOrCreateRoleState(selectedRole);
            _roleStates[selectedRole] = roleState with { AssignedPlayer = playerId };
            assignedPlayers.Add(playerId);
            assignedRoles.Add(selectedRole);

            Logger.Info($"Assigned fallback role {selectedRole} to player {playerId}");

            var updateEvent = new PlanetPrisonRoleUpdateEvent(selectedRole, false, true);
            RaiseNetworkEvent(updateEvent);

            SpawnPlayerWithRole(playerId, selectedRole);

            // Убираем роль из списка доступных
            availableFallbackRoles.Remove(selectedRole);
        }

        Logger.Info($"Role assignment complete. Assigned {assignedPlayers.Count} players to roles from {votingPlayers.Count} voters");
    }

    private void SpawnPlayerWithRole(NetUserId playerId, string roleId)
    {
        // Получаем сессию игрока
        if (!_player.TryGetSessionById(playerId, out var session))
        {
            Logger.Error($"Cannot find session for player {playerId} to assign role {roleId}");
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
            Logger.Error($"Unknown role ID: {roleId}");
            return;
        }

        Logger.Info($"Spawning player {session.Name} with role {roleId} ({jobProtoId})");

        try
        {
            // Находим запущенную карту тюрьмы (любую, так как роли глобальны для тюрьмы)
            var prisonMapId = _launchedMaps.Values.FirstOrDefault();
            if (prisonMapId == MapId.Nullspace)
            {
                Logger.Error($"No prison map is currently launched for role assignment");
                return;
            }

            // Получаем станцию по MapId
            var station = _stationSystem.GetStationInMap(prisonMapId);
            if (station == null)
            {
                Logger.Error($"Cannot find station for map {prisonMapId}");
                return;
            }

            // Ищем координаты спавна для роли
            var coordinates = GetJobSpawnCoordinates(station.Value, jobProtoId);
            if (!coordinates.HasValue)
            {
                Logger.Error($"Cannot find spawn coordinates for job {jobProtoId} on station {station.Value}");
                return;
            }

            // Получаем профиль игрока для спавна
            var profile = HumanoidCharacterProfile.DefaultWithSpecies();

            // Создаем персонажа без job (чтобы избежать автоматического применения starting gear)
            var character = _stationSpawning.SpawnPlayerMob(coordinates.Value, null, profile, station.Value);

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

            Logger.Info($"Successfully spawned player {session.Name} as {roleId} on prison map");
        }
        catch (Exception e)
        {
            Logger.Error($"Error spawning player {session.Name} with role {roleId}: {e.Message}");
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

        Logger.Info("All map and role priorities reset to Never, assignments cleared, and player list reset");
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

        Logger.Info("Map voting reset but role priorities preserved");
    }

    private void SendFinalUpdatesAfterLaunch(string launchedMapId)
    {
        // Отправляем обновления для всех карт
        foreach (var mapId in new[] { MetusMapId, NoxMapId })
        {
            var voteCount = 0; // Все голоса сброшены
            // Для запущенной карты отправляем RemainingSeconds = 0 чтобы клиент показал "(запускается)" и затем "(запущен)"
            var remainingSeconds = (mapId == launchedMapId) ? 0 : (int?)null;
            var isLaunched = _launchedMaps.ContainsKey(mapId);
            var updateEvent = new PlanetPrisonVoteUpdateEvent(mapId, voteCount, false, remainingSeconds, 0, 2, isLaunched);
            RaiseNetworkEvent(updateEvent);
        }

        Logger.Info($"Final updates sent after launching {launchedMapId}");
    }

    private void SendGlobalStateReset()
    {
        Logger.Info("Sending global state reset to all clients");

        // Получаем минимальное количество игроков из компонента
        var planetPrisonQuery = EntityQueryEnumerator<PlanetPrisonSharedComponent>();
        var minPlayersRequired = 2; // Значение по умолчанию

        if (planetPrisonQuery.MoveNext(out var planetPrisonComp))
        {
            minPlayersRequired = planetPrisonComp.MinPlayersRequired;
        }

        // Отправляем специальное сообщение о глобальном сбросе для каждой карты
        foreach (var mapId in new[] { MetusMapId, NoxMapId })
        {
            // Используем специальную комбинацию параметров для обозначения глобального сброса:
            // TotalPriorityPlayers = -1 (специальный флаг)
            var isLaunched = _launchedMaps.ContainsKey(mapId);
            var updateEvent = new PlanetPrisonVoteUpdateEvent(mapId, 0, false, null, -1, minPlayersRequired, isLaunched);
            RaiseNetworkEvent(updateEvent);
            Logger.Info($"Sent reset for {mapId}");
        }

        Logger.Info("Global state reset notifications sent to all clients");
    }

    private void OnPrisonStatusRequest(PlanetPrisonStatusRequestMessage msg, EntitySessionEventArgs args)
    {
        // Получаем минимальное количество игроков из компонента
        var planetPrisonQuery = EntityQueryEnumerator<PlanetPrisonSharedComponent>();
        var minPlayersRequired = 2; // Значение по умолчанию

        if (planetPrisonQuery.MoveNext(out var planetPrisonComp))
        {
            minPlayersRequired = planetPrisonComp.MinPlayersRequired;
        }

        // Отправляем текущее состояние голосования клиенту
        var voteCount = GetVoteCount(msg.MapId);
        var isVoting = HasActiveTimer(msg.MapId) && _launchedMaps.ContainsKey(msg.MapId);

        // Проверяем существует ли запущенная карта
        bool mapExists = false;
        if (voteCount > 0)
        {
            // Если есть голоса за карту, проверяем была ли она запущена
            // Для упрощения - если есть таймер голосования, значит карта в процессе запуска
            // Если голосов достаточно и нет таймера, значит карта запущена
            if (voteCount >= minPlayersRequired && !isVoting)
            {
                mapExists = true; // Карта должна быть запущена
            }
        }

        var isLaunched = _launchedMaps.ContainsKey(msg.MapId);
        var updateEvent = new PlanetPrisonVoteUpdateEvent(msg.MapId, voteCount, isVoting, null, _priorityPlayers.Count, minPlayersRequired, isLaunched);
        RaiseNetworkEvent(updateEvent, args.SenderSession);
    }

    private void OnRolePriority(PlanetPrisonRolePriorityMessage msg, EntitySessionEventArgs args)
    {
        var player = args.SenderSession;
        if (player == null)
            return;

        Logger.Info($"Received role priority from {player.Name}: role={msg.RoleId}, priority={msg.Priority}");

        // Получаем или создаем состояние роли
        var currentRoleState = GetOrCreateRoleState(msg.RoleId);

        // Сохраняем приоритет игрока для роли
        currentRoleState.Priorities[player.UserId] = msg.Priority;

        // Проверяем, можно ли назначить эту роль игроку
        // Пока что просто отправляем обновление статуса роли
        var isTaken = currentRoleState.AssignedPlayer != null;
        var isAssigned = currentRoleState.AssignedPlayer == player.UserId;

        var updateEvent = new PlanetPrisonRoleUpdateEvent(msg.RoleId, isTaken, isAssigned);
        RaiseNetworkEvent(updateEvent, player);
    }

    private void OnMapRemoved(MapRemovedEvent ev)
    {
        var mapIdValue = (int)ev.MapId;
        if (mapIdValue >= 100) // Карта тюрьмы
        {
            Logger.Info($"Prison map (ID: {mapIdValue}) removed, resetting voting states");

            // Проигрываем звук при удалении карты тюрьмы
            _audioSystem.PlayGlobal("/Audio/Misc/notice1.ogg", Filter.Broadcast(), false, AudioParams.Default);

        // Сброс состояния голосования карт при удалении карты тюрьмы
        foreach (var mapState in _mapStates.Values)
        {
            mapState.Timer.Cancel();
        }
        _mapStates.Clear();

        // Отменяем глобальный таймер, если он активен
        if (HasActiveTimer("GLOBAL"))
        {
            // Находим и отменяем глобальный таймер
            // Поскольку глобальный таймер хранится в _mapStates, но мы уже очистили _mapStates,
            // просто сбрасываем состояние
        }

        _launchedMaps.Clear();

            // Сбрасываем только голоса карт, но сохраняем приоритеты ролей
            ResetMapVotingOnly();

            // Отправляем клиентам обновления о сбросе состояний
            SendGlobalStateReset();
        }
    }

    private async void StartGlobalPrisonVote()
    {
        Logger.Info($"Starting global prison vote... (players: {_priorityPlayers.Count})");

        // Получаем минимальное количество игроков из компонента
        var planetPrisonQuery = EntityQueryEnumerator<PlanetPrisonSharedComponent>();
        var minPlayersRequired = 2; // Значение по умолчанию

        if (planetPrisonQuery.MoveNext(out var planetPrisonComp))
        {
            minPlayersRequired = planetPrisonComp.MinPlayersRequired;
        }

        // Дополнительная проверка - должно быть минимум игроков
        if (_priorityPlayers.Count < minPlayersRequired)
        {
            Logger.Error($"Cannot start prison vote with {_priorityPlayers.Count} players, need at least {minPlayersRequired}");
            return;
        }

        // Вычисляем суммарный приоритет для каждой карты от проголосовавших игроков
        var mapScores = new Dictionary<string, double>();
        foreach (var mapId in new[] { MetusMapId, NoxMapId })
        {
            var mapState = GetOrCreateMapState(mapId);
            if (mapState.Votes.Count > 0)
            {
                // Суммируем приоритеты всех игроков, которые голосовали за эту карту
                var totalPriority = mapState.Votes.Values.Where(p => p > 0).Sum();
                mapScores[mapId] = totalPriority;
                Logger.Info($"Map {mapId} total priority: {totalPriority} from {mapState.Votes.Count} votes");
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
            Logger.Error($"No maps with votes found, cancelling vote");
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
        if (_priorityPlayers.Count < minPlayersForSelectedMap)
        {
            Logger.Info($"Selected map {selectedMapId} needs {minPlayersForSelectedMap} players, but only {_priorityPlayers.Count} available. Waiting for more players...");
            // Не отменяем голосование, ждем больше игроков
            return;
        }

        Logger.Info($"Selected map {selectedMapId} with priority {maxScore} (meets player requirements: {_priorityPlayers.Count} >= {minPlayersForSelectedMap})");

        // Проигрываем звук только когда карта действительно выбрана и подходит
        _audioSystem.PlayGlobal("/Audio/Machines/beep.ogg", Filter.Broadcast(), false, AudioParams.Default);

        // Запускаем таймер для выбранной карты
        var globalState = GetOrCreateMapState("GLOBAL");
        // Обновляем таймер в состоянии
        _mapStates["GLOBAL"] = globalState with { Timer = new CancellationTokenSource() };
        var startEvent = new PlanetPrisonVoteUpdateEvent(selectedMapId, _priorityPlayers.Count, true, 5, _priorityPlayers.Count, minPlayersRequired, false);
        RaiseNetworkEvent(startEvent);

        try
        {
            // Начинаем с 5 секунд
            var globalTimer = GetOrCreateMapState("GLOBAL").Timer;

            for (int i = 5; i >= 1; i--)
            {
                if (globalTimer.IsCancellationRequested)
                    return;

                var updateEvent = new PlanetPrisonVoteUpdateEvent(selectedMapId, _priorityPlayers.Count, true, i, _priorityPlayers.Count, minPlayersRequired, false);
                RaiseNetworkEvent(updateEvent);

                // Если время вышло и игроков недостаточно, отменить голосование
                if (i == 0 && _priorityPlayers.Count < minPlayersRequired)
                {
                    Logger.Info("Voting timeout reached, not enough players. Cancelling vote.");
                    // Отменить голосование - сбросить состояния
                    ResetMapVotingOnly();
                    SendGlobalStateReset();
                    return;
                }

                await Task.Delay(1000, globalTimer.Token);
            }

            // При 0 секундах сразу показываем "(запускается)" без задержки
            var launchEvent = new PlanetPrisonVoteUpdateEvent(selectedMapId, _priorityPlayers.Count, true, 0, _priorityPlayers.Count, minPlayersRequired, false);
            RaiseNetworkEvent(launchEvent);

            // Небольшая задержка чтобы клиент успел показать "(запускается)" перед загрузкой карты
            await Task.Delay(100, globalTimer.Token);

            // Запускаем выбранную карту
            var launchedMapId = await SpawnPrisonMap(selectedMapId, selectedMapId);

            // Сохраняем соответствие protoId -> MapId
            _launchedMaps[selectedMapId] = launchedMapId;

            // Назначаем роли игрокам на основе их приоритетов ПЕРЕД сбросом
            AssignPrisonRoles();

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
            var endEvent = new PlanetPrisonVoteUpdateEvent(selectedMapId, _priorityPlayers.Count, false, null, _priorityPlayers.Count, minPlayersRequired, true);
            RaiseNetworkEvent(endEvent);
        }
    }


    private async Task<MapId> SpawnPrisonMap(string mapId, string protoId)
    {
        Logger.Info($"Spawning prison map {mapId}");

        // Находим свободный MapId
        var freeMapId = FindFreeMapId();
        var mapIdObj = new MapId(freeMapId);

        // Проверяем что ID свободен
        if (_mapManager.MapExists(mapIdObj))
        {
            Logger.Error($"Map ID {freeMapId} already exists, cannot spawn prison map");
            return MapId.Nullspace;
        }

        try
        {
            // Используем тот же подход что и в GameTicker для надежной загрузки
            Logger.Info($"Loading prison map using GameTicker.LoadGameMapWithId...");

            // Используем GameTicker для загрузки карты с конкретным ID (без инициализации)
            var opts = new DeserializationOptions { InitializeMaps = false };
            if (!_prototypeManager.TryIndex<GameMapPrototype>(protoId, out var gameMapProto))
            {
                Logger.Error($"Unknown GameMapPrototype prototype: {protoId}");
                return MapId.Nullspace;
            }

            var uids = _gameTicker.LoadGameMapWithId(gameMapProto, mapIdObj, opts);

            // Вручную инициализируем карту после загрузки
            if (_entManager.System<SharedMapSystem>().IsInitialized(mapIdObj))
            {
                Logger.Info("Map is already initialized");
            }
            else
            {
                Logger.Info("Manually initializing map...");
                _entManager.System<SharedMapSystem>().InitializeMap(mapIdObj);
            }

            if (uids.Count == 0)
            {
                Logger.Error("Failed to load prison map - no entities loaded");
                return MapId.Nullspace;
            }

            Logger.Info($"Prison map loaded successfully with map ID {freeMapId}, entities: {uids.Count}");

            // Для карт с планетами создаем планету
            if (protoId == MetusMapId) // Metus - планета
            {
                await CreatePlanetForMap(mapIdObj, protoId);
            }

            Logger.Info($"Map {freeMapId} initialized successfully. Prison map spawning complete!");
            return mapIdObj;
        }
        catch (Exception e)
        {
            Logger.Error($"Failed to spawn prison map: {e.Message}");
            return MapId.Nullspace;
        }
    }

    private int FindFreeMapId()
    {
        for (int i = 100; i < 200; i++)
        {
            if (!_mapManager.MapExists(new MapId(i)))
            {
                Logger.Info($"Found free map ID: {i}");
                return i;
            }
        }
        Logger.Error("No free map IDs found in range 100-199");
        return 111; // Fallback
    }

    private async Task CreatePlanetForMap(MapId mapId, string protoId)
    {
        try
        {
            // Получаем компонент станции для биомов
            var query = AllEntityQuery<PlanetPrisonStationComponent>();
            if (!query.MoveNext(out var stationComp))
            {
                Logger.Warning("No PlanetPrisonStationComponent found for creating planet");
                return;
            }

            // Выбираем случайный биом
            if (!_prototypeManager.TryIndex(_random.Pick(stationComp.Biomes), out var biome))
            {
                Logger.Warning("No biome found for prison planet");
                return;
            }

            // Создаем планету
            var mapUid = _mapManager.GetMapEntityId(mapId);
            _biomeSystem.EnsurePlanet(mapUid, biome);

            Logger.Info($"Created planet with biome {biome.ID} for map {mapId}");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to create planet for map {mapId}: {ex.Message}");
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
        var resetEvent = new PlanetPrisonVoteUpdateEvent(mapId, 0, false, null, 0, 2, isLaunched);
        RaiseNetworkEvent(resetEvent);

        Logger.Info($"Prison voting reset for {mapId}");
    }
}

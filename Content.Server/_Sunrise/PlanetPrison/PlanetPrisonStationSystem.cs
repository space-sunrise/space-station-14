using System;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
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
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly ShuttleSystem _shuttle = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IConsoleHost _console = default!;
    [Dependency] private readonly AudioSystem _audioSystem = default!;

    private readonly Dictionary<string, Dictionary<NetUserId, int>> _prisonVotes = new();
    private readonly Dictionary<string, CancellationTokenSource> _voteTimers = new();
    private readonly HashSet<NetUserId> _priorityPlayers = new(); // Игроки, поставившие приоритеты
    private readonly Dictionary<string, MapId> _launchedMaps = new(); // Соответствие protoId -> MapId запущенных карт
    private const string MetusMapId = "PlanetPrison";
    private const string NoxMapId = "PlanetPrisonOld";

    public override void Initialize()
    {
        SubscribeLocalEvent<PlanetPrisonStationComponent, ComponentInit>(OnPlanetPrisonStationInit);
        SubscribeLocalEvent<PlanetPrisonStationComponent, ComponentShutdown>(OnPrisonShutdown);
        SubscribeLocalEvent<MapRemovedEvent>(OnMapRemoved);

        SubscribeNetworkEvent<PlanetPrisonVoteMessage>(OnPrisonVote);
        SubscribeNetworkEvent<PlanetPrisonStatusRequestMessage>(OnPrisonStatusRequest);

        // Сбрасываем состояние при старте нового раунда
        SubscribeLocalEvent<RoundStartedEvent>(OnRoundStarted);

        Log.Level = LogLevel.Info;
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

        if (!_protoManager.TryIndex(_random.Pick(component.Biomes), out var biome))
        {
            Log.Warning("No Prison map found, skipping setup.");
            return;
        }

        if (!_protoManager.TryIndex(station, out var gameMap))
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

        Logger.Info($"Received prison vote from {player.Name}: map={msg.MapId}, priority={msg.Priority}");

        // Инициализируем голоса для карты, если еще не инициализированы
        if (!_prisonVotes.ContainsKey(msg.MapId))
        {
            _prisonVotes[msg.MapId] = new Dictionary<NetUserId, int>();
        }

        var mapVotes = _prisonVotes[msg.MapId];
        var oldPriority = mapVotes.GetValueOrDefault(player.UserId, 0);
        mapVotes[player.UserId] = msg.Priority;

        // Проверяем общее количество активных приоритетов игрока по всем картам
        int totalActivePriorities = 0;
        foreach (var votes in _prisonVotes.Values)
        {
            if (votes.ContainsKey(player.UserId) && votes[player.UserId] > 0)
            {
                totalActivePriorities++;
            }
        }

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

        // Отправляем обновление всем клиентам для всех карт
        foreach (var mapId in new[] { MetusMapId, NoxMapId })
        {
            var voteCount = _prisonVotes.ContainsKey(mapId) ? _prisonVotes[mapId].Count(kvp => kvp.Value > 0) : 0;
            var updateEvent = new PlanetPrisonVoteUpdateEvent(mapId, voteCount, _voteTimers.ContainsKey(mapId) || _voteTimers.ContainsKey("GLOBAL"), remainingSeconds: null, _priorityPlayers.Count);
            RaiseNetworkEvent(updateEvent); // Отправляем всем клиентам, а не только отправителю
        }

        // Жесткая проверка: нужно минимум 2 игрока для запуска голосования
        if (playerCount < 2)
        {
            Logger.Info($"BLOCKED: Not enough players for vote: {playerCount}/2 required");
            return; // Ранний выход, если игроков недостаточно
        }

        // Если набралось 2+ игрока с приоритетами, начинаем глобальное голосование
        Logger.Info($"Checking vote start: playerCount={playerCount}, hasGlobalTimer={_voteTimers.ContainsKey("GLOBAL")}, priorityPlayers={string.Join(",", _priorityPlayers)}");
        if (!_voteTimers.ContainsKey("GLOBAL"))
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
        // Сбрасываем все голосования при старте нового раунда
        _launchedMaps.Clear();
        ResetAllPrioritiesAndPlayers();
        foreach (var timer in _voteTimers.Values)
        {
            timer.Cancel();
        }
        _voteTimers.Clear();

        Logger.Info("Prison voting state reset on round start");
    }

    private void ResetAllPrioritiesAndPlayers()
    {
        // Сбрасываем все голоса до "Никогда" (0)
        foreach (var mapVotes in _prisonVotes.Values)
        {
            foreach (var userId in mapVotes.Keys.ToList())
            {
                mapVotes[userId] = 0; // Никогда
            }
        }

        // Очищаем пул игроков
        _priorityPlayers.Clear();

        Logger.Info("All priorities reset to Never and player pool cleared");
    }

    private void SendFinalUpdatesAfterLaunch(string launchedMapId)
    {
        // Отправляем обновления для всех карт
        foreach (var mapId in new[] { MetusMapId, NoxMapId })
        {
            var voteCount = 0; // Все голоса сброшены
            // Для запущенной карты отправляем RemainingSeconds = 0 чтобы клиент показал "(запускается)" и затем "(запущен)"
            var remainingSeconds = (mapId == launchedMapId) ? 0 : (int?)null;
            var updateEvent = new PlanetPrisonVoteUpdateEvent(mapId, voteCount, false, remainingSeconds, 0);
            RaiseNetworkEvent(updateEvent);
        }

        Logger.Info($"Final updates sent after launching {launchedMapId}");
    }

    private void OnPrisonStatusRequest(PlanetPrisonStatusRequestMessage msg, EntitySessionEventArgs args)
    {
        // Отправляем текущее состояние голосования клиенту
        var voteCount = _prisonVotes.ContainsKey(msg.MapId)
            ? _prisonVotes[msg.MapId].Count(kvp => kvp.Value > 0)
            : 0;

        var isVoting = _voteTimers.ContainsKey(msg.MapId);

        // Проверяем существует ли запущенная карта
        bool mapExists = false;
        if (_prisonVotes.ContainsKey(msg.MapId))
        {
            // Если есть голоса за карту, проверяем была ли она запущена
            // Для упрощения - если есть таймер голосования, значит карта в процессе запуска
            // Если голосов достаточно и нет таймера, значит карта запущена
            int requiredVotes = 2; // Все карты требуют 2 голоса
            if (voteCount >= requiredVotes && !isVoting)
            {
                mapExists = true; // Карта должна быть запущена
            }
        }

        var updateEvent = new PlanetPrisonVoteUpdateEvent(msg.MapId, voteCount, isVoting, null, _priorityPlayers.Count);
        RaiseNetworkEvent(updateEvent, args.SenderSession);
    }

    private void OnMapRemoved(MapRemovedEvent ev)
    {
        var mapIdValue = (int)ev.MapId;
        if (mapIdValue >= 100) // Карта тюрьмы
        {
            Logger.Info($"Prison map (ID: {mapIdValue}) removed, resetting ALL voting states");

            // Полный сброс состояния голосования при удалении любой карты тюрьмы
            _prisonVotes.Clear();
            _priorityPlayers.Clear();

            foreach (var timer in _voteTimers.Values)
                timer.Cancel();
            _voteTimers.Clear();

            _launchedMaps.Clear();
        }
    }

    private async void StartGlobalPrisonVote()
    {
        Logger.Info($"Starting global prison vote... (players: {_priorityPlayers.Count})");

        // Дополнительная проверка - должно быть минимум 2 игрока
        if (_priorityPlayers.Count < 2)
        {
            Logger.Error($"Cannot start prison vote with {_priorityPlayers.Count} players, need at least 2");
            return;
        }

        // Проигрываем звук перед запуском голосования
        _audioSystem.PlayGlobal("/Audio/Machines/beep.ogg", Filter.Broadcast(), false, AudioParams.Default);

        // Вычисляем средний приоритет для каждой карты
        var mapScores = new Dictionary<string, double>();
        foreach (var mapId in new[] { MetusMapId, NoxMapId })
        {
            if (_prisonVotes.ContainsKey(mapId))
            {
                var votes = _prisonVotes[mapId];
                if (votes.Count > 0)
                {
                    var avgPriority = votes.Values.Where(p => p > 0).DefaultIfEmpty(0).Average();
                    mapScores[mapId] = avgPriority;
                    Logger.Info($"Map {mapId} average priority: {avgPriority} from {votes.Count} votes");
                }
                else
                {
                    mapScores[mapId] = 0; // Нет голосов = приоритет 0
                }
            }
            else
            {
                mapScores[mapId] = 0; // Карта не голосовалась
            }
        }

        // Выбираем карту с наивысшим средним приоритетом
        var maxScore = mapScores.Values.Max();
        var bestMaps = mapScores.Where(kvp => kvp.Value == maxScore).Select(kvp => kvp.Key).ToList();
        var selectedMapId = _random.Pick(bestMaps);
        Logger.Info($"Selected map {selectedMapId} with priority {maxScore} (candidates: {string.Join(", ", bestMaps)})");

        // Запускаем таймер для выбранной карты
        _voteTimers["GLOBAL"] = new CancellationTokenSource();
        var startEvent = new PlanetPrisonVoteUpdateEvent(selectedMapId, _priorityPlayers.Count, true, 5, _priorityPlayers.Count);
        RaiseNetworkEvent(startEvent);

        try
        {
            // Начинаем с 5 секунд
            for (int i = 5; i >= 1; i--)
            {
                if (_voteTimers["GLOBAL"].IsCancellationRequested)
                    return;

                var updateEvent = new PlanetPrisonVoteUpdateEvent(selectedMapId, _priorityPlayers.Count, true, i, _priorityPlayers.Count);
                RaiseNetworkEvent(updateEvent);

                await Task.Delay(1000, _voteTimers["GLOBAL"].Token);
            }

            // При 0 секундах сразу показываем "(запускается)" без задержки
            var launchEvent = new PlanetPrisonVoteUpdateEvent(selectedMapId, _priorityPlayers.Count, true, 0, _priorityPlayers.Count);
            RaiseNetworkEvent(launchEvent);

            // Небольшая задержка чтобы клиент успел показать "(запускается)" перед загрузкой карты
            await Task.Delay(100, _voteTimers["GLOBAL"].Token);

            // Запускаем выбранную карту
            var launchedMapId = await SpawnPrisonMap(selectedMapId, selectedMapId);

            // Сохраняем соответствие protoId -> MapId
            _launchedMaps[selectedMapId] = launchedMapId;

            // После запуска карты сбрасываем все приоритеты и пул игроков
            ResetAllPrioritiesAndPlayers();

            // Отправляем финальные обновления клиентам
            SendFinalUpdatesAfterLaunch(selectedMapId);
        }
        catch (TaskCanceledException)
        {
            // Голосование отменено
        }
        finally
        {
            _voteTimers.Remove("GLOBAL");
            var endEvent = new PlanetPrisonVoteUpdateEvent(selectedMapId, _priorityPlayers.Count, false, null, _priorityPlayers.Count);
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
            var gameMapProto = _protoManager.Index<GameMapPrototype>(protoId);

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
            if (!_protoManager.TryIndex(_random.Pick(stationComp.Biomes), out var biome))
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
        if (_prisonVotes.ContainsKey(mapId))
        {
            _prisonVotes[mapId].Clear();
        }

        if (_voteTimers.ContainsKey(mapId))
        {
            _voteTimers[mapId].Cancel();
            _voteTimers.Remove(mapId);
        }

        // Отправляем обновление о сбросе
        var resetEvent = new PlanetPrisonVoteUpdateEvent(mapId, 0, false);
        RaiseNetworkEvent(resetEvent);

        Logger.Info($"Prison voting reset for {mapId}");
    }
}

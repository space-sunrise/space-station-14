using System.Linq;
using System.Threading.Tasks;
using Content.Client.UserInterface.Systems.Ghost.Controls.PlanetPrison;
using Content.Client.UserInterface.Systems.Ghost;
using Content.Client.Players.PlayTimeTracking;
using Content.Shared._Sunrise.PlanetPrison;
using Content.Shared._Sunrise.NewLife;
using Content.Shared.Maps;
using Robust.Client.Audio;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Maths;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.IoC;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;
using System.IO;
using Content.Client.Lobby.UI.Loadouts;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Content.Shared.Humanoid;
using Content.Shared.Preferences;
using Content.Shared.Clothing;
using Robust.Client.Player;
using Content.Client.Lobby;

namespace Content.Client._Sunrise.PlanetPrison;

public sealed class PlanetPrisonUISystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IClientPreferencesManager _preferencesManager = default!;
    [Dependency] private readonly IResourceManager _resourceManager = default!;
    [Dependency] private readonly IEntityNetworkManager _net = default!;
    [Dependency] private readonly JobRequirementsManager _jobRequirements = default!;

    private PlanetPrisonWindow? _window;
    private LoadoutWindow? _loadoutWindow;
    private PlanetPrisonMapEntry? _metusMapEntry;
    private PlanetPrisonMapEntry? _noxMapEntry;
    private System.Threading.CancellationTokenSource? _timerCancellation;

    // Отдельное состояние для каждой карты
    private readonly Dictionary<string, bool> _hasVoted = new();
    private readonly Dictionary<string, PlanetPrisonMapEntry.PriorityLevel> _localPriority = new();
    private readonly Dictionary<string, bool> _mapLaunched = new();
    private readonly Dictionary<string, int> _lastVoteCount = new();
    private readonly Dictionary<int, string> _mapIdToProto = new(); // Соответствие MapId -> protoId

    // Отдельное состояние для ролей
    private readonly Dictionary<string, PlanetPrisonRoleEntry.PriorityLevel> _rolePriority = new();

    private int _totalPriorityPlayers = 0; // Общее количество игроков с приоритетами
    private int _minPlayersRequired = 2; // Минимальное количество игроков для запуска
    private bool _initialStateRequested = false; // Флаг, запрашивалось ли начальное состояние

    public event Action<bool>? PrisonButtonHighlightChanged;
    public event Action<bool>? PrisonButtonAvailabilityChanged;

    public override void Initialize()
    {
        base.Initialize();

        // Создаем окно сразу при инициализации системы
        _window = new PlanetPrisonWindow();
        _window.MapsTabPressed += OnMapsTabPressed;
        _window.RolesTabPressed += OnRolesTabPressed;
        _window.RolesTabActivated += OnRolesTabActivated;

        SubscribeNetworkEvent<PlanetPrisonVoteUpdateEvent>(OnPrisonVoteUpdate);
        SubscribeNetworkEvent<PlanetPrisonRoleUpdateEvent>(OnRoleUpdate);
        SubscribeNetworkEvent<PlanetPrisonCloseWindowEvent>(OnCloseWindow);
        SubscribeLocalEvent<MapRemovedEvent>(OnMapRemoved);

        PopulateMaps();
        PopulateRoles();
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _timerCancellation?.Cancel();

        if (_window != null)
        {
            _window.MapsTabPressed -= OnMapsTabPressed;
            _window.RolesTabPressed -= OnRolesTabPressed;
            _window.Dispose();
            _window = null;
        }

    }

    public void OpenWindow()
    {

        // Проверяем доступность ролей перед открытием окна
        if (!AreAnyPrisonRolesAvailable())
        {
            Logger.Info("PlanetPrisonUISystem: No prison roles available, not opening window");
            // Можно показать уведомление игроку
            return;
        }

        if (_window == null)
        {
            Logger.Error("PlanetPrisonUISystem: Window is null!");
            return;
        }

        // Сбрасываем счетчик участников и состояния карт перед запросом актуального статуса
        _totalPriorityPlayers = 0;

        // Сбрасываем статусы всех карт
        if (_metusMapEntry != null)
        {
            _metusMapEntry.HideStatus();
        }
        if (_noxMapEntry != null)
        {
            _noxMapEntry.HideStatus();
        }

        // Загружаем сохраненные приоритеты ролей из файла при каждом открытии окна
        LoadRolePrioritiesFromPreferences();

        // Обновляем данные перед открытием окна
        PopulateMaps();
        PopulateRoles();


        // Обновляем состояния кнопок ролей в соответствии с загруженными приоритетами
        UpdateRoleButtonStates();
        RefreshRoleRequirements();

        // Если окно открывается во время активного голосования, блокируем кнопки
        if (_window != null && HasActiveVoting())
        {
            _window.SetRolesLocked(true);
            _window.SetMapsLocked(true);
            Logger.Info("Window opened during active voting - buttons locked");
        }

        // Обновляем счетчик участников (покажет 0, потом обновится из ответа сервера)
        UpdatePriorityCounter();

        // Открываем окно только если оно не открыто
        if (_window != null && !_window.IsOpen)
        {
            _window.OpenCentered();
        }

        // Отправляем все текущие приоритеты на сервер
        SendAllRolePrioritiesToServer();

        // Запрашиваем актуальный статус голосования для всех карт
        RequestPrisonVoteStatus();
    }

    private void OnMapsTabPressed()
    {
        // Обработка переключения на вкладку карт
        // PopulateMaps() не вызываем, чтобы не сбросить состояние
        // PopulateMaps() вызывается только при первом открытии окна
    }

    private void OnRolesTabPressed()
    {
        // Обработка переключения на вкладку ролей
        // PopulateRoles() не вызываем, чтобы не сбросить состояние
        // PopulateRoles() вызывается только при первом открытии окна
    }

    private void OnRolesTabActivated()
    {
        UpdateRoleButtonStates();
        RefreshRoleRequirements();
    }

    private void PopulateMaps()
    {
        if (_window == null)
            return;

        _window.ClearMaps();

        // Создаем карту Metus
        _metusMapEntry = new PlanetPrisonMapEntry(
            Loc.GetString("planet-prison-map-metus-title"),
            Loc.GetString("planet-prison-map-metus-description", ("minPlayers", 2))
        );

        _metusMapEntry.PrioritySelected += (priority) => OnMapPrioritySelected("PlanetPrison", priority);
        _metusMapEntry.JoinPressed += OnJoinPressed;
        _window.AddMapEntry(_metusMapEntry);

        // Создаем карту Nox
        _noxMapEntry = new PlanetPrisonMapEntry(
            Loc.GetString("planet-prison-map-nox-title"),
            Loc.GetString("planet-prison-map-nox-description", ("minPlayers", 3))
        );

        _noxMapEntry.PrioritySelected += (priority) => OnMapPrioritySelected("PlanetPrisonOld", priority);
        _noxMapEntry.JoinPressed += OnJoinPressed;
        _window.AddMapEntry(_noxMapEntry);


        // Настраиваем каждую карту отдельно
        SetupMapEntry("PlanetPrison", _metusMapEntry);
        SetupMapEntry("PlanetPrisonOld", _noxMapEntry);

    }

    private void UpdatePriorityCounter()
    {
        if (_window == null) return;

        // Обычное отображение количества участников
        _window.GetPriorityCounterLabel().Text = Loc.GetString("planet-prison-participants-count",
            ("count", _totalPriorityPlayers));

        // Стандартный цвет фона
        var panel = _window.GetPriorityCounterPanel();
        panel.PanelOverride = new StyleBoxFlat { BackgroundColor = Color.FromHex("#202023") };

        _window.GetPriorityCounterPanel().Visible = true;
    }

    private int GetRequiredVotes(string mapId)
    {
        return 2; // Все карты требуют 2 голоса
    }

    private void SetupMapEntry(string mapId, PlanetPrisonMapEntry? entry)
    {
        if (entry == null) return;

        // Инициализируем состояния если они не существуют
        if (!_localPriority.ContainsKey(mapId))
            _localPriority[mapId] = PlanetPrisonMapEntry.PriorityLevel.Never;
        if (!_hasVoted.ContainsKey(mapId))
            _hasVoted[mapId] = false;
        if (!_mapLaunched.ContainsKey(mapId))
            _mapLaunched[mapId] = false;
        if (!_lastVoteCount.ContainsKey(mapId))
            _lastVoteCount[mapId] = 0;

        // Устанавливаем сохраненный приоритет
        entry.SetSelectedPriority(_localPriority[mapId]);

        // Восстанавливаем состояние
        if (_mapLaunched.ContainsKey(mapId) && _mapLaunched[mapId])
        {
            // Карта запущена - показываем статус и кнопку "Присоединиться"
            entry.ShowLaunchedStatus();
            entry.SetLaunched(true);
        }
        else
        {
            // Карта не запущена - делаем её активной
            entry.HideStatus();
            entry.HideVoteCount();
            entry.SetLaunched(false);
            entry.EnableButtons();
        }
    }

    private void OnMapPrioritySelected(string mapId, PlanetPrisonMapEntry.PriorityLevel priority)
    {
        Logger.Info($"{mapId} priority selected: {priority}");

        // Проверяем, что игрок имеет доступ хотя бы к одной роли тюрьмы
        if (!AreAnyPrisonRolesAvailable())
        {
            Logger.Warning($"Player tried to vote for {mapId} but has no available prison roles - blocking vote");
            return;
        }

        _localPriority[mapId] = priority;
        // Не устанавливаем _hasVoted[mapId] = true здесь - кнопки блокируются только при запуске голосования

        // Отправляем голос на сервер (не блокируем кнопки локально)
        var message = new PlanetPrisonVoteMessage(mapId, (int)priority);
        RaiseNetworkEvent(message);

        // Отправляем запрос на обновление статуса, чтобы UI обновился
        RequestPrisonVoteStatus();
    }

    private void OnPrisonVoteUpdate(PlanetPrisonVoteUpdateEvent msg)
    {
        Logger.Info($"OnPrisonVoteUpdate received - MapId={msg.MapId}, IsVoting={msg.IsVoting}, TotalPriorityPlayers={msg.TotalPriorityPlayers}, InsufficientRoles=[{string.Join(",", msg.InsufficientRoles)}]");

        // Специальная обработка глобального сброса состояний
        if (!msg.IsVoting && msg.TotalPriorityPlayers == -1 && msg.RemainingSeconds == 0)
        {
            // Полный сброс всех состояний на клиенте (специальный флаг TotalPriorityPlayers = -1)
            Logger.Info($"Received global state reset from server for {msg.MapId}, resetting counter to 0");
            _totalPriorityPlayers = 0; // Явно сбрасываем счетчик

            // Полностью сбрасываем статусы запущенных карт при глобальном сбросе
            _mapLaunched.Clear();

            ResetAllClientStates(null);

            // Перезагружаем приоритеты ролей после глобального сброса
            LoadRolePrioritiesFromPreferences();
            if (_window != null)
            {
                UpdateRoleButtonStates();
            }

            Logger.Info("Global state reset completed on client");
            return;
        }

        Logger.Debug($"DEBUG: Processing update for {msg.MapId} - VoteCount: {msg.VoteCount}, IsVoting: {msg.IsVoting}, IsLaunched: {msg.IsLaunched}, RemainingSeconds: {msg.RemainingSeconds}, TotalPriorityPlayers: {msg.TotalPriorityPlayers}, InsufficientRoles: [{string.Join(", ", msg.InsufficientRoles ?? Array.Empty<string>())}]");

        // Сохраняем количество голосов для конкретной карты
        _lastVoteCount[msg.MapId] = msg.VoteCount;

        // Обновляем статус запущенной карты
        _mapLaunched[msg.MapId] = msg.IsLaunched;
        var entry = msg.MapId == "PlanetPrison" ? _metusMapEntry : _noxMapEntry;
        if (entry != null)
        {
            entry.SetLaunched(msg.IsLaunched);
        }

        // Обновляем общее количество игроков с приоритетами только если это не специальный флаг сброса
        if (msg.TotalPriorityPlayers != -1)
        {
            _totalPriorityPlayers = msg.TotalPriorityPlayers;
        }

        // Обновляем счетчик приоритетов
        UpdatePriorityCounter();

        // Специальная обработка для только что запущенной карты
        if (msg.IsLaunched)
        {
            _mapLaunched[msg.MapId] = true;

            // Помечаем entry как запущенную
            var launchedEntry = msg.MapId == "PlanetPrison" ? _metusMapEntry : _noxMapEntry;
            if (launchedEntry != null)
            {
                launchedEntry.SetLaunched(true);
            }

            // Сбрасываем состояния голосования (но не статусы запущенных карт)
            ResetVotingStatesOnly();
            // Принудительно обновляем UI для всех карт
            SetupMapEntry("PlanetPrison", _metusMapEntry);
            SetupMapEntry("PlanetPrisonOld", _noxMapEntry);
        }

        // Обновляем UI для карты
        var targetEntry = msg.MapId == "PlanetPrison" ? _metusMapEntry : _noxMapEntry;
        if (targetEntry != null)
        {
            UpdateMapEntryUI(msg, targetEntry);
        }

        // Определяем какую карту обновлять
        PlanetPrisonMapEntry? mapEntry = null;
        if (msg.MapId == "PlanetPrison" && _metusMapEntry != null)
            mapEntry = _metusMapEntry;
        else if (msg.MapId == "PlanetPrisonOld" && _noxMapEntry != null)
            mapEntry = _noxMapEntry;

        if (mapEntry != null)
        {
            if (msg.IsVoting && msg.RemainingSeconds > 0)
            {
                // Во время голосования: блокируем кнопки (счетчик не показываем)
                mapEntry.HideVoteCount();
                mapEntry.DisableButtons(); // Блокируем все кнопки во время голосования

                if (msg.RemainingSeconds > 0)
                {
                    // Показываем таймер
                    mapEntry.ShowTimer((int)msg.RemainingSeconds);
                    mapEntry.HideStatus();
                    mapEntry.HideVoteCount();
                }
                else // RemainingSeconds == 0
                {
                    // Таймер закончился, показываем "(запускается)"
                    mapEntry.HideTimer();
                    mapEntry.HideVoteCount();
                    mapEntry.ShowLaunchingStatus();
                }

                UpdatePrisonButtonHighlight(true);

                // Проигрываем звук, если это начало голосования
                if (msg.RemainingSeconds == 5)
                {
                    // _entManager.System<AudioSystem>().PlayGlobal("/Audio/Effects/beep.ogg", Filter.Local(), false, AudioParams.Default);
                }
            }
            else if (msg.IsLaunched)
            {
                // Карта запущена - показываем статус запуска
                mapEntry.HideVoteCount();
                mapEntry.HideTimer();
                mapEntry.DisableButtons(); // Полная блокировка всех кнопок
                UpdatePrisonButtonHighlight(false);
                _hasVoted[msg.MapId] = true;

                // Показываем "(запускается)" желтым, затем "(запущен)" красным
                mapEntry.ShowLaunchingStatus();
                Robust.Shared.Timing.Timer.Spawn(2000, () => {
                    if (mapEntry != null)
                    {
                        mapEntry.ShowLaunchedStatus();
                    }
                });
            }
            else
            {
                // Голосование не активно
                mapEntry.HideTimer();

                if (_mapLaunched.ContainsKey(msg.MapId) && _mapLaunched[msg.MapId])
                {
                    // Карта запущена - показываем статус "(запущен)"
                    mapEntry.ShowLaunchedStatus();
                    mapEntry.HideVoteCount();
                    mapEntry.DisableButtons(); // Кнопки остаются заблокированными
                }
                else
                {
                    // Карта не запущена
                    mapEntry.HideStatus();

                    // Не показываем счетчики для отдельных карт
                    mapEntry.HideVoteCount();
                    if (!_hasVoted.ContainsKey(msg.MapId) || !_hasVoted[msg.MapId])
                    {
                        mapEntry.EnableButtons();
                    }
                    else
                    {
                        mapEntry.DisableButtons();
                    }
                }

                UpdatePrisonButtonHighlight(false);
            }
        }

        // Блокируем/разблокируем кнопки ролей и карт только во время отсчета запуска
        if (_window != null)
        {
            var shouldLock = msg.IsVoting && msg.RemainingSeconds >= 0;
            _window.SetRolesLocked(shouldLock);
            _window.SetMapsLocked(shouldLock);
        }
    }

    private void OnRolePrioritySelected(string roleId, PlanetPrisonRoleEntry.PriorityLevel priority)
    {
        // Обновляем локальное состояние
        _rolePriority[roleId] = priority;

        // Сохраняем приоритеты ролей в preferences
        SaveRolePrioritiesToPreferences();

        // Отправляем приоритет роли на сервер
        var msg = new PlanetPrisonRolePriorityMessage(roleId, (int)priority);
        RaiseNetworkEvent(msg);

        // Отправляем запрос на обновление статуса, чтобы UI обновился
        RequestPrisonVoteStatus();
    }

    private void SendAllRolePrioritiesToServer()
    {
        foreach (var kvp in _rolePriority)
        {
            var roleId = kvp.Key;
            var priority = kvp.Value;
            var msg = new PlanetPrisonRolePriorityMessage(roleId, (int)priority);
            RaiseNetworkEvent(msg);
        }
    }


    private void OnRoleLoadoutPressed(string roleId)
    {
        try
        {
            // Закрываем существующее окно, если оно открыто
            _loadoutWindow?.Dispose();
            _loadoutWindow = null;

            // Получаем прототип роли
            if (!_prototypeManager.TryIndex<JobPrototype>(roleId, out var jobProto))
            {
                Logger.Error($"Job prototype not found for role: {roleId}");
                return;
            }

            // Получаем прототип лодаута для роли
            var loadoutProtoId = LoadoutSystem.GetJobPrototype(jobProto.ID);
            if (!_prototypeManager.TryIndex<RoleLoadoutPrototype>(loadoutProtoId, out var roleLoadoutProto))
            {
                Logger.Error($"Role loadout prototype not found for role: {roleId}");
                return;
            }

            // Получаем реальный профиль игрока, как в lobby
            var profile = (HumanoidCharacterProfile?)_preferencesManager.Preferences?.SelectedCharacter;
            if (profile == null)
            {
                Logger.Error("Cannot get player profile for loadout window");
                return;
            }

            // Получаем существующий лодаут игрока для этой роли
            RoleLoadout? roleLoadout = null;
            if (profile.Loadouts.TryGetValue(LoadoutSystem.GetJobPrototype(jobProto.ID), out var existingLoadout))
            {
                roleLoadout = existingLoadout; // НЕ клонируем, работаем с оригиналом
            }
            else
            {
                // Если лодаута нет, создаем дефолтный
                roleLoadout = new RoleLoadout(loadoutProtoId);
                roleLoadout.SetDefault(profile, _playerManager.LocalSession, _prototypeManager, Array.Empty<string>());
            }

            // Создаем и открываем окно лодаутов, как в lobby
            var collection = IoCManager.Instance!;
            var session = _playerManager.LocalSession!;

            _loadoutWindow = new LoadoutWindow(profile, roleLoadout, roleLoadoutProto, session, collection, null)
            {
                Title = Loc.GetString("loadout-window-title-loadout", ("job", $"{jobProto.LocalizedName}")),
            };

            // Добавляем обработчики событий, как в lobby
            _loadoutWindow.OnLoadoutPressed += (loadoutGroup, loadoutProto) =>
            {
                roleLoadout.AddLoadout(loadoutGroup, loadoutProto, _prototypeManager);
                _loadoutWindow.RefreshLoadouts(roleLoadout, session, collection);
                // Сохраняем изменения в профиле
                SaveLoadoutToProfile(profile, roleLoadout);
            };

            _loadoutWindow.OnLoadoutUnpressed += (loadoutGroup, loadoutProto) =>
            {
                roleLoadout.RemoveLoadout(loadoutGroup, loadoutProto, _prototypeManager);
                _loadoutWindow.RefreshLoadouts(roleLoadout, session, collection);
                // Сохраняем изменения в профиле
                SaveLoadoutToProfile(profile, roleLoadout);
            };

            // Открываем окно
            _loadoutWindow.RefreshLoadouts(roleLoadout, session, collection);
            _loadoutWindow.OpenCenteredLeft();

            Logger.Info($"Opened loadout window for role: {roleId}");
        }
        catch (Exception e)
        {
            Logger.Error($"Error opening loadout window for role {roleId}: {e.Message}");
        }
    }

    private void SaveLoadoutToProfile(HumanoidCharacterProfile? profile, RoleLoadout roleLoadout)
    {
        if (profile == null || _preferencesManager.Preferences == null)
            return;

        // Создаём обновлённый профиль с новым loadout
        var updatedProfile = profile.WithLoadout(roleLoadout);

        // Получаем текущий выбранный слот
        var selectedSlot = _preferencesManager.Preferences.SelectedCharacterIndex;

        // Обновляем профиль в preferences
        _preferencesManager.UpdateCharacter(updatedProfile, selectedSlot);
    }

    private void SaveRolePrioritiesToPreferences()
    {
        Logger.Info($"Saving role priorities to file: {_rolePriority.Count} priorities");

        try
        {
            // Сохраняем приоритеты ролей в файл
            var prioritiesData = string.Join(",", _rolePriority.Select(kvp => $"{kvp.Key}:{(int)kvp.Value}"));
            Logger.Info($"Saving priorities data: '{prioritiesData}'");
            var dirPath = new ResPath("/PlanetPrison");
            if (!_resourceManager.UserData.IsDir(dirPath))
            {
                _resourceManager.UserData.CreateDir(dirPath);
            }
            var filePath = dirPath / "priorities.txt";
            Logger.Info($"Saving to file: {filePath}");

            using var stream = _resourceManager.UserData.Open(filePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            using var writer = new StreamWriter(stream);
            writer.Write(prioritiesData);

            Logger.Info("Role priorities saved to file successfully");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to save role priorities to file: {ex}");
        }
    }

    private void LoadRolePrioritiesFromPreferences()
    {
        try
        {
            var dirPath = new ResPath("/PlanetPrison");
            if (!_resourceManager.UserData.IsDir(dirPath))
            {
                _resourceManager.UserData.CreateDir(dirPath);
            }
            var filePath = dirPath / "priorities.txt";
            if (_resourceManager.UserData.Exists(filePath))
            {
                using var stream = _resourceManager.UserData.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(stream);
                var prioritiesData = reader.ReadToEnd();

                if (!string.IsNullOrEmpty(prioritiesData))
                {
                    var priorityPairs = prioritiesData.Split(',');

                    foreach (var pair in priorityPairs)
                    {
                        var parts = pair.Split(':');
                        if (parts.Length == 2 && int.TryParse(parts[1], out var priorityValue))
                        {
                            _rolePriority[parts[0]] = (PlanetPrisonRoleEntry.PriorityLevel)priorityValue;
                        }
                        else
                        {
                            Logger.Warning($"Failed to parse priority pair: '{pair}'");
                        }
                    }
                }
            }
            else
            {
                Logger.Info("No priorities file found");
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load role priorities from file: {ex}");
        }

        Logger.Info($"Loaded {_rolePriority.Count} role priorities");
    }

    private void UpdateRoleButtonStates()
    {
        if (_window == null)
            return;


        _window.UpdateRolePriorities(_rolePriority);
    }

    private bool HasActiveVoting()
    {
        // Проверяем, есть ли активное голосование по любой карте
        return _lastVoteCount.ContainsKey("PlanetPrison") && _lastVoteCount.ContainsKey("PlanetPrisonOld") &&
               (_lastVoteCount["PlanetPrison"] > 0 || _lastVoteCount["PlanetPrisonOld"] > 0);
    }

    private void OnRoleUpdate(PlanetPrisonRoleUpdateEvent msg)
    {
        // Обновляем UI роли на основе статуса
        // Сервер автоматически отправляет обновления карт при изменении ролей,
        // поэтому дополнительный запрос не нужен

        // Пока что просто логируем, в будущем можно добавить визуальные индикаторы
        // TODO: Найти соответствующий PlanetPrisonRoleEntry и обновить его статус
    }

    private void OnCloseWindow(PlanetPrisonCloseWindowEvent msg)
    {
        // Закрываем окно тюрьмы после назначения роли
        Logger.Info("Closing prison window after role assignment");
        if (_window != null)
        {
            _window.Close();
            // Не устанавливаем _window = null, чтобы можно было открыть снова
        }
    }

    private void UpdatePrisonButtonHighlight(bool highlight)
    {
        PrisonButtonHighlightChanged?.Invoke(highlight);
    }

    private void OnMapRemoved(MapRemovedEvent ev)
    {
        // При удалении карты тюрьмы сбрасываем статус ТОЛЬКО удаленной карты
        // Карты тюрьмы имеют ID >= 100
        var mapIdValue = (int)ev.MapId;
        if (mapIdValue >= 100) // Карта тюрьмы
        {
            // Определяем, какая карта была удалена по ID
            // Поскольку порядок запуска фиксированный (Metus первая, Nox вторая),
            // и они получают последовательные ID, используем эту логику
            string? removedProtoId = null;
            if (_mapLaunched["PlanetPrison"])
            {
                _mapLaunched["PlanetPrison"] = false;
                removedProtoId = "PlanetPrison";
                Logger.Info($"Metus map (ID: {mapIdValue}) removed, status reset");
            }
            else if (_mapLaunched["PlanetPrisonOld"])
            {
                _mapLaunched["PlanetPrisonOld"] = false;
                removedProtoId = "PlanetPrisonOld";
                Logger.Info($"Nox map (ID: {mapIdValue}) removed, status reset");
            }

            // Обновляем UI только для удаленной карты, другая остается запущенной
            if (removedProtoId == "PlanetPrison")
            {
                SetupMapEntry("PlanetPrison", _metusMapEntry);
            }
            else if (removedProtoId == "PlanetPrisonOld")
            {
                SetupMapEntry("PlanetPrisonOld", _noxMapEntry);
            }
        }
    }

    private void RequestPrisonVoteStatus()
    {
        // Отправляем запросы на получение текущего статуса голосования для всех карт
        foreach (var mapId in new[] { "PlanetPrison", "PlanetPrisonOld" })
        {
            var message = new PlanetPrisonStatusRequestMessage(mapId);
            RaiseNetworkEvent(message);
        }
    }

    // Метод для сброса состояния карты (вызывается при удалении карты)
    public void ResetMapState()
    {
        _initialStateRequested = false; // Позволяем заново запросить состояние
        ResetAllClientStates(null); // null означает сбросить все состояния
    }

    // Метод для сброса всех состояний при запуске карты
    private void ResetAllClientStates(string? launchedMapId)
    {
        _totalPriorityPlayers = 0; // Сбрасываем счетчик игроков

        // Сбрасываем состояние для всех карт, но НЕ трогаем статус _mapLaunched (запущенные карты остаются запущенными)
        foreach (var mapId in new[] { "PlanetPrison", "PlanetPrisonOld" })
        {

            _hasVoted[mapId] = false;
            // НЕ сбрасываем _mapLaunched - запущенные карты остаются запущенными до их удаления
            _lastVoteCount[mapId] = 0;
            _localPriority[mapId] = PlanetPrisonMapEntry.PriorityLevel.Never;
        }

        // Обновляем UI для каждой карты отдельно
        SetupMapEntry("PlanetPrison", _metusMapEntry);
        SetupMapEntry("PlanetPrisonOld", _noxMapEntry);

        UpdatePriorityCounter();
    }

    // Сбрасывает только состояния голосования, но сохраняет статусы запущенных карт
    private void ResetVotingStatesOnly()
    {
        _totalPriorityPlayers = 0;

        foreach (var mapId in new[] { "PlanetPrison", "PlanetPrisonOld" })
        {
            _hasVoted[mapId] = false;
            _lastVoteCount[mapId] = 0;
            _localPriority[mapId] = PlanetPrisonMapEntry.PriorityLevel.Never;
        }

        UpdatePriorityCounter();
    }

    private void UpdateMapEntryUI(PlanetPrisonVoteUpdateEvent msg, PlanetPrisonMapEntry entry)
    {
        Logger.Debug($"DEBUG: UpdateMapEntryUI called for {msg.MapId} - IsVoting: {msg.IsVoting}, IsLaunched: {msg.IsLaunched}, RemainingSeconds: {msg.RemainingSeconds}, VoteCount: {msg.VoteCount}");

        // Обработка только что запущенной карты - показываем финальный статус сразу
        if (msg.IsLaunched)
        {
            Logger.Debug($"DEBUG: Handling launching status for {msg.MapId}");
            entry.HideTimer();
            entry.HideVoteCount();
            entry.ShowLaunchingStatus();
            entry.DisableButtons();
            return;
        }

        if (msg.IsVoting)
        {
            HandleVotingUI(msg, entry);
        }
        else
        {
            HandleNonVotingUI(msg, entry);
        }
    }

    private void HandleVotingUI(PlanetPrisonVoteUpdateEvent msg, PlanetPrisonMapEntry entry)
    {
        entry.DisableButtons();
        entry.HideVoteCount();
        entry.HideStatus();

        // При 1 секунде показываем "(запускается)" вместо цифры для плавного перехода
        if (msg.RemainingSeconds > 1)
        {
            entry.ShowTimer((int)msg.RemainingSeconds);
        }
        else
        {
            entry.HideTimer();
            entry.ShowLaunchingStatus();
        }
    }

    private void HandleNonVotingUI(PlanetPrisonVoteUpdateEvent msg, PlanetPrisonMapEntry entry)
    {
        Logger.Debug($"DEBUG: HandleNonVotingUI for {msg.MapId} - checking _mapLaunched: {(_mapLaunched.ContainsKey(msg.MapId) ? _mapLaunched[msg.MapId].ToString() : "NOT_FOUND")}");

        entry.HideTimer();

        if (_mapLaunched.ContainsKey(msg.MapId) && _mapLaunched[msg.MapId])
        {
            Logger.Debug($"DEBUG: Showing launched status for {msg.MapId} because _mapLaunched is true");
            // Карта уже запущена - блокируем интерфейс
            entry.ShowLaunchedStatus();
            entry.DisableButtons();
            entry.HideVoteCount();
        }
        else
        {
            Logger.Debug($"DEBUG: Hiding status for {msg.MapId} because _mapLaunched is false or not found");
            // Карта доступна для голосования
            entry.HideStatus();
            entry.EnableButtons();

            if (msg.VoteCount > 0)
                entry.ShowVoteCount(msg.VoteCount, GetRequiredVotes(msg.MapId));
            else
                entry.HideVoteCount();
        }
    }

    private void PopulateRoles()
    {
        if (_window == null)
            return;


        _window.ClearRoles();

        // Добавляем доступные роли тюрьмы в правильном порядке
        // 1. Заключенный (самая верхняя роль)
        var prisonerEntry = new PlanetPrisonRoleEntry(
            "PlanetPrisoner",
            Loc.GetString("planet-prison-role-prisoner-title"),
            Loc.GetString("planet-prison-role-prisoner-description")
        );
        prisonerEntry.PrioritySelected += (priority) => OnRolePrioritySelected("PlanetPrisoner", priority);
        prisonerEntry.LoadoutPressed += () => OnRoleLoadoutPressed("PlanetPrisoner");
        CheckRoleRequirements("PlanetPrisoner", prisonerEntry);
        _window.AddRoleEntry(prisonerEntry);
        // Восстанавливаем сохраненный приоритет или устанавливаем Never по умолчанию
        _rolePriority.TryAdd("PlanetPrisoner", PlanetPrisonRoleEntry.PriorityLevel.Never);

        // 2. Начальник тюрьмы
        var headEntry = new PlanetPrisonRoleEntry(
            "HeadOfPrison",
            Loc.GetString("planet-prison-role-head-title"),
            Loc.GetString("planet-prison-role-head-description")
        );
        headEntry.PrioritySelected += (priority) => OnRolePrioritySelected("HeadOfPrison", priority);
        headEntry.LoadoutPressed += () => OnRoleLoadoutPressed("HeadOfPrison");
        CheckRoleRequirements("HeadOfPrison", headEntry);
        _window.AddRoleEntry(headEntry);
        // Восстанавливаем сохраненный приоритет или устанавливаем Never по умолчанию
        _rolePriority.TryAdd("HeadOfPrison", PlanetPrisonRoleEntry.PriorityLevel.Never);

        // 3. Инспектор тюрьмы
        var inspectorEntry = new PlanetPrisonRoleEntry(
            "PrisonInspector",
            Loc.GetString("planet-prison-role-inspector-title"),
            Loc.GetString("planet-prison-role-inspector-description")
        );
        inspectorEntry.PrioritySelected += (priority) => OnRolePrioritySelected("PrisonInspector", priority);
        inspectorEntry.LoadoutPressed += () => OnRoleLoadoutPressed("PrisonInspector");
        CheckRoleRequirements("PrisonInspector", inspectorEntry);
        _window.AddRoleEntry(inspectorEntry);
        // Восстанавливаем сохраненный приоритет или устанавливаем Never по умолчанию
        _rolePriority.TryAdd("PrisonInspector", PlanetPrisonRoleEntry.PriorityLevel.Never);

        // 4. Разнорабочий тюрьмы
        var workerEntry = new PlanetPrisonRoleEntry(
            "PrisonWorker",
            Loc.GetString("planet-prison-role-worker-title"),
            Loc.GetString("planet-prison-role-worker-description")
        );
        workerEntry.PrioritySelected += (priority) => OnRolePrioritySelected("PrisonWorker", priority);
        workerEntry.LoadoutPressed += () => OnRoleLoadoutPressed("PrisonWorker");
        CheckRoleRequirements("PrisonWorker", workerEntry);
        _window.AddRoleEntry(workerEntry);
        // Восстанавливаем сохраненный приоритет или устанавливаем Never по умолчанию
        _rolePriority.TryAdd("PrisonWorker", PlanetPrisonRoleEntry.PriorityLevel.Never);

        // 5. Инженер тюрьмы
        var engineerEntry = new PlanetPrisonRoleEntry(
            "PrisonEngineer",
            Loc.GetString("planet-prison-role-engineer-title"),
            Loc.GetString("planet-prison-role-engineer-description")
        );
        engineerEntry.PrioritySelected += (priority) => OnRolePrioritySelected("PrisonEngineer", priority);
        engineerEntry.LoadoutPressed += () => OnRoleLoadoutPressed("PrisonEngineer");
        CheckRoleRequirements("PrisonEngineer", engineerEntry);
        _window.AddRoleEntry(engineerEntry);
        // Восстанавливаем сохраненный приоритет или устанавливаем Never по умолчанию
        _rolePriority.TryAdd("PrisonEngineer", PlanetPrisonRoleEntry.PriorityLevel.Never);

        // 6. Ученый тюрьмы
        var scientistEntry = new PlanetPrisonRoleEntry(
            "PrisonScientist",
            Loc.GetString("planet-prison-role-scientist-title"),
            Loc.GetString("planet-prison-role-scientist-description")
        );
        scientistEntry.PrioritySelected += (priority) => OnRolePrioritySelected("PrisonScientist", priority);
        scientistEntry.LoadoutPressed += () => OnRoleLoadoutPressed("PrisonScientist");
        CheckRoleRequirements("PrisonScientist", scientistEntry);
        _window.AddRoleEntry(scientistEntry);
        // Восстанавливаем сохраненный приоритет или устанавливаем Never по умолчанию
        _rolePriority.TryAdd("PrisonScientist", PlanetPrisonRoleEntry.PriorityLevel.Never);

        // 7. Врач тюрьмы
        var doctorEntry = new PlanetPrisonRoleEntry(
            "PrisonDoctor",
            Loc.GetString("planet-prison-role-doctor-title"),
            Loc.GetString("planet-prison-role-doctor-description")
        );
        doctorEntry.PrioritySelected += (priority) => OnRolePrioritySelected("PrisonDoctor", priority);
        doctorEntry.LoadoutPressed += () => OnRoleLoadoutPressed("PrisonDoctor");
        CheckRoleRequirements("PrisonDoctor", doctorEntry);
        _window.AddRoleEntry(doctorEntry);
        // Восстанавливаем сохраненный приоритет или устанавливаем Never по умолчанию
        _rolePriority.TryAdd("PrisonDoctor", PlanetPrisonRoleEntry.PriorityLevel.Never);

        // 8. Повар тюрьмы
        var chefEntry = new PlanetPrisonRoleEntry(
            "PrisonChef",
            Loc.GetString("planet-prison-role-chef-title"),
            Loc.GetString("planet-prison-role-chef-description")
        );
        chefEntry.PrioritySelected += (priority) => OnRolePrioritySelected("PrisonChef", priority);
        chefEntry.LoadoutPressed += () => OnRoleLoadoutPressed("PrisonChef");
        CheckRoleRequirements("PrisonChef", chefEntry);
        _window.AddRoleEntry(chefEntry);
        // Восстанавливаем сохраненный приоритет или устанавливаем Never по умолчанию
        _rolePriority.TryAdd("PrisonChef", PlanetPrisonRoleEntry.PriorityLevel.Never);

        // 9. Стажер тюрьмы
        var traineeEntry = new PlanetPrisonRoleEntry(
            "PrisonTrainee",
            Loc.GetString("planet-prison-role-trainee-title"),
            Loc.GetString("planet-prison-role-trainee-description")
        );
        traineeEntry.PrioritySelected += (priority) => OnRolePrioritySelected("PrisonTrainee", priority);
        traineeEntry.LoadoutPressed += () => OnRoleLoadoutPressed("PrisonTrainee");
        CheckRoleRequirements("PrisonTrainee", traineeEntry);
        _window.AddRoleEntry(traineeEntry);
        // Восстанавливаем сохраненный приоритет или устанавливаем Never по умолчанию
        _rolePriority.TryAdd("PrisonTrainee", PlanetPrisonRoleEntry.PriorityLevel.Never);
    }

    private void OnJoinPressed()
    {
        var msg = new NewLifeOpenRequest();
        _net.SendSystemNetworkMessage(msg);
    }

    private void RefreshRoleRequirements()
    {
        if (_window == null)
            return;

        // Обновляем требования для всех ролей
        var roleIds = new[] { "PlanetPrisoner", "HeadOfPrison", "PrisonInspector", "PrisonWorker", "PrisonEngineer", "PrisonScientist", "PrisonDoctor", "PrisonChef", "PrisonTrainee" };

        foreach (var roleId in roleIds)
        {
            var entry = _window.GetRoleEntry(roleId);
            if (entry != null)
            {
                CheckRoleRequirements(roleId, entry);
            }
        }
    }

    private void CheckRoleRequirements(string roleId, PlanetPrisonRoleEntry entry)
    {
        // Получаем прототип роли
        if (!_prototypeManager.TryIndex<JobPrototype>(roleId, out var jobProto))
        {
            Logger.Warning($"Job prototype not found for role: {roleId}");
            return;
        }

        // Получаем текущий профиль игрока
        var profile = (HumanoidCharacterProfile?)_preferencesManager.Preferences?.SelectedCharacter;
        if (profile == null)
        {
            Logger.Warning("Cannot get player profile for role requirements check");
            return;
        }

        // Проверяем требования роли
        if (!_jobRequirements.IsAllowed(jobProto, profile, out var reason))
        {
            entry.LockRequirements(reason);
        }
        else
        {
            entry.UnlockRequirements();
        }
    }

    public bool AreAnyPrisonRolesAvailable()
    {
        // Получаем текущий профиль игрока
        var profile = (HumanoidCharacterProfile?)_preferencesManager.Preferences?.SelectedCharacter;
        if (profile == null)
        {
            Logger.Warning("Cannot get player profile for prison roles availability check - profile not loaded yet");
            // Если профиль не загружен, разрешаем доступ (на случай задержки загрузки)
            return true;
        }

        // Проверяем все роли тюрьмы
        var prisonRoleIds = new[] { "PlanetPrisoner", "HeadOfPrison", "PrisonInspector", "PrisonWorker", "PrisonEngineer", "PrisonScientist", "PrisonDoctor", "PrisonChef", "PrisonTrainee" };

        foreach (var roleId in prisonRoleIds)
        {
            if (!_prototypeManager.TryIndex<JobPrototype>(roleId, out var jobProto))
                continue;

            // Если хотя бы одна роль доступна, возвращаем true
            if (_jobRequirements.IsAllowed(jobProto, profile, out _))
            {
                return true;
            }
        }

        Logger.Info("No prison roles are available for player - all locked by requirements");
        return false;
    }

    public string GetPrisonRequirementsText()
    {
        // Получаем текущий профиль игрока
        var profile = (HumanoidCharacterProfile?)_preferencesManager.Preferences?.SelectedCharacter;
        if (profile == null)
        {
            return "Loading player profile...";
        }

        // Получаем требования для роли PlanetPrisoner (как пример самой простой роли)
        if (_prototypeManager.TryIndex<JobPrototype>("PlanetPrisoner", out var prisonerProto))
        {
            if (!_jobRequirements.IsAllowed(prisonerProto, profile, out var reason))
            {
                // Возвращаем только текст требований
                return reason.ToString();
            }
        }

        return "No prison roles available";
    }

}
